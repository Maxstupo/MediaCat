namespace MediaCat.Core.Services.Catalog.Database {
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using MediaCat.Core.Model;
    using MediaCat.Core.Utility.Extensions;

    /// <summary>
    /// A file system based warehouse for handling the actual imported files of a MediaCat catalog.
    /// </summary>
    public sealed class Warehouse : IWarehouse {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IFileSystem fileSystem;
        private readonly ISQLiteDatabase database;

        public Warehouse(IFileSystem fileSystem, ISQLiteDatabase database) {
            this.fileSystem = fileSystem;
            this.database = database;
        }

        /// <summary>
        /// Create a new store at the provided path.
        /// </summary>
        /// <param name="label">The cosmetic label of this store.</param>
        /// <param name="path">The folderpath of this store. Supports absolute, relative, and network paths.</param>
        /// <param name="isDefault">True if this new store should be the default selected.</param>
        /// <returns> 
        /// A warehouse result containing the newly created Store and the status of this operation.
        /// <br/><br/>Possible status codes are: <br/>
        ///     - <see cref="WarehouseStoreStatus.FailureDatabaseClosed"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailureLabelAlreadyExists"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailurePathAlreadyExists"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailureFolderAlreadyExists"/><br/>
        ///     - <see cref="WarehouseStoreStatus.Success"/>
        /// </returns>
        public async Task<WarehouseResult> CreateStoreAsync(string label, string path, bool isDefault) {
            if (!database.IsOpen)
                return new WarehouseResult(WarehouseStoreStatus.FailureDatabaseClosed);

            if (string.IsNullOrWhiteSpace(label))
                label = path;

            // Check if the label is already registered.
            if (await database.Connection.Table<Store>().Where(x => x.Label == label).CountAsync() != 0)
                return new WarehouseResult(WarehouseStoreStatus.FailureLabelAlreadyExists);

            // Check if the path is already registered.
            if (await database.Connection.Table<Store>().Where(x => x.Path == path).CountAsync() != 0)
                return new WarehouseResult(WarehouseStoreStatus.FailurePathAlreadyExists);

            Store store = new Store {
                Guid = Guid.NewGuid().ToString(),
                Label = label,
                Path = path,
                IsDefault = isDefault,
                Status = StoreStatus.Ok
            };

            string storePath = ResolveStorePath(store); // the folderpath of the store.

            if (fileSystem.Directory.Exists(storePath)) // Check if the folder exists.
                return new WarehouseResult(WarehouseStoreStatus.FailureFolderAlreadyExists);

            // Check if we have write access.
            if (!fileSystem.IsDirectoryWritable(fileSystem.Directory.GetParent(storePath).FullName))
                return new WarehouseResult(WarehouseStoreStatus.FailureFolderNotWritable);


            Logger.Info("Create store: {path} ({label}) {guid} -> {storePath}", store.Path, store.Label, store.Guid, storePath);

            // If this new store is default, make all other stores not default.
            if (store.IsDefault) {
                await EnsureDefaultStoreAsync(store);
            } else {
                // Make this store the default if no default store exists.
                store.IsDefault = await database.Connection.Table<Store>().Where(x => x.IsDefault).CountAsync() == 0;
            }

            // Insert the newly created store into the database.
            Logger.Debug("Updating database...");
            await database.Connection.InsertAsync(store).ConfigureAwait(false);

            // Create directory stucture.
            Logger.Debug("Creating directory structure...");
            fileSystem.Directory.CreateDirectory(storePath);

            GetPartitionFolders(store).ForEach(x => fileSystem.Directory.CreateDirectory(x));

            // Create the Guid file
            Logger.Debug("Creating Guid file...");

            string guidFilepath = GetGuidPath(store);
            fileSystem.File.WriteAllText(guidFilepath, store.Guid, Encoding.UTF8);

            // Set the Guid file attributes
            try { // catch exception since readonly attribute is optional.
                System.IO.FileAttributes attr = fileSystem.File.GetAttributes(guidFilepath);
                attr |= System.IO.FileAttributes.ReadOnly;
                fileSystem.File.SetAttributes(guidFilepath, attr);
            } catch (UnauthorizedAccessException e) {
                Logger.Error(e, "Failed to set GUID file attributes when creating a new store: {path} ({label}) {guid} -> {storePath}", store.Path, store.Label, store.Guid, storePath);
            }

            return new WarehouseResult(store, WarehouseStoreStatus.Success);
        }

        /// <summary>
        /// Delete an empty store.      
        /// </summary>
        ///  <returns> 
        ///  A warehouse result containing the status of this operation.
        ///     <br/><br/>Possible status codes are: <br/>
        ///     - <see cref="WarehouseStoreStatus.FailureDatabaseClosed"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailureNotEmpty"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailureFolderNotExists"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailureInvalid"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailureMismatch"/><br/>
        ///     - <see cref="WarehouseStoreStatus.Success"/>
        /// </returns>
        public async Task<WarehouseResult> DeleteStoreAsync(Store store) {
            if (!database.IsOpen)
                return new WarehouseResult(WarehouseStoreStatus.FailureDatabaseClosed);

            if (store.TotalFiles > 0 || store.UsedSpace > 0) // Dont delete stores with files in them for safety.
                return new WarehouseResult(WarehouseStoreStatus.FailureNotEmpty);

            ValidateStore(store);

            // Check if the store has any problems, if it does abort the operation.
            WarehouseResult result = CheckStatus(store);
            if (result != null)
                return result;

            string storePath = ResolveStorePath(store);

            // Check if we have write access.
            if (!fileSystem.IsDirectoryWritable(fileSystem.Directory.GetParent(storePath).FullName))
                return new WarehouseResult(WarehouseStoreStatus.FailureFolderNotWritable);

            if (!fileSystem.Directory.Exists(storePath)) // Check if the doesn't exist.
                return new WarehouseResult(WarehouseStoreStatus.FailureFolderNotExists);

            Logger.Info("Delete store: {path} ({label}) {guid} -> {storePath}", store.Path, store.Label, store.Guid, storePath);

            string guidFilepath = GetGuidPath(store);
            // Set the Guid file attributes
            try { // catch exception since readonly attribute is optional.
                System.IO.FileAttributes attr = fileSystem.File.GetAttributes(guidFilepath);
                attr &= ~System.IO.FileAttributes.ReadOnly;
                fileSystem.File.SetAttributes(guidFilepath, attr);

                Logger.Debug("Unset read-only flag on Guid file...");
            } catch (UnauthorizedAccessException e) {
                Logger.Error(e, "Failed to set GUID file attributes when creating a new store: {path} ({label}) {guid} -> {storePath}", store.Path, store.Label, store.Guid, storePath);
            }

            Logger.Debug("Deleting folders...");
            fileSystem.Directory.Delete(storePath, true);

            Logger.Debug("Updating database...");
            await database.Connection.DeleteAsync(store).ConfigureAwait(false);

            return new WarehouseResult(WarehouseStoreStatus.Success);
        }

        /// <summary>
        /// Edit the specified store by updating the catalog and moving the store if needed.
        /// </summary>
        /// <returns>
        ///  A warehouse result containing the status of this operation.
        ///     <br/><br/>Possible status codes are: <br/>
        ///     - <see cref="WarehouseStoreStatus.FailureDatabaseClosed"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailureFolderNotExists"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailureInvalid"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailureMismatch"/><br/>
        ///     - <see cref="WarehouseStoreStatus.Success"/>
        /// </returns>
        public async Task<WarehouseResult> EditStoreAsync(Store store) {
            if (!database.IsOpen)
                return new WarehouseResult(WarehouseStoreStatus.FailureDatabaseClosed);

            string storePath = ResolveStorePath(store);

            Logger.Info("Edit store: {path} ({label}) {guid} -> {storePath}", store.Path, store.Label, store.Guid, storePath);

            // Get the current store values.
            Store oldStore = await database.Connection.GetAsync<Store>(store.Id).ConfigureAwait(false);

            ValidateStore(oldStore);

            // Check if the current store has any problems, if it does abort the operation.
            WarehouseResult result = CheckStatus(oldStore);
            if (result != null)
                return result;

            string oldStorePath = ResolveStorePath(oldStore);

            // TODO: Check if old path exists.
            // TODO: check if new path exists.

            if (oldStorePath != storePath) {
                // TODO: Move files with progress.
            }

            //     await database.Connection.UpdateAsync(store).ConfigureAwait(false);
            if (store.IsDefault)
                await EnsureDefaultStoreAsync(store);

            return new WarehouseResult(WarehouseStoreStatus.Failure);
        }

        public Task<WarehouseResult> UpdateStoreStatisticsAsync(Store storageLocation) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns all store. Ensures each store Status property has been updated.
        /// </summary>
        /// <returns>A new list of all stores, or a new empty list if none exist.</returns>
        public async Task<List<Store>> GetStoresAsync() {
            if (!database.IsOpen)
                return new List<Store>();

            List<Store> results = await database.Connection.Table<Store>().ToListAsync().ConfigureAwait(false);

            // Update store status based on GUID and directory existance.
            results.ForEach(ValidateStore);

            return results;
        }

        /// <summary>
        /// Parses the given filepath to determine if it is suitable for importing. Returns an ImportItem if suitable.
        /// </summary>
        /// <returns>
        ///  A warehouse result containing the status of this operation, and an ImportItem if success.
        ///     <br/><br/>Possible status codes are: <br/>
        ///     - <see cref="WarehouseStoreStatus.FailureDatabaseClosed"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailureFileNotExists"/><br/>
        ///     - <see cref="WarehouseStoreStatus.FailureMimeTypeUnknown"/><br/>
        ///     - <see cref="WarehouseStoreStatus.Success"/>
        /// </returns>
        public async Task<WarehouseResult> ParseFileAsync(string filepath, CancellationToken ct) {
            if (!database.IsOpen)
                return new WarehouseResult(WarehouseStoreStatus.FailureDatabaseClosed);

            if (!fileSystem.File.Exists(filepath))
                return new WarehouseResult(WarehouseStoreStatus.FailureFileNotExists);

            string extension = fileSystem.Path.GetExtension(filepath);

            Mime mime = await database.Connection.Table<Mime>().FirstOrDefaultAsync(x => x.Extension == extension).ConfigureAwait(false);
            if (mime == null)
                return new WarehouseResult(WarehouseStoreStatus.FailureMimeTypeUnknown);

            long filesize = fileSystem.FileInfo.FromFileName(filepath).Length;

#if DEBUG
            await Task.Delay(1, ct).ContinueWith(t => { }).ConfigureAwait(false);
#endif

            return new WarehouseResult(new ImportItem { Filepath = filepath, Filesize = filesize, Mime = mime }, WarehouseStoreStatus.Success);
        }

        // TODO: Clean up
        // TODO: Add checks
        public async Task<WarehouseResult> ImportFileAsync(Store store, ImportItem item, CancellationToken ct) {
            if (!database.IsOpen)
                return new WarehouseResult(WarehouseStoreStatus.FailureDatabaseClosed);

            if (!fileSystem.File.Exists(item.Filepath))
                return new WarehouseResult(WarehouseStoreStatus.FailureFileNotExists);

            ValidateStore(store);
            WarehouseResult result = CheckStatus(store);
            if (result != null)
                return result;


            string hash = fileSystem.GetHash<SHA256CryptoServiceProvider>(item.Filepath, ct: ct);
            if (ct.IsCancellationRequested) 
                return new WarehouseResult(WarehouseStoreStatus.Failure);
            

            string extension = fileSystem.Path.GetExtension(item.Filepath);

            Record record = new Record {
                Storage = store,
                Hash = hash,
                Extension = extension,
                Size = item.Filesize,
                Mime = item.Mime,
                ImportedOn = DateTime.Now
            };

            await database.Connection.InsertAsync(record);

            string dstPath = ResolveRecordFilepath(record);

            if (!fileSystem.File.Exists(dstPath)) {
                Logger.Info("Copying: {src} -> {dst}", item.Filesize, dstPath);
                fileSystem.File.Copy(item.Filepath, dstPath);
            }

            store.TotalFiles++;
            store.UsedSpace += item.Filesize;

            await database.Connection.UpdateAsync(store);

            return new WarehouseResult(record, WarehouseStoreStatus.Success);
        }

        /// <summary>
        /// Validates the specified store by checking if the location, structure, and GUID match the specified store. 
        /// Reads the GUID file and checks if all expected folders exist.
        /// </summary>
        private void ValidateStore(Store store) {
            string storePath = ResolveStorePath(store);

            if (!fileSystem.Directory.Exists(storePath)) { // The store directory doesn't exist.
                store.Status = StoreStatus.Missing;

            } else {
                string guidFilepath = GetGuidPath(store);

                if (!fileSystem.File.Exists(guidFilepath)) { // GUID file doesn't exist.
                    store.Status = StoreStatus.Invalid;
                } else {
                    string guid = fileSystem.File.ReadAllText(guidFilepath, Encoding.UTF8);

                    if (!guid.Equals(store.Guid, StringComparison.Ordinal)) { // GUID doesn't match.
                        store.Status = StoreStatus.Mismatch;
                    } else {
                        // Check if all partition folders exist within the store.
                        store.Status = GetPartitionFolders(store).All(x => fileSystem.Directory.Exists(x)) ? StoreStatus.Ok : StoreStatus.Invalid;
                    }

                }

            }

        }

        public string ResolveRecordFilepath(Record record) {
            string storePath = ResolveStorePath(record.Storage);

            string partition = record.Hash.Substring(0, 2);

            return fileSystem.Path.Combine(storePath, partition, record.Filename);
        }

        /// <summary>
        /// Returns an enumerable of filepaths to the specified store partition folders. Directories returned aren't guaranteed to exist.
        /// </summary>
        private IEnumerable<string> GetPartitionFolders(Store store) {
            string storePath = ResolveStorePath(store);

            return Enumerable.Range(0, 0xFF + 1).Select(i => i.ToString("x2")).SelectMany(x => {
                string fileFolderPath = fileSystem.Path.Combine(storePath, x);
                string thumbFolderPath = fileSystem.Path.Combine(storePath, $"t{x}");
                return Enumerable.Empty<string>().Append(fileFolderPath).Append(thumbFolderPath);
            });
        }

        /// <summary>
        /// Ensures that only one store is set to default. The specified store is the new default store.
        /// </summary>
        private async Task EnsureDefaultStoreAsync(Store defaultStore) {
            List<Store> storageLocations = await database.Connection.Table<Store>().Where(x => x.IsDefault && x.Id != defaultStore.Id).ToListAsync().ConfigureAwait(false);
            storageLocations.ForEach(x => x.IsDefault = false);

            defaultStore.IsDefault = true;
            storageLocations.Add(defaultStore);

            await database.Connection.UpdateAllAsync(storageLocations);
        }

        public string ResolveStorePath(Store store) {
            return ResolveStorePath(store.Path);
        }

        public string ResolveStorePath(string storePath) {
            bool isAbsolute = fileSystem.Path.IsFullPath(storePath);

            string path = storePath;
            if (!isAbsolute) {
                string catalogDirectory = fileSystem.Path.GetDirectoryName(fileSystem.Path.GetFullPath(database.Filepath));
                path = fileSystem.Path.Combine(catalogDirectory, storePath);
            }

            return fileSystem.Path.GetFullPath(path);
        }

        /// <summary>
        /// Returns the guid file path for the specified store.
        /// </summary>
        private string GetGuidPath(Store store) {
            string storePath = ResolveStorePath(store);
            return fileSystem.Path.Combine(storePath, "___store___.guid");
        }

        private static WarehouseResult CheckStatus(Store store) {
            switch (store.Status) {
                case StoreStatus.Missing:
                    return new WarehouseResult(WarehouseStoreStatus.FailureFolderNotExists);
                case StoreStatus.Invalid:
                    return new WarehouseResult(WarehouseStoreStatus.FailureInvalid);
                case StoreStatus.Mismatch:
                    return new WarehouseResult(WarehouseStoreStatus.FailureMismatch);
                default:
                    return null;
            }
        }

    }

}
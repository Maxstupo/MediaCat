namespace MediaCat.Core.Services.Catalog.Database {
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using MediaCat.Core.Model;
    using MediaCat.Core.Utility;
    using MediaCat.Core.Utility.Extensions;
    using SQLite;
    using SQLiteNetExtensionsAsync.Extensions;

    public sealed class SQLiteDatabaseCatalog : ICatalog, IDatabase {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly SQLiteOpenFlags OpenFlags = SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite;

        public string Filepath => connection?.DatabasePath ?? null;

        public bool IsOpen => connection != null;


        private SQLiteAsyncConnection connection;

        private readonly IFileSystem fileSystem;


        public event EventHandler OnConnectionStateChanged;


        public SQLiteDatabaseCatalog(IFileSystem fileSystem) {
            this.fileSystem = fileSystem;
        }

        #region Connection/Creation Methods

        private static async Task CreateTablesAsync(SQLiteAsyncConnection connection) {

            CreateTableResult status = await connection.CreateTableAsync<Mime>().ConfigureAwait(false);

            if (status == CreateTableResult.Created) {
                Logger.Debug("Defining default catalog mimes types...");
                await connection.InsertAllAsync(MimeTypesMap.MimeTypes.Where(x => x.Value.StartsWith("video") || x.Value.StartsWith("image") || x.Value.StartsWith("audio")).Select(x => new Mime {
                    Extension = $".{x.Key}",
                    Type = x.Value,
                    Label = string.Empty,
                    Viewer = 0
                }));
            }


            await connection.CreateTableAsync<StorageLocation>().ConfigureAwait(false);
            await connection.CreateTableAsync<File>().ConfigureAwait(false);

        }

        public async Task<CatalogStatus> CreateAsync(string path) {
            if (fileSystem.File.Exists(path))
                return CatalogStatus.FailureHasCatalog;

            Logger.Debug("Creating SQLite database...");

            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(path, OpenFlags);

            await CreateTablesAsync(connection).ConfigureAwait(false);

            await connection.CloseAsync().ConfigureAwait(false);

            return CatalogStatus.Success;
        }

        public async Task<CatalogStatus> OpenAsync(string path) {
            if (IsOpen)
                return CatalogStatus.FailureOpen;

            path = fileSystem.Path.GetFullPath(path);

            if (!fileSystem.File.Exists(path))
                return CatalogStatus.FailureNoCatalog;

            Logger.Debug("Opening SQLite database...");

            connection = new SQLiteAsyncConnection(path);

            await CreateTablesAsync(connection).ConfigureAwait(false);

            NotifyConnectionStateChanged();

            return CatalogStatus.Success;
        }

        public async Task<CatalogStatus> CloseAsync() {
            if (!IsOpen)
                return CatalogStatus.FailureClosed;

            Logger.Debug("Closing SQLite database...");

            await connection.CloseAsync().ConfigureAwait(false);

            connection = null;

            NotifyConnectionStateChanged();

            return CatalogStatus.Success;
        }

        #endregion

        #region Storage Location Operations

        /// <summary>Returns the full filepath to the GUID file of the specified StorageLocation.</summary>
        public string GetStorageLocationGuidPath(StorageLocation location) {
            string storePath = ResolveStorageLocationPath(location);
            return fileSystem.Path.Combine(storePath, "storage_location.guid");
        }

        /// <inheritdoc cref="ResolveStorageLocationPath(string)"/>
        public string ResolveStorageLocationPath(StorageLocation location) {
            return ResolveStorageLocationPath(location.Path);
        }

        // Converts the potentially relative storage location path into an absolute filepath based on the loaded catalog directory.
        public string ResolveStorageLocationPath(string locationPath) {
            bool isAbsolute = fileSystem.Path.IsFullPath(locationPath);

            string path = locationPath;
            if (!isAbsolute) {
                string catalogDirectory = fileSystem.Path.GetDirectoryName(fileSystem.Path.GetFullPath(Filepath));
                path = fileSystem.Path.Combine(catalogDirectory, locationPath);
            }

            return fileSystem.Path.GetFullPath(path);
        }

        public async Task<CatalogStorageResult> CreateStoreAsync(string label, string path, bool isDefault) {
            if (!IsOpen)
                return new CatalogStorageResult(StorageLocationStatus.Failure);

            if (string.IsNullOrWhiteSpace(label))
                label = path;

            // Check if the label is already registered.
            if (await connection.Table<StorageLocation>().Where(x => x.Label == label).CountAsync() != 0)
                return new CatalogStorageResult(StorageLocationStatus.LabelExists);

            // Check if the path is already registered.
            if (await connection.Table<StorageLocation>().Where(x => x.Path == path).CountAsync() != 0)
                return new CatalogStorageResult(StorageLocationStatus.PathExists);

            StorageLocation location = new StorageLocation {
                Label = label,
                GUID = Guid.NewGuid().ToString(),
                Path = path,
                IsDefault = isDefault,
                Status = StoreStatus.Ok
            };
            string storePath = ResolveStorageLocationPath(location); // the true storage location folder path.

            Logger.Debug("{path} -> {storePath}", location.Path, storePath);

            if (fileSystem.Directory.Exists(storePath)) // Check if folder exists.
                return new CatalogStorageResult(StorageLocationStatus.DirectoryExists);

            Logger.Info("Create storage location: {path} ({label}) {guid}", location.Path, location.Label, location.GUID);

            // If this new location is default, make all other storage locations not default.
            if (location.IsDefault) {
                List<StorageLocation> storageLocations = await connection.Table<StorageLocation>().Where(x => x.IsDefault).ToListAsync();
                storageLocations.ForEach(x => x.IsDefault = false);

                await connection.UpdateAllAsync(storageLocations);
            } else {
                // Make this storage location the default if no default store exists.
                location.IsDefault = await connection.Table<StorageLocation>().Where(x => x.IsDefault).CountAsync() == 0;
            }

            Logger.Trace("Updating database...");
            await connection.InsertAsync(location);

            // create folder // TODO: Check if we have write access.
            fileSystem.Directory.CreateDirectory(storePath);

            // create sub folders
            Logger.Debug("Creating directory structure...");
            Enumerable.Range(0, 0xFF + 1).Select(i => i.ToString("x2")).ForEach(i => {
                string fileFolderPath = fileSystem.Path.Combine(storePath, i);
                string thumbFolderPath = fileSystem.Path.Combine(storePath, $"t{i}");
                fileSystem.Directory.CreateDirectory(fileFolderPath);
                fileSystem.Directory.CreateDirectory(thumbFolderPath);
            });

            Logger.Debug("Creating GUID file...");
            string guidFilepath = GetStorageLocationGuidPath(location);
            fileSystem.File.WriteAllText(guidFilepath, location.GUID, Encoding.UTF8);

            // Set the file attributes
            try { // catch exception since readonly attribute is optional.
                System.IO.FileAttributes attr = fileSystem.File.GetAttributes(guidFilepath);
                attr |= System.IO.FileAttributes.ReadOnly;
                fileSystem.File.SetAttributes(guidFilepath, attr);
            } catch (UnauthorizedAccessException e) {
                Logger.Error(e, "Failed to set storage location GUID file attributes when creating new storage loation: {path} ({label}) {guid}", location.Path, location.Label, location.GUID);
            }

            return new CatalogStorageResult(location, StorageLocationStatus.Success);
        }

        public async Task<CatalogStorageResult> DeleteStoreAsync(StorageLocation storageLocation) {
            if (storageLocation.TotalFiles > 0 || storageLocation.UsedSpace > 0)
                return new CatalogStorageResult(StorageLocationStatus.NotEmpty);

            ValidateStorageLocation(storageLocation);

            switch (storageLocation.Status) {
                case StoreStatus.Missing:
                    return new CatalogStorageResult(StorageLocationStatus.DirectoryNotExists);
                case StoreStatus.Invalid:
                    return new CatalogStorageResult(StorageLocationStatus.FailureInvalid);
                case StoreStatus.Mismatch:
                    return new CatalogStorageResult(StorageLocationStatus.FailureMismatch);
                default: break;
            }

            Logger.Info("Delete storage location: {path} ({label}) {guid}", storageLocation.Path, storageLocation.Label, storageLocation.GUID);

            string storePath = ResolveStorageLocationPath(storageLocation);

            Logger.Debug("Unsetting read-only on GUID file...");
            string guidFilepath = GetStorageLocationGuidPath(storageLocation);
            // Set the file attributes
            try { // catch exception since readonly attribute is optional.
                System.IO.FileAttributes attr = fileSystem.File.GetAttributes(guidFilepath);
                attr &= ~System.IO.FileAttributes.ReadOnly;
                fileSystem.File.SetAttributes(guidFilepath, attr);
            } catch (UnauthorizedAccessException e) {
                Logger.Error(e, "Failed to set storage location GUID file attributes when deleting storage loation: {path} ({label}) {guid}", storageLocation.Path, storageLocation.Label, storageLocation.GUID);
            }

            Logger.Trace("Updating database...");
            // TODO: Check if we have write access
            fileSystem.Directory.Delete(storePath, true);

            await connection.DeleteAsync(storageLocation);

            return new CatalogStorageResult(StorageLocationStatus.Success);
        }

        public async Task<List<StorageLocation>> GetStoresAsync() {
            List<StorageLocation> results = await connection.Table<StorageLocation>().ToListAsync();

            // Update store status based on GUID and directory existance.
            results.ForEach(storageLocation => ValidateStorageLocation(storageLocation));

            return results;
        }

        public StorageLocation ValidateStorageLocation(StorageLocation storageLocation) {
            string storePath = ResolveStorageLocationPath(storageLocation);
            if (!fileSystem.Directory.Exists(storePath)) {
                storageLocation.Status = StoreStatus.Missing;
            } else {
                string guidFilepath = GetStorageLocationGuidPath(storageLocation);
                if (!fileSystem.File.Exists(guidFilepath)) {
                    storageLocation.Status = StoreStatus.Invalid;
                } else {
                    string guid = fileSystem.File.ReadAllText(guidFilepath, Encoding.UTF8);
                    storageLocation.Status = guid.Equals(storageLocation.GUID, StringComparison.Ordinal) ? StoreStatus.Ok : StoreStatus.Mismatch;
                }
            }
            return storageLocation;
        }

        #endregion

        private void NotifyConnectionStateChanged() {
            OnConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose() {
            if (!IsOpen)
                return;

            Logger.Debug("Disposing SQLite database...");

            connection.GetConnection().Close();
            connection = null;

            NotifyConnectionStateChanged();
        }


    }

}
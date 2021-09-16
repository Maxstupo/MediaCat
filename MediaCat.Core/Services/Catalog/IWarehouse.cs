namespace MediaCat.Core.Services.Catalog {
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using MediaCat.Core.Model;

    [Flags]
    public enum WarehouseStoreStatus {
        None = 1 << 0,

        /// <summary>The status is considered a success.</summary>
        Success = 1 << 1,

        /// <summary>The status is considered a failure.</summary>
        Failure = 1 << 2,

        Exists = 1 << 3,
        Not = 1 << 4,

        DatabaseClosed = 1 << 5,
        Label = 1 << 6,
        Path = 1 << 7,
        Folder = 1 << 8,
        File = 1 << 9,
        Empty = 1 << 10,
        Invalid = 1 << 11,
        Mismatch = 1 << 12,
        MimeType = 1 << 13,
        Writable = 1 << 14,

        /// <summary>The operation required the database to be open.</summary>
        FailureDatabaseClosed = Failure | DatabaseClosed,

        /// <summary>The operation couldn't proceed since a label with that name already existed.</summary>
        FailureLabelAlreadyExists = Failure | Label | Exists,

        /// <summary>The operation couldn't proceed since the specified path already existed.</summary>
        FailurePathAlreadyExists = Failure | Path | Exists,

        /// <summary>The operation couldn't proceed since the directory already existed.</summary>
        FailureFolderAlreadyExists = Failure | Folder | Exists,

        /// <summary>The operation couldn't proceed since the directory doesn't exist.</summary>
        FailureFolderNotExists = Failure | Folder | Not | Exists,

        /// <summary>The operation couldn't proceed since the store wasn't empty.</summary>
        FailureNotEmpty = Failure | Not | Empty,

        /// <summary>The operation couldn't proceed since the store was invalid.</summary>
        FailureInvalid = Failure | Invalid,

        /// <summary>The operation couldn't proceed since the store doesn't match the requested store (GUID).</summary>
        FailureMismatch = Failure | Mismatch,

        /// <summary>The operation couldn't proceed since the mime type wasn't known.</summary>
        FailureMimeTypeUnknown = Failure | MimeType,

        /// <summary>The operation couldn't proceed since the file doesn't exist.</summary>
        FailureFileNotExists = Failure | File | Not | Exists,

        /// <summary>The operation couldn't proceed since the folder isn't writable.</summary>
        FailureFolderNotWritable = Failure | Folder | Not | Writable

    }

    public sealed class WarehouseResult {

        public Store Store { get; }

        public ImportItem ImportItem { get; }

        public Record File { get; }

        public WarehouseStoreStatus Status { get; }

        public WarehouseResult(WarehouseStoreStatus status) : this(null, null, null, status) { }

        public WarehouseResult(Store store, WarehouseStoreStatus status) : this(null, store, null, status) { }
        public WarehouseResult(Record file, WarehouseStoreStatus status) : this(file, null, null, status) { }

        public WarehouseResult(ImportItem importItem, WarehouseStoreStatus status) : this(null, null, importItem, status) { }

        private WarehouseResult(Record file, Store store, ImportItem importItem, WarehouseStoreStatus status) {
            this.File = file;
            this.Store = store;
            this.ImportItem = importItem;
            this.Status = status;
        }

    }


    /// <summary>
    /// Represents an implementation that can interact with the stores for holding the actual imported files of the MediaCat catalog.
    /// </summary>
    public interface IWarehouse {

        /// <summary>
        /// Create a new store at the provided path.
        /// </summary>
        /// <param name="label">The cosmetic label of this store.</param>
        /// <param name="path">The folderpath of this store. Supports absolute, relative, and network paths.</param>
        /// <param name="isDefault">True if this new store should be the default selected.</param>
        /// <returns>A warehouse result containing the newly created StorageLocation and the status of this operation.</returns>
        Task<WarehouseResult> CreateStoreAsync(string label, string path, bool isDefault);

        /// <summary>
        /// Edit the specified store by updating the catalog and moving the store if needed.
        /// </summary>
        Task<WarehouseResult> EditStoreAsync(Store store);

        /// <summary>
        /// Delete an empty store.      
        /// </summary>
        /// <returns> 
        /// A warehouse result containing the status of this operation.
        /// </returns>
        Task<WarehouseResult> DeleteStoreAsync(Store store);

        //Task<WarehouseResult> MergeStoreAsync(StorageLocation source, StorageLocation destination);


        /// <summary>
        /// Returns all stores.
        /// </summary>
        /// <returns>A new list of all stores, or a new empty list if none exist.</returns>
        Task<List<Store>> GetStoresAsync();

        Task<WarehouseResult> UpdateStoreStatisticsAsync(Store store);

        /// <summary>
        /// Parses the given filepath to determine if it is suitable for importing. Returns an ImportItem if suitable.
        /// </summary>
        /// <returns>
        /// A warehouse result containing the status of this operation and an ImportItem.
        /// </returns>    
        Task<WarehouseResult> ParseFileAsync(string filepath, CancellationToken ct);

        Task<WarehouseResult> ImportFileAsync(Store store, ImportItem item, CancellationToken ct);

        /// <summary>
        /// Validates the specified store by checking if the location, structure, and GUID match the specified store. 
        /// </summary>
        void ValidateStore(Store store);

        /// <summary>
        /// Returns the absolute filepath of the specified store. 
        /// Resolving relative paths based on the catalog filepath.
        /// </summary>
        string ResolveStorePath(Store store);

        /// <inheritdoc cref="ResolveStorePath(Store)"/>
        string ResolveStorePath(string storePath);

    }

}

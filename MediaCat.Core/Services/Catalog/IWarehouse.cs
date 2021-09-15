namespace MediaCat.Core.Services.Catalog {
    using System;
    using System.Collections.Generic;
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
        Empty = 1 << 9,
        Invalid = 1 << 10,
        Mismatch = 1 << 11,

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
    }

    public sealed class WarehouseResult {
        public Store Store { get; }
        public WarehouseStoreStatus Status { get; }

        public WarehouseResult(WarehouseStoreStatus status) : this(null, status) { }

        public WarehouseResult(Store store, WarehouseStoreStatus status) {
            this.Store = store;
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
        /// <returns>A catalog storage result containing the newly created StorageLocation and the status of this operation.</returns>
        Task<WarehouseResult> CreateStoreAsync(string label, string path, bool isDefault);

        /// <summary>
        /// Edit the specified store by updating the catalog and moving the store if needed.
        /// </summary>
        Task<WarehouseResult> EditStoreAsync(Store store);

        /// <summary>
        /// Delete an empty store.      
        /// </summary>
        /// <returns> 
        /// A catalog storage result containing the status of this operation.
        /// </returns>
        Task<WarehouseResult> DeleteStoreAsync(Store store);

        //Task<CatalogStorageResult> MergeStoreAsync(StorageLocation source, StorageLocation destination);


        /// <summary>
        /// Returns all stores.
        /// </summary>
        /// <returns>A new list of all stores, or a new empty list if none exist.</returns>
        Task<List<Store>> GetStoresAsync();

        Task<WarehouseResult> UpdateStoreStatisticsAsync(Store store);

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

namespace MediaCat.Core.Services.Catalog {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using MediaCat.Core.Model;

    public struct CatalogStorageResult {
        public StorageLocation StorageLocation { get; }
        public StorageLocationStatus Status { get; set; }

        public CatalogStorageResult(StorageLocationStatus status) : this(null, status) {
        }

        public CatalogStorageResult(StorageLocation storageLocation, StorageLocationStatus status) {
            this.StorageLocation = storageLocation;
            this.Status = status;
        }

    }

    public interface ICatalog {

        #region Storage Location Operations

        //Task<List<StorageLocation>> ValidateStoresAsync();

        /// <summary>
        /// Create a new storage location at the provided path.
        /// </summary>
        /// <param name="label">The cosmetic label of this storage location.</param>
        /// <param name="path">The folderpath of this store. Supports absolute, relative, and network paths.</param>
        /// <param name="isDefault">True if this new storage location should be the default selected.</param>
        /// <returns>A catalog storage result containing the newly created StorageLocation and the status of this methods operation.</returns>
        Task<CatalogStorageResult> CreateStoreAsync(string label, string path, bool isDefault);

        //   Task<CatalogStorageResult> MoveStoreAsync(StorageLocation source, StorageLocation destination);

        Task<CatalogStorageResult> DeleteStoreAsync(StorageLocation storageLocation);

        //    Task<CatalogStorageResult> MergeStoreAsync(StorageLocation source, StorageLocation destination);

        //    Task<CatalogStorageResult> RemapStoreAsync(StorageLocation source);

        Task<List<StorageLocation>> GetStoresAsync();

        /// <summary>Retruns the absolute filepath of the specified storage location path, by optionally resolving the relative storage location path based on the catalog filepath.</summary>
        string ResolveStorageLocationPath(string locationPath);

        #endregion

    }

}

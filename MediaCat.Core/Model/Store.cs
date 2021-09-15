namespace MediaCat.Core.Model {
    using SQLite;

    /// <summary>
    /// The current status of a Store. Indicates if the store is missing, mismatched GUID, or invalid folder structure.
    /// </summary>
    public enum StoreStatus : int {
        /// <summary>Everything is fine.</summary>
        Ok = 0,
        /// <summary>The store doesn't exist at the specified path.</summary>
        Missing = 1,
        /// <summary>The store GUID and the actual GUID in the folder don't match.</summary>
        Mismatch = 2,
        /// <summary>The store is missing the GUID file or the folder structure is incorrect.</summary>
        Invalid = 3
    }


    public sealed class Store {

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>The globally unique identifier that identifies this store. A copy of this GUID is placed within the managed folder.</summary>
        public string Guid { get; set; }

        /// <summary>The cosmetic label of this store.</summary>
        [Unique]
        public string Label { get; set; }

        /// <summary>The folder path to the store. This can be an absolute or relative path (to the catalog file).</summary>
        [Indexed]
        public string Path { get; set; }

        /// <summary>If true this store should be used by default, if none is specified when importing files.</summary>
        public bool IsDefault { get; set; }

        /// <summary>The total number of files stored in this store.</summary>
        public long TotalFiles { get; set; }

        /// <summary>The total number of bytes all files within this store use.</summary>
        public long UsedSpace { get; set; }

        [Ignore]
        public StoreStatus Status { get; set; }

    }

}

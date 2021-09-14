namespace MediaCat.Core.Model {
    using SQLite;

    public enum StoreStatus : int {
        /// <summary>Everything is fine.</summary>
        Ok = 0,
        /// <summary>The storage location doesn't exist at the specified path.</summary>
        Missing = 1,
        /// <summary>The storage location GUID and the actual GUID in the folder don't match.</summary>
        Mismatch = 2,
        /// <summary>The storage location is missing the GUID file or the folder structure is incorrect.</summary>
        Invalid = 3
    }


    public sealed class StorageLocation {

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>The globally unique identifier that identifies this storage location. A copy of this GUID is placed within the managed folder.</summary>
        public string GUID { get; set; }

        /// <summary>The cosmetic label of this storage location</summary>
        [Unique]
        public string Label { get; set; }

        /// <summary>The folder path to the storage location. This can be an absolute or relative path (to the catalog file).</summary>
        [Indexed]
        public string Path { get; set; }

        /// <summary>If true this storage location should be used by default, if none is specified when importing files.</summary>
        public bool IsDefault { get; set; }

        /// <summary>The total number of files stored in this storage location.</summary>
        public long TotalFiles { get; set; }

        /// <summary>The total number of bytes all files within this storage location use.</summary>
        public long UsedSpace { get; }

        [Ignore]
        public StoreStatus Status { get; set; }

    }

}

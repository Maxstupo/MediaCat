namespace MediaCat.Core.Model {
    using System;
    using SQLite;
    using SQLiteNetExtensions.Attributes;

    /// <summary></summary>
    public sealed class File {

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>The storage location id this file is located in.</summary>
        [ForeignKey(typeof(StorageLocation))]
        public int StorageId { get; set; }

        /// <summary>The storage location this file is located in.</summary>
        [OneToOne]
        public StorageLocation Storage { get; set; }

        /// <summary>The original filepath this file was located at before being imported. If the storage location isn't defined, this filepath will be used instead.</summary>
        public string ImportFilepath { get; set; }

        [Ignore]
        public string Filename => $"{Hash}{Extension}";

        /// <summary>The SHA-256 file hash.</summary>
        [Indexed]
        public string Hash { get; set; }

        /// <summary>The file extension with separator (e.g. ".png").</summary>
        public string Extension { get; set; }

        /// <summary>The identified mime type id of this file based on the file extension.</summary>
        [ForeignKey(typeof(Mime))]
        public int MimeId { get; set; }

        /// <summary>The identified mime type of this file based on the file extension.</summary>
        [OneToOne]
        public Mime Mime { get; set; }

        /// <summary>The file size in bytes.</summary>
        public long Size { get; set; }

        /// <summary>The date and time when this file was imported into the MediaCat catalog.</summary>
        public DateTime ImportedOn { get; set; }

    }

}
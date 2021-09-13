namespace MediaCat.Core.Model {
    using SQLite;

    /// <summary>Represents a mime type and file extension. Allows for different media viewers based on mime type.</summary>
    public sealed class Mime {

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        /// <summary>The cosmetic name of this mime type.</summary>
        public string Label { get; set; }

        /// <summary>The mime type in the format of "category/extension"</summary>
        [Indexed]
        public string Type { get; set; }

        /// <summary>The file extension this mime type represents.</summary>
        [Indexed]
        public string Extension { get; set; }

        /// <summary>The media viewer implementation id associated with this mime type.</summary>
        public int Viewer { get; set; }


    }

}

namespace MediaCat.ViewModels {
    using System;
    using MediaCat.Core.Model;
    using Stylet;

    public sealed class ImportItemViewModel : PropertyChangedBase {

        public int Index { get; set; }

        public string Filepath => ImportItem.Filepath;

        public long Filesize => ImportItem.Filesize;

        public string MimeType => ImportItem.Mime.Type;

        public string Status { get; set; }

        public DateTime ImportedOn { get; set; }

        public ImportItem ImportItem { get; }

        public ImportItemViewModel(ImportItem importItem) {
            this.ImportItem = importItem ?? throw new ArgumentNullException(nameof(importItem));
        }

    }

}
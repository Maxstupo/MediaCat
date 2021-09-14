namespace MediaCat.ViewModels {
    using System;
    using System.Collections.Generic;
    using MediaCat.Core.Model;

    public class StorageLocationViewModel : IEquatable<StorageLocationViewModel> {
      
        public string Status => Storage.Status.ToString();

        public string Label => Storage.Label;

        public string Path => Storage.Path;

        public bool IsDefault => Storage.IsDefault;

        public long TotalFiles => Storage.TotalFiles;

        public long UsedSpace => Storage.UsedSpace;

        public StorageLocation Storage { get; } // The model is public only for interacting with the repository.

        public StorageLocationViewModel(StorageLocation location) {
            this.Storage = location;
        }

        public override bool Equals(object obj) {
            return Equals(obj as StorageLocationViewModel);
        }

        public bool Equals(StorageLocationViewModel other) {
            return other != null &&
                   EqualityComparer<StorageLocation>.Default.Equals(this.Storage, other.Storage);
        }

        public override int GetHashCode() {
            return 1612953078 + EqualityComparer<StorageLocation>.Default.GetHashCode(this.Storage);
        }

        public static bool operator ==(StorageLocationViewModel left, StorageLocationViewModel right) {
            return EqualityComparer<StorageLocationViewModel>.Default.Equals(left, right);
        }

        public static bool operator !=(StorageLocationViewModel left, StorageLocationViewModel right) {
            return !(left == right);
        }

    }

}
namespace MediaCat.ViewModels {
    using System;
    using System.Collections.Generic;
    using MediaCat.Core.Model;
    using Stylet;

    public class StoreItemViewModel : PropertyChangedBase, IEquatable<StoreItemViewModel> {

        public string Status => Store.Status.ToString();

        public string Label => Store.Label;

        public string Path => Store.Path;

        public bool IsDefault => Store.IsDefault;

        public long TotalFiles => Store.TotalFiles;

        public long UsedSpace => Store.UsedSpace;

        public Store Store { get; } // The model is public only for interacting with the warehouse.

        public StoreItemViewModel(Store store) {
            this.Store = store;
        }

        public void NotifyAllPropertiesChanged() {
            base.NotifyOfPropertyChange(null);
        }

        public override bool Equals(object obj) {
            return Equals(obj as StoreItemViewModel);
        }

        public bool Equals(StoreItemViewModel other) {
            return other != null &&
                   EqualityComparer<Store>.Default.Equals(this.Store, other.Store);
        }

        public override int GetHashCode() {
            return 1612953078 + EqualityComparer<Store>.Default.GetHashCode(this.Store);
        }

        public static bool operator ==(StoreItemViewModel left, StoreItemViewModel right) {
            return EqualityComparer<StoreItemViewModel>.Default.Equals(left, right);
        }

        public static bool operator !=(StoreItemViewModel left, StoreItemViewModel right) {
            return !(left == right);
        }

    }

}
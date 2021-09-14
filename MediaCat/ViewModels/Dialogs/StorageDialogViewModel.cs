namespace MediaCat.ViewModels.Dialogs {
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using MediaCat.Core.Model;
    using MediaCat.Core.Services.Catalog;
    using MediaCat.Core.Services.Localization;
    using MediaCat.ViewModels;
    using Stylet;

    public sealed class StorageDialogViewModel : ViewModelBase, ICanRefreshData {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IWindowManager windowManager;
        private readonly ICatalog catalog;

        private readonly AddStoreDialogViewModel storeDialogViewModel;

        public BindableCollection<StorageLocationViewModel> Items { get; } = new BindableCollection<StorageLocationViewModel>();

        public bool IsRefreshingData { get; private set; }


        public StorageDialogViewModel(II18N i18n, IWindowManager windowManager, ICatalog catalog, AddStoreDialogViewModel storeDialogViewModel) : base(i18n) {
            this.windowManager = windowManager;
            this.catalog = catalog;
            this.storeDialogViewModel = storeDialogViewModel;
        }

        public StorageDialogViewModel() : base() {

        }

        public bool CanRefreshDataAsync => !IsRefreshingData;
        public async Task RefreshDataAsync() {
            IsRefreshingData = true;
            {
                Items.Clear();

#if DEBUG // TEMP: DEBUG DELAY
                await Task.Delay(250);
#endif

                List<StorageLocation> stores = await catalog.GetStoresAsync();
                Items.AddRange(stores.Select(x => new StorageLocationViewModel(x)));
            }
            IsRefreshingData = false;

            NotifyOfPropertyChange(nameof(Items)); // Update column widths.
        }

        public bool CanAddStore => !IsRefreshingData;
        public void AddStore() {
            if (windowManager.ShowDialog(storeDialogViewModel, this).GetValueOrDefault()) 
                Items.Add(new StorageLocationViewModel(storeDialogViewModel.Result));            
        }

        public bool CanMoveStore => false;
        public void MoveStore() { }

        public bool CanMergeStore => false;
        public void MergeStore() { }

        public bool CanDeleteStore => false;
        public void DeleteStore() { }

    }

}
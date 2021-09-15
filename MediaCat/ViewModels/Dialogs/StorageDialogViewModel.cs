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
        private readonly IWarehouse catalog;

        private readonly AddStoreDialogViewModel storeDialogViewModel;


        public StorageLocationViewModel SelectedItem { get; set; }
        public BindableCollection<StorageLocationViewModel> Items { get; } = new BindableCollection<StorageLocationViewModel>();

        public bool IsRefreshingData { get; private set; }


        public StorageDialogViewModel(II18N i18n, IWindowManager windowManager, IWarehouse catalog, AddStoreDialogViewModel storeDialogViewModel) : base(i18n) {
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

                List<Store> stores = await catalog.GetStoresAsync();
                Items.AddRange(stores.Select(x => new StorageLocationViewModel(x)));
            }
            IsRefreshingData = false;

            NotifyOfPropertyChange(nameof(Items)); // Update column widths.
        }

        public bool CanAddStore => !IsRefreshingData;
        public void AddStore() {
            if (windowManager.ShowDialog(storeDialogViewModel, this).GetValueOrDefault())
                Items.Add(new StorageLocationViewModel(storeDialogViewModel.StorageLocation));
        }

        public bool CanEditStore => !IsRefreshingData && SelectedItem != null;
        public async Task EditStore() {
            storeDialogViewModel.EditMode = true;
            storeDialogViewModel.StorageLocation = SelectedItem.Storage;

            if (windowManager.ShowDialog(storeDialogViewModel, this).GetValueOrDefault())
                await RefreshDataAsync();
        }

        public bool CanMergeStore => false;
        public void MergeStore() { }

        public bool CanDeleteStore => !IsRefreshingData && SelectedItem != null;
        public async Task DeleteStore() {
            if (SelectedItem.TotalFiles > 0) {
                windowManager.ShowMessageBox(I18N["dialogs.storage.actions.remove.deletion_with_files"], I18N["dialogs.storage.actions.remove.deletion_with_files.title"], System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Exclamation);

            } else if (windowManager.ShowMessageBox(I18N["dialogs.storage.actions.remove.confirm_delete"], I18N["dialogs.storage.actions.remove.confirm_delete.title"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {
                await catalog.DeleteStoreAsync(SelectedItem.Storage);
                await RefreshDataAsync();
            }
        }

    }

}
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
        private readonly IWarehouse warehouse;

        private readonly AddStoreDialogViewModel addStoreDialogViewModel;


        public StoreItemViewModel SelectedItem { get; set; }
        public BindableCollection<StoreItemViewModel> Items { get; } = new BindableCollection<StoreItemViewModel>();

        public bool IsRefreshingData { get; private set; }


        public StorageDialogViewModel(II18N i18n, IWindowManager windowManager, IWarehouse warehouse, AddStoreDialogViewModel addStoreDialogViewModel) : base(i18n) {
            this.windowManager = windowManager;
            this.warehouse = warehouse;
            this.addStoreDialogViewModel = addStoreDialogViewModel;
        }

        public StorageDialogViewModel() : base() {

        }

        public bool CanRefreshDataAsync => !IsRefreshingData;
        public async Task RefreshDataAsync() {
            Logger.Debug("Refreshing data...");
            IsRefreshingData = true;
            {
                Items.Clear();

#if DEBUG // TEMP: DEBUG DELAY
                await Task.Delay(250);
#endif

                List<Store> stores = await warehouse.GetStoresAsync();
                Items.AddRange(stores.Select(x => new StoreItemViewModel(x)));
            }
            IsRefreshingData = false;

            NotifyOfPropertyChange(nameof(Items)); // Update column widths.
        }

        public bool CanAddStore => !IsRefreshingData;
        public void AddStore() {
            if (windowManager.ShowDialog(addStoreDialogViewModel, this).GetValueOrDefault())
                Items.Add(new StoreItemViewModel(addStoreDialogViewModel.Store));
        }

        public bool CanEditStore => !IsRefreshingData && SelectedItem != null;
        public async Task EditStore() {
            addStoreDialogViewModel.EditMode = true;
            addStoreDialogViewModel.Store = SelectedItem.Store;

            if (windowManager.ShowDialog(addStoreDialogViewModel, this).GetValueOrDefault())
                await RefreshDataAsync();
        }

        public bool CanMergeStore => false;
        public void MergeStore() { }

        public bool CanDeleteStore => !IsRefreshingData && SelectedItem != null;
        public async Task DeleteStore() {
            if (SelectedItem.TotalFiles > 0 || SelectedItem.UsedSpace > 0) { // Notify user we cant delete store with files in it.
                windowManager.ShowMessageBox(
                    I18N["dialogs.storage.actions.remove.deletion_with_files"],
                    I18N["dialogs.storage.actions.remove.deletion_with_files.title"],
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Exclamation);

            } else if (windowManager.ShowMessageBox(I18N["dialogs.storage.actions.remove.confirm_delete"], I18N["dialogs.storage.actions.remove.confirm_delete.title"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning, System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {

            WarehouseResult result=    await warehouse.DeleteStoreAsync(SelectedItem.Store);
                Logger.Debug(result.Status);
                
                await RefreshDataAsync();
            }
        }

        public bool CanMoveStore => false;
        public void MoveStore() { }

    }

}
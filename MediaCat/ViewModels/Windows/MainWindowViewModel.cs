namespace MediaCat.ViewModels.Windows {
    using System;
    using System.Threading.Tasks;
    using MediaCat.Core.Services.Catalog.Database;
    using MediaCat.Core.Services.Localization;
    using MediaCat.Services;
    using MediaCat.ViewModels.Dialogs;
    using MediaCat.ViewModels.Tabs;
    using Stylet;

    public sealed class MainWindowViewModel : ConductorOneActive<ITabPage>, IHandle<ImportEvent> {
        private static readonly string WikiUrl = @"https://github.com/Maxstupo/MediaCat";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IFileFolderDialog fileFolderDialog;
        private readonly IWindowManager windowManager;
        private readonly IDatabase database;

        private readonly StorageDialogViewModel storageDialogViewModel;
        private readonly ImportDialogViewModel importDialogViewModel;
        private readonly Func<SearchTabViewModel> searchTabViewModelFactory;

        public new string DisplayName {
            get {
                if (database.IsOpen) { // TODO: Provide app version to I18N title
                    return I18N.Get("windows.main.title", I18N["windows.main.title.separator"], database.Filepath);
                } else {
                    return I18N.Get("windows.main.title", string.Empty, string.Empty);
                }
            }
        }


        public MainWindowViewModel(II18N i18n, IFileFolderDialog fileFolderDialog, IWindowManager windowManager, IEventAggregator eventAggregator,
            IDatabase database,
            StorageDialogViewModel storageDialogViewModel,
            ImportDialogViewModel importDialogViewModel,
            Func<SearchTabViewModel> searchTabViewModelFactory) : base(i18n) {
            this.fileFolderDialog = fileFolderDialog;
            this.windowManager = windowManager;
            this.database = database;
            this.storageDialogViewModel = storageDialogViewModel;
            this.importDialogViewModel = importDialogViewModel;
            this.searchTabViewModelFactory = searchTabViewModelFactory;

            eventAggregator.Subscribe(this);
        }

        // Dummy designer ctor
        public MainWindowViewModel() : base() { }

        protected override void OnInitialActivate() {
            NewSearchTab();
        }

        public void Handle(ImportEvent importEvent) {
            // TODO: Add import tab
        }

        public void NewSearchTab() {
            SearchTabViewModel vm = searchTabViewModelFactory();
            vm.DisplayName = $"Search ({Items.Count})";

            Logger.Debug("Creating tab: {vm}", vm);

            ActivateItem(vm);
        }

        public async Task OpenCatalogFileAsync(string filepath) {
            Logger.Trace("Open catalog file: {filepath}", filepath);

            CatalogStatus result;

            if (database.IsOpen) { // attempt to close the catalog file, so we can open the new one.
                Logger.Debug("A catalog is already open, attempting to close...");

                result = await database.CloseAsync();
                if (result.HasFlag(CatalogStatus.Failure)) {

                    // failed to close catalog, log and inform user.
                    Logger.Error("Failed to close catalog {status}", result);
                    windowManager.ShowMessageBox(I18N.Get("dialogs.open_catalog.failed.close", result), I18N["dialogs.open_catalog.failed.close.title"], icon: System.Windows.MessageBoxImage.Error);

                    return;
                }
            }

            result = await database.OpenAsync(filepath);
            if (result.HasFlag(CatalogStatus.Failure)) {

                // failed to open catalog, log and inform user.
                Logger.Error("Failed to open catalog {filepath} -> {status}", filepath, result);
                windowManager.ShowMessageBox(I18N.Get("dialogs.open_catalog.failed.open", filepath, result), I18N["dialogs.open_catalog.failed.open.title"], icon: System.Windows.MessageBoxImage.Error);

            } else {
                NotifyOfPropertyChange(nameof(CanCloseCatalog));
                NotifyOfPropertyChange(nameof(DisplayName));
                windowManager.ShowMessageBox(I18N["dialogs.open_catalog.success"], I18N["dialogs.open_catalog.success.title"], icon: System.Windows.MessageBoxImage.Information);
            }

        }


        public async Task ShowNewCatalogDialog() {
            string filepath = fileFolderDialog.ShowSaveFileDialog(I18N["dialogs.new_catalog.title"], I18N["dialogs.new_catalog.filter"], checkFileExists: false);

            if (filepath != null) {

                Logger.Trace("New catalog: {filepath}", filepath);

                CatalogStatus result = await database.CreateAsync(filepath); // create the catalog.
                if (result.HasFlag(CatalogStatus.Failure)) {
                    // failed to create catalog, log and inform user.
                    Logger.Error("Failed to create catalog {filepath} -> {status}", filepath, result);
                    windowManager.ShowMessageBox(I18N.Get("dialogs.new_catalog.failed", filepath, result), I18N["dialogs.new_catalog.open_confirmation.title"], icon: System.Windows.MessageBoxImage.Error);

                } else {

                    // Ask the user if they want to open the newly created catalog.
                    if (windowManager.ShowMessageBox(I18N["dialogs.new_catalog.open_confirmation"], I18N["dialogs.new_catalog.open_confirmation.title"], System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question, System.Windows.MessageBoxResult.Yes) == System.Windows.MessageBoxResult.Yes)
                        await OpenCatalogFileAsync(filepath);
                }

            }

        }

        public async Task ShowOpenCatalogDialog() {
            string[] filepaths = fileFolderDialog.ShowOpenFileDialog(I18N["dialogs.open_catalog.title"], I18N["dialogs.open_catalog.filter"], multiselect: false);

            if (filepaths.Length == 1)
                await OpenCatalogFileAsync(filepaths[0]);
        }

        public bool CanCloseCatalog => database.IsOpen;
        public async Task CloseCatalog() {
            CatalogStatus result = await database.CloseAsync();
            if (result.HasFlag(CatalogStatus.Failure)) {

                // failed to close catalog, log and inform user.
                Logger.Error("Failed to close catalog {status}", result);
                windowManager.ShowMessageBox(I18N.Get("dialogs.open_catalog.failed.close", result), I18N["dialogs.open_catalog.failed.close.title"], icon: System.Windows.MessageBoxImage.Error);

            }

            NotifyOfPropertyChange(nameof(CanCloseCatalog));
            NotifyOfPropertyChange(nameof(DisplayName));

        }

        public void ShowImportFilesDialog() {
            windowManager.ShowDialog(importDialogViewModel, this);
        }

        public void ShowStorageLocationsDialog() {
            windowManager.ShowDialog(storageDialogViewModel, this);
        }

        public void OpenWikiLink() {
            Logger.Info("Opening link: {url}", WikiUrl);
            System.Diagnostics.Process.Start(WikiUrl);
        }

    }

}
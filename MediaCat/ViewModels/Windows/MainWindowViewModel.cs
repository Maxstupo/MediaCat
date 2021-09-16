namespace MediaCat.ViewModels.Windows {
    using System;
    using MediaCat.Core.Services.Localization;
    using MediaCat.Services;
    using MediaCat.ViewModels.Dialogs;
    using MediaCat.ViewModels.Tabs;
    using Stylet;

    public sealed class MainWindowViewModel : ConductorOneActive<ITabPage>, IHandle<ImportEvent> {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IFileFolderDialog fileFolderDialog;
        private readonly IWindowManager windowManager;
        private readonly StorageDialogViewModel storageDialogViewModel;
        private readonly ImportDialogViewModel importDialogViewModel;
        private readonly Func<SearchTabViewModel> searchTabViewModelFactory;

        public MainWindowViewModel(II18N i18n, IFileFolderDialog fileFolderDialog, IWindowManager windowManager, IEventAggregator eventAggregator,
            StorageDialogViewModel storageDialogViewModel,
            ImportDialogViewModel importDialogViewModel,
            Func<SearchTabViewModel> searchTabViewModelFactory) : base(i18n) {
            this.fileFolderDialog = fileFolderDialog;
            this.windowManager = windowManager;
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

        public void NewSearchTab() {
            SearchTabViewModel vm = searchTabViewModelFactory();
            vm.DisplayName = $"Search ({Items.Count})";

            Logger.Debug("Creating Tab: {vm}", vm);

            ActivateItem(vm);
        }

        public void ShowNewCatalogDialog() { }

        public void ShowOpenCatalogDialog() { }

        public void ShowImportFilesDialog() {
            windowManager.ShowDialog(importDialogViewModel, this);
        }

        public void ShowStorageLocationsDialog() {
            windowManager.ShowDialog(storageDialogViewModel, this);
        }

        public void Handle(ImportEvent importEvent) {
            // TODO: Add import tab
        }

    }

}
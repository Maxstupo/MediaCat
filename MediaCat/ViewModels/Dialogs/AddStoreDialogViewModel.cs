namespace MediaCat.ViewModels.Dialogs {
    using System.IO.Abstractions;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using MediaCat.Core.Model;
    using MediaCat.Core.Services.Catalog;
    using MediaCat.Core.Services.Catalog.Database;
    using MediaCat.Core.Services.Localization;
    using MediaCat.Core.Utility.Extensions;
    using MediaCat.Services;
    using Stylet;

    public sealed class AddStoreDialogViewModel : ViewModelBase {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IFileFolderDialog fileFolderDialog;
        private readonly IWindowManager windowManager;
        private readonly IWarehouse warehouse;
        private readonly IDatabase database;
        private readonly IFileSystem fileSystem;

        /// <summary>If edit mode is false, this is the result that can be accessed after the dialog is confirmed. Edit mode - this is the store this view model is editing.</summary>
        public Store Store { get; set; } = new Store();

        /// <summary>If true we are editing the Store property instead of creating one.</summary>
        public bool EditMode { get; set; } = false;

        private string label = string.Empty;

        /// <summary>The cosmetic label of the store.</summary>
        public string Label {
            get => label;
            set {
                if (string.IsNullOrWhiteSpace(Name) || Name == Label.AsAlphaNumeric())
                    Name = value;
                label = value;
            }
        }

        private string name = string.Empty;
        /// <summary>The name of the store (the name of the folder).</summary>
        public string Name {
            get => name;
            set => name = value.AsAlphaNumeric();
        }

        /// <summary>The directory location for the store folder.</summary>
        public string Location { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        private string Path => fileSystem.Path.Combine(Location, Name);

        public AddStoreDialogViewModel(II18N i18n, IFileFolderDialog fileFolderDialog, IWindowManager windowManager, IWarehouse catalog, IDatabase database, IFileSystem fileSystem) : base(i18n) {
            this.fileFolderDialog = fileFolderDialog;
            this.windowManager = windowManager;
            this.warehouse = catalog;
            this.database = database;
            this.fileSystem = fileSystem;
        }

        public AddStoreDialogViewModel() : base() {

        }

        public override void OnOpen() {
            if (EditMode) { // Edit mode, set properties to equal existing store.
                if (Store == null)
                    throw new System.Exception("Store is null! EditMode requires a StorageLocation!");

                Label = Store.Label;
                Name = fileSystem.Path.GetFileName(Store.Path);
                Location = fileSystem.Path.GetDirectoryName(Store.Path);
                IsDefault = Store.IsDefault;

            } else { // Clear properties
                Store = null;
                Label = Name = Location = string.Empty;
                IsDefault = false;

            }
        }

        public void Browse() {
            string[] folderpaths = fileFolderDialog.ShowOpenFolderDialog(I18N["dialogs.add_store.browse.title"], false);
            if (folderpaths.Length == 1) {
                string folderpath = folderpaths[0];
                string catalogDirectory = fileSystem.Path.GetDirectoryName(database.Filepath);

                // Use a relative path if in the same directory tree as the catalog.
                if (fileSystem.Path.CanBeRelative(folderpath, catalogDirectory) &&
                    windowManager.ShowMessageBox(I18N["dialogs.add_store.can_be_relative"], I18N["dialogs.add_store.can_be_relative.title"], buttons: System.Windows.MessageBoxButton.YesNo, icon: System.Windows.MessageBoxImage.Question, defaultResult: System.Windows.MessageBoxResult.No) == System.Windows.MessageBoxResult.Yes) {

                    Location = fileSystem.Path.TryMakeRelative(folderpath, catalogDirectory);
                } else {
                    Location = folderpath;
                }

            }
        }

        private bool CheckPathExists() {
            string storePath = warehouse.ResolveStorePath(Path);
            Logger.Trace("Path: {storePath}", storePath);
            return fileSystem.Directory.Exists(storePath);
        }


        public bool CanConfirm => !string.IsNullOrWhiteSpace(Label) && !string.IsNullOrWhiteSpace(Location) && !string.IsNullOrWhiteSpace(Name)
            && !CheckPathExists();
        public async Task Confirm() {

            if (EditMode) {
                Store.Path = Path;
                Store.Label = Label;
                Store.IsDefault = IsDefault;
            }

            WarehouseResult result = !EditMode ?
                await Task.Run(() => warehouse.CreateStoreAsync(Label, Path, IsDefault)) :
                await Task.Run(() => warehouse.EditStoreAsync(Store));

            bool hasFailure = result.Status.HasFlag(WarehouseStoreStatus.Failure);
            if (hasFailure) {
                Logger.Error($"Failed to {(EditMode ? "edit" : "create")} store: {{status}}", result.Status);

                windowManager.ShowMessageBox(I18N.Get($"dialogs.{(EditMode ? "edit" : "add")}_store.failed", result.Status), I18N[$"dialogs.{(EditMode ? "edit" : "add")}_store.failed.title"], System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

            } else if (!EditMode) {
                Store = result.Store;
            }

            EditMode = false;
            RequestClose(!hasFailure);
        }

        public void Cancel() {
            EditMode = false;
            RequestClose(false);
        }

    }

}
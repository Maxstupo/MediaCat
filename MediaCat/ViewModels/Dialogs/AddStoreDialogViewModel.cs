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
        private readonly IWarehouse catalog;
        private readonly IDatabase database;
        private readonly IFileSystem fileSystem;

        public Store StorageLocation { get; set; } = new Store();

        public bool EditMode { get; set; } = false;

        private string label = string.Empty;
        public string Label {
            get => label;
            set {
                if (string.IsNullOrWhiteSpace(Name) || Name == ToAlphaNumeric(Label))
                    Name = value;
                label = value;
            }
        }

        private string name = string.Empty;
        public string Name {
            get => name;
            set => name = ToAlphaNumeric(value);
        }

        private string ToAlphaNumeric(string value) => Regex.Replace(value, "[^a-zA-Z0-9_-]+", string.Empty, RegexOptions.Compiled);

        public string Location { get; set; } = string.Empty;

        public bool IsDefault { get; set; }

        public string Path => fileSystem.Path.Combine(Location, Name);

        public AddStoreDialogViewModel(II18N i18n, IFileFolderDialog fileFolderDialog, IWindowManager windowManager, IWarehouse catalog, IDatabase database, IFileSystem fileSystem) : base(i18n) {
            this.fileFolderDialog = fileFolderDialog;
            this.windowManager = windowManager;
            this.catalog = catalog;
            this.database = database;
            this.fileSystem = fileSystem;
        }

        public AddStoreDialogViewModel() : base() {

        }

        public override void OnOpen() {
            if (EditMode) { // Edit mode, set properties to equal existing storage location.
                if (StorageLocation == null)
                    throw new System.Exception("StorageLocation is null! EditMode requires a StorageLocation!");

                Label = StorageLocation.Label;
                Name = fileSystem.Path.GetFileName(StorageLocation.Path);
                Location = fileSystem.Path.GetDirectoryName(StorageLocation.Path);
                IsDefault = StorageLocation.IsDefault;

            } else { // Clear properties
                StorageLocation = null;
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
            string storePath = catalog.ResolveStorageLocationPath(Path);
            Logger.Trace("Path: {storePath}", storePath);
            return fileSystem.Directory.Exists(storePath);
        }


        public bool CanConfirm => !string.IsNullOrWhiteSpace(Label) && !string.IsNullOrWhiteSpace(Location) && !string.IsNullOrWhiteSpace(Name)
            && (EditMode || !CheckPathExists());
        public async Task Confirm() {

            if (EditMode) {
                StorageLocation.Path = Path;
                StorageLocation.Label = Label;
                StorageLocation.IsDefault = IsDefault;
            }

            WarehouseResult result = !EditMode ?
                await Task.Run(() => catalog.CreateStoreAsync(Label, Path, IsDefault)) :
                await Task.Run(() => catalog.EditStoreAsync(StorageLocation));

            bool hasFailure = result.Status.HasFlag(WarehouseStoreStatus.Failure);

            if (hasFailure) {
                Logger.Error($"Failed to {(EditMode ? "edit" : "create")} store: {{status}}", result.Status);

                windowManager.ShowMessageBox(I18N.Get($"dialogs.{(EditMode ? "edit" : "add")}_store.failed", result.Status), I18N[$"dialogs.{(EditMode ? "edit" : "add")}_store.failed.title"], System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

            } else if (!EditMode) {
                StorageLocation = result.Store;
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
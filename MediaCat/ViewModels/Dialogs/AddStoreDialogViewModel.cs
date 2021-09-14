namespace MediaCat.ViewModels.Dialogs {
    using System.IO.Abstractions;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using MediaCat.Core.Model;
    using MediaCat.Core.Services.Catalog;
    using MediaCat.Core.Services.Localization;
    using MediaCat.Core.Utility.Extensions;
    using MediaCat.Services;
    using Stylet;

    public sealed class AddStoreDialogViewModel : ViewModelBase {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IFileFolderDialog fileFolderDialog;
        private readonly IWindowManager windowManager;
        private readonly ICatalog catalog;
        private readonly IDatabase database;
        private readonly IFileSystem fileSystem;

        public StorageLocation Result { get; private set; }

        public bool EditMode { get; set; } = false;

        private string name = string.Empty;
        public string Name { get => name; set => name = Regex.Replace(value, "[^a-zA-Z0-9_-]+", string.Empty, RegexOptions.Compiled); }

        public string Location { get; set; } = string.Empty;

        public bool IsDefault { get; set; }


        public string Path => fileSystem.Path.Combine(Location, Name);

        public AddStoreDialogViewModel(II18N i18n, IFileFolderDialog fileFolderDialog, IWindowManager windowManager, ICatalog catalog, IDatabase database, IFileSystem fileSystem) : base(i18n) {
            this.fileFolderDialog = fileFolderDialog;
            this.windowManager = windowManager;
            this.catalog = catalog;
            this.database = database;
            this.fileSystem = fileSystem;
        }

        public AddStoreDialogViewModel() : base() {

        }

        public override void OnOpen() {
            if (!EditMode) {
                Name = string.Empty;
                Location = string.Empty;
                IsDefault = false;
                Result = null;
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


        public bool CanConfirm => !string.IsNullOrWhiteSpace(Location) && !string.IsNullOrWhiteSpace(Name) && !CheckPathExists();
        public async Task Confirm() {
            CatalogStorageResult result = await catalog.CreateStoreAsync(Name, Path, IsDefault);

            if (result.Status.HasFlag(StorageLocationStatus.Failure)) {
                Logger.Error("Failed to create store: {status}", result.Status);

                windowManager.ShowMessageBox(I18N.Get("dialogs.add_store.failed", result.Status), I18N["dialogs.add_store.failed.title"], System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);

                RequestClose(false);
            } else {
                Result = result.StorageLocation;
                RequestClose(true);
            }

        }

        public void Cancel() {
            RequestClose(false);
        }

    }

}
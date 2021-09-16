namespace MediaCat.ViewModels.Dialogs {
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MediaCat.Core.Model;
    using MediaCat.Core.Services.Catalog;
    using MediaCat.Core.Services.Localization;
    using MediaCat.Core.Utility;
    using MediaCat.Core.Utility.Extensions;
    using MediaCat.Services;
    using Stylet;

    public sealed class ImportEvent {

        public List<ImportItem> Items { get; }

        public Store Store { get; }

        public ImportEvent(Store store, List<ImportItem> items) {
            this.Store = store;
            this.Items = items;
        }

    }

    public sealed class ImportDialogViewModel : ViewModelBase, ICanRefreshData {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IEventAggregator eventAggregator;
        private readonly IFileFolderDialog fileFolderDialog;
        private readonly IFileSystem fileSystem;
        private readonly IWarehouse warehouse;

        /// <summary>The status of the current staging operation.</summary>
        public string Status { get; private set; }

        /// <summary>If true, files are being parsed into the staging list.</summary>
        public bool IsLoading { get; private set; }

        /// <summary>If true, the loading operation (AddFile, AddFolder) is paused.</summary>
        public bool IsLoadingPaused { get; private set; }


        /// <summary>If true, the progress is indeterminate.</summary>
        public bool IsLoadingIndeterminate { get; private set; } = true;

        /// <summary>The maximum progress.</summary>   
        public int LoadingMax { get; private set; }

        /// <summary>The current progress.</summary>
        public int LoadingValue { get; set; }


        /// <summary>A collection of available and valid stores.</summary>
        public BindableCollection<StoreItemViewModel> Stores { get; } = new BindableCollection<StoreItemViewModel>();
        /// <summary>The current store these files will be imported into.</summary>
        public StoreItemViewModel SelectedStore { get; set; }

        /// <summary>A list of already parsed import items wrapped in a view model. These items will be imported if the dialog is confirmed.</summary>
        public BindableCollection<ImportItemViewModel> Items { get; } = new BindableCollection<ImportItemViewModel>();

        public bool IsRefreshingData { get; private set; }

        private CancellationTokenSource cts; // the cts & pts used by add file & folder operations.
        private readonly PauseTokenSource pts = new PauseTokenSource();


        public ImportDialogViewModel(II18N i18n, IEventAggregator eventAggregator, IFileFolderDialog fileFolderDialog, IFileSystem fileSystem, IWarehouse warehouse) : base(i18n) {
            this.eventAggregator = eventAggregator;
            this.fileFolderDialog = fileFolderDialog;
            this.fileSystem = fileSystem;
            this.warehouse = warehouse;
        }

        public ImportDialogViewModel() : base() { }


        public bool CanRefreshDataAsync => !IsRefreshingData;
        public async Task RefreshDataAsync() {
            IsRefreshingData = true;
            {
                Status = I18N["dialogs.import.status.waiting"];

                Items.Clear();

                Stores.Clear(); // Load available stores.
                Stores.AddRange((await warehouse.GetStoresAsync()).Select(x => new StoreItemViewModel(x)));

                SelectedStore = Stores.FirstOrDefault(x => x.IsDefault);
            }
            IsRefreshingData = false;
        }

        /// <summary>
        /// Parse the specified filepath and check if the warehouse accepts it as a suitable file. If so add it to the list.
        /// </summary>
        private async Task ParseFileAsync(string filepath, int loadingValue, CancellationToken ct) {
            WarehouseResult result = await warehouse.ParseFileAsync(filepath, ct); // check if filepath is suitable for importing.

            await Execute.OnUIThreadAsync(() => {
                Status = I18N.Get($"dialogs.import.status.parse_file", filepath);

                LoadingValue = loadingValue;

                if (result.Status.HasFlag(WarehouseStoreStatus.Failure)) {
                    Status = I18N.Get($"dialogs.import.status.unsupported", filepath);
                    Logger.Warn("Unsupported file: {filepath}", filepath);

                } else {
                    ImportItemViewModel itemVm = new ImportItemViewModel(result.ImportItem);
                    if (!Items.Contains(itemVm))
                        Items.Add(itemVm);
                }
            });
        }

        public bool CanAddFile => !IsLoading;
        public async Task AddFile() {
            string[] filepaths = fileFolderDialog.ShowOpenFileDialog(I18N["dialogs.import.actions.add_file.title"], I18N.GetOrNull("dialogs.import.actions.add_file.filter"), multiselect: true);

            Logger.Trace("Add file: {filepaths}", filepaths);

            if (filepaths.Length > 0) {

                UpdateState(filepaths.Length, true); // Set loading.

                cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                PauseToken pt = pts.Token;

                await Task.Run(async () => {

                    for (int index = 0; index < filepaths.Length; index++) {
                        await pt.WaitWhilePausedAsync(ct); // async wait if paused.

                        if (ct.IsCancellationRequested) 
                            break;                        

                        await ParseFileAsync(filepaths[index], index, ct);
                    }

                }, ct).ContinueWith(t => { // Dispose of our CancellationTokenSource
                    cts?.Cancel();
                    cts?.Dispose();
                    cts = null;
                });

                UpdateState(0, false); // Not loading

            }
        }

        public bool CanAddFolder => !IsLoading;
        public async Task AddFolder() {
            string[] folderpaths = fileFolderDialog.ShowOpenFolderDialog(I18N["dialogs.import.actions.add_folder.title"], multiselect: true);

            Logger.Trace("Add folder: {folderpaths}", folderpaths);

            if (folderpaths.Length > 0) {

                UpdateState(0, true); // Set loading.
                IsLoadingIndeterminate = true; // indeterminate since we need to know the total folder count before we can update the progress bar.

                cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                PauseToken pt = pts.Token;

                Status = I18N["dialogs.import.status.discovering"];

                await Task.Run(async () => {
                    List<string> discoveredFiles = new List<string>();

                    // Find all files in all selected directories.
                    foreach (string directory in folderpaths) {
                        await pt.WaitWhilePausedAsync(ct); // async wait if paused.

                        if (ct.IsCancellationRequested)
                            break;

                        discoveredFiles.AddRange(fileSystem.Directory.EnumerateFiles(directory, "*.*", System.IO.SearchOption.AllDirectories, ct));
                    }

                    if (ct.IsCancellationRequested) { // User stopped, so provide feedback message.
                        await Execute.OnUIThreadAsync(() => {
                            Status = I18N["dialogs.import.status.stopped"];
                        });
                    } else {

                        await Execute.OnUIThreadAsync(() => {
                            Status = I18N.Get("dialogs.import.status.discovered", discoveredFiles.Count);
                        });

                        // Delay so we can see above status message. ContinueWith stops TaskCanceledException
                        await Task.Delay(2000, ct).ContinueWith(t => { });
                        await pt.WaitWhilePausedAsync(ct); // async wait if paused.

                        UpdateState(discoveredFiles.Count, true); // update progress bars with new maximum.

                        // Parse and add to staging list.
                        for (int index = 0; index < discoveredFiles.Count; index++) {
                            await pt.WaitWhilePausedAsync(ct); // async wait if paused.

                            if (ct.IsCancellationRequested)
                                break;

                            await ParseFileAsync(discoveredFiles[index], index, ct);
                        }

                    }

                }).ContinueWith(t => { // Dispose of our CancellationTokenSource
                    cts?.Cancel();
                    cts?.Dispose();
                    cts = null;
                });

                UpdateState(0, false); // Not loading

            }

        }

        public bool CanRemoveFile => false;
        public void RemoveFile() { }

        public bool CanImport => !IsLoading && Items.Count > 0 && SelectedStore != null;
        public void Import() {
            eventAggregator.Publish(new ImportEvent(SelectedStore.Store, Items.Select(x => x.ImportItem).ToList()));
            RequestClose(true);
        }

        public bool CanCancel => !IsLoading;
        public void Cancel() {
            RequestClose(false);
        }

        public bool CanStopLoading => IsLoading;
        public void StopLoading() {
            cts?.Cancel();
            cts?.Dispose();
            cts = null;

            UpdateState(0, false);
        }

        public bool CanToggleLoading => IsLoading;
        public void ToggleLoading() {
            pts.IsPaused = !pts.IsPaused;
            IsLoadingPaused = pts.IsPaused;
        }

        public override async Task<bool> CanCloseAsync() {
            return !IsLoading && await base.CanCloseAsync();
        }


        /// <summary>
        /// Update the view model state, based on if it is loading or not. Unpauses loading and disables indeterminate mode.
        /// </summary>
        private void UpdateState(int loadingMax, bool isLoading) {
            if (isLoading) {
                LoadingValue = 0;
                LoadingMax = loadingMax;
                Status = I18N["dialogs.import.status.parsing"];

            } else {
                LoadingValue = 0;
                LoadingMax = 0;

                Status = I18N["dialogs.import.status.ready"];
            }

            IsLoading = isLoading;
            IsLoadingIndeterminate = false;
            IsLoadingPaused = false;
            pts.IsPaused = false;
        }

    }

}
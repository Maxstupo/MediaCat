namespace MediaCat.ViewModels.Tabs {
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using MediaCat.Core.Model;
    using MediaCat.Core.Services.Catalog;
    using MediaCat.Core.Services.Localization;
    using MediaCat.Core.Utility;
    using Stylet;

    public sealed class ImportTabViewModel : ViewModelBase, ITabPage {

        private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly IWarehouse warehouse;

        public bool CanUserClose => !IsLoading;

        public bool CanUserDuplicate => false;

        public bool CanUserRename => true;


        public string Status { get; private set; } = "Waiting for files...";

        public bool IsLoading { get; private set; }
        public bool IsLoadingPaused { get; private set; }

        public int LoadingMax { get; private set; }
        public int LoadingValue { get; set; }

        public BindableCollection<ImportItemViewModel> Items { get; } = new BindableCollection<ImportItemViewModel>();

        private readonly PauseTokenSource pts = new PauseTokenSource();
        private CancellationTokenSource cts;

        private bool isUsed = false; // true if this tab task has run.


        public ImportTabViewModel(II18N i18n, IWarehouse warehouse) : base(i18n) {
            this.warehouse = warehouse;
            DisplayName = I18N["tabs.import.title"];
        }

        public ImportTabViewModel() : base() { } // Dummy designer ctor

        public async Task BeginImport(Store store, List<ImportItem> items) {
            if (isUsed)
                return;
            isUsed = true;

            DisplayName = I18N.Get("tabs.import.title", store.Label, items.Count);

            int idx = 0;
            Items.AddRange(items.Select(x => new ImportItemViewModel(x) { Index = ++idx }));

            LoadingMax = items.Count;
            IsLoading = true;

            cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            PauseToken pt = pts.Token;

            await Task.Run(async () => {

                for (int index = 0; index < Items.Count; index++) {
                    await pt.WaitWhilePausedAsync();

                    if (ct.IsCancellationRequested)
                        break;

                    ImportItemViewModel importItemViewModel = Items[index];

                    WarehouseResult result = await warehouse.ImportFileAsync(store, importItemViewModel.ImportItem, ct);

                    await Execute.PostToUIThreadAsync(() => {
                        LoadingValue = index;
                        Status = "Imported: " + importItemViewModel.Filepath;
                        importItemViewModel.ImportedOn = DateTime.Now;
                        importItemViewModel.Status = "Done.";
                    });
                }

            }, ct).ContinueWith(t => {
                cts?.Cancel();
                cts?.Dispose();
                cts = null;
            });

            IsLoading = false;
        }



        public bool CanToggleLoading => IsLoading;
        public void ToggleLoading() {
            pts.IsPaused = !pts.IsPaused;
            IsLoadingPaused = pts.IsPaused;
        }

        public bool CanStopLoading => IsLoading;
        public void StopLoading() {
            cts.Cancel();
            cts.Dispose();
            cts = null;

            IsLoadingPaused = pts.IsPaused = false;
            LoadingValue = 0;
            Status = "User stopped.";
        }


        public ITabPage Clone() {
            return null;
        }


    }

}
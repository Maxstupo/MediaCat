namespace MediaCat.ViewModels.Dialogs {
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using MediaCat.Core.Services.Localization;

    public sealed class ImportDialogViewModel : ViewModelBase, ICanRefreshData {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public string Status { get; private set; } = "Waiting for files...";

        public bool IsLoading { get; private set; }
        public bool IsLoadingPaused { get; private set; }
        public bool IsLoadingIndeterminate { get; private set; } = true;
        public int LoadingMax { get; private set; }
        public int LoadingValue { get; set; }

        public bool IsRefreshingData => IsLoading;

        public ImportDialogViewModel(II18N i18n) : base(i18n) {

        }

        public ImportDialogViewModel() : base() { }


        public bool CanRefreshDataAsync => !IsLoading;
        public Task RefreshDataAsync() {
            return Task.CompletedTask;
        }

    }

}
namespace MediaCat.ViewModels.Dialogs {
    using System.Threading.Tasks;
    using MediaCat.Core.Services.Localization;

    public sealed class StorageDialogViewModel : ViewModelBase, ICanRefreshData {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();


        public StorageDialogViewModel(II18N i18n) : base(i18n) {

        }

        public StorageDialogViewModel() : base() {

        }

        public Task RefreshDataAsync() {
            return Task.CompletedTask;
        }

    }

}
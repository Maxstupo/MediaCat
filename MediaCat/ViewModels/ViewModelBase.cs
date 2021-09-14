namespace MediaCat.ViewModels {
    using System;
    using System.Threading.Tasks;
    using MediaCat.Core.Services.Localization;
    using MediaCat.Services;
    using Stylet;

    public abstract class ConductorOneActive<T> : Conductor<T>.Collection.OneActive where T : class {

        public II18N I18N { get; }

        public ConductorOneActive(II18N i18N) {
            this.I18N = i18N;
        }

        // Dummy designer ctor
        public ConductorOneActive() {
            if (!Execute.InDesignMode)
                throw new Exception("Designer constructor!");
            I18N = new I18NMock(ViewModelBase.DesignerLocaleFilepath);
        }

    }

    /// <summary>
    /// The base view model used primarily by tab and dialog view models. Supports ICanRefreshData interface, will be called when the view is opened.
    /// </summary>
    public abstract class ViewModelBase : Screen {
        public static readonly string DesignerLocaleFilepath = "../Locales/default.en.json";

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public II18N I18N { get; }

        private bool allowDeactivated = true;

        public ViewModelBase(II18N i18N) {
            this.I18N = i18N;
        }

        // Dummy designer ctor
        public ViewModelBase() {
            if (!Execute.InDesignMode)
                throw new Exception("Designer constructor!");
            I18N = new I18NMock(DesignerLocaleFilepath);
        }

        public virtual async void OnOpen() {
            if (this is ICanRefreshData refresh)
                await refresh.RefreshDataAsync();
        }

        public override async Task<bool> CanCloseAsync() {
            if (this is ICanRefreshData refresh)
                return !refresh.IsRefreshingData && await base.CanCloseAsync();
            return await base.CanCloseAsync();
        }

        protected override void OnInitialActivate() {
            allowDeactivated = false;
        }

        protected override void OnStateChanged(ScreenState previousState, ScreenState newState) {
            base.OnStateChanged(previousState, newState);

            if ((previousState == ScreenState.Closed || (allowDeactivated && previousState == ScreenState.Deactivated)) && newState == ScreenState.Active) {
                Logger.Trace("OnOpen()");
                OnOpen();
            }
        }

    }

}
namespace MediaCat.ViewModels {
    using System;
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
            I18N = new I18NMock("../Locales/default.en.json");
        }

    }

    public abstract class ScreenBase : Screen {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public II18N I18N { get; }


        private bool allowDeactivated = true;

        public ScreenBase(II18N i18N) {
            this.I18N = i18N;
        }

        // Dummy designer ctor
        public ScreenBase() {
            if (!Execute.InDesignMode)
                throw new Exception("Designer constructor!");
            I18N = new I18NMock("../Locales/default.en.json");
        }

        public virtual async void OnOpen() {
            if (this is ICanRefreshData refresh)
                await refresh.RefreshDataAsync();
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
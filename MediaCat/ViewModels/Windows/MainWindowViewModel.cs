namespace MediaCat.ViewModels.Windows {
    using System;
    using MediaCat.Core.Services.Localization;
    using MediaCat.Services;
    using Stylet;

    public sealed class MainWindowViewModel : Screen {

        public II18N I18N { get; }

        public MainWindowViewModel(II18N i18n) {
            this.I18N = i18n;
        }

        // Dummy designer ctor
        public MainWindowViewModel() {
            if (!Execute.InDesignMode)
                throw new InvalidOperationException("Designer constructor!");
            I18N = new I18NMock("../Locales/default.en.json");
        }

    }

}
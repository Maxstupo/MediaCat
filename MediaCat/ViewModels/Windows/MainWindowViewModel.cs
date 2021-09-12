namespace MediaCat.ViewModels.Windows {
    using System;
    using MediaCat.Core.Services.Localization;
    using MediaCat.Services;
    using Stylet;

    public sealed class MainWindowViewModel : Screen {
        private readonly IFileFolderDialog fileFolderDialog;

        public II18N I18N { get; }

        public MainWindowViewModel(II18N i18n, IFileFolderDialog fileFolderDialog) {
            this.I18N = i18n;
            this.fileFolderDialog = fileFolderDialog;
        }

        // Dummy designer ctor
        public MainWindowViewModel() {
            if (!Execute.InDesignMode)
                throw new InvalidOperationException("Designer constructor!");
            I18N = new I18NMock("../Locales/default.en.json");
        }


        public void ShowSaveFileDialog() {
            fileFolderDialog.ShowSaveFileDialog("Save File...", "All Files (*.*)|*.*");
        }

    }

}
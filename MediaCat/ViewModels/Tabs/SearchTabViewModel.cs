namespace MediaCat.ViewModels.Tabs {
    using System;
    using MediaCat.Core.Services.Localization;

    public sealed class SearchTabViewModel : ViewModelBase, ITabPage {

        public bool CanUserClose { get; } = true;

        public bool CanUserDuplicate { get; } = true;

        public bool CanUserRename { get; } = false;


        public SearchTabViewModel(II18N i18n) : base(i18n) {

        }

        // Dummy designer ctor
        public SearchTabViewModel() : base() { }


        public ITabPage Clone() {
            return new SearchTabViewModel();
        }

    }

}
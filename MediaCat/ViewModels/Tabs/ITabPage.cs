namespace MediaCat.ViewModels.Tabs {
    using Stylet;

    public interface ITabPage : IScreen {

        bool CanUserClose { get; }

        bool CanUserDuplicate { get; }

        bool CanUserRename { get; }


        ITabPage Clone();

    }

}
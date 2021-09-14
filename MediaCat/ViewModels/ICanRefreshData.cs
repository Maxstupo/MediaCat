namespace MediaCat.ViewModels {
    using System.Threading.Tasks;

    public interface ICanRefreshData {

        bool IsRefreshingData { get; }
        bool CanRefreshDataAsync { get; }

        Task RefreshDataAsync();

    }

}
namespace MediaCat.ViewModels {
    using System.Threading.Tasks;

    public interface ICanRefreshData {

        Task RefreshDataAsync();

    }

}
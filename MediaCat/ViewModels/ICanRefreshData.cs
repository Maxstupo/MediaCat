namespace MediaCat.ViewModels {
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a view model that can refresh data.
    /// </summary>
    public interface ICanRefreshData {

        /// <summary>If true the data is currently refreshing. Expected that this property is updated within the RefreshDataAsync method.</summary>
        bool IsRefreshingData { get; }

        /// <summary>True if RefreshDataAsync can be called.</summary>
        bool CanRefreshDataAsync { get; }

        Task RefreshDataAsync();

    }

}
namespace MediaCat.Core.Services.Catalog.Database {
    using System;
    using System.Threading.Tasks;

    [Flags]
    public enum CatalogStatus { // TODO: Clean up
        None = 0,
        Success = 1,
        Failure = 2,

        Open = 4,
        Closed = 8,
        Has = 16,
        Not = 32,
        Catalog = 64,

        FailureFileExists = Failure | Has | Catalog,
        FailureFileNotExists = Failure | Has | Not | Catalog,
        FailureOpen = Failure | Open,
        FailureClosed = Failure | Closed,

    }

    public interface IDatabase : IDisposable {

        string Filepath { get; }

        bool IsOpen { get; }

        event EventHandler OnConnectionStateChanged;

        Task<CatalogStatus> CreateAsync(string path);

        Task<CatalogStatus> OpenAsync(string path);

        Task<CatalogStatus> CloseAsync();

    }

}
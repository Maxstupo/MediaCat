namespace MediaCat.Core.Services.Catalog {
    using System;
    using System.Threading.Tasks;

    [Flags]
    public enum StorageLocationStatus {
        None = 0,
        Success = 1,
        Failure = 2,

        Exists = 4,
        Directory = 8,
        Label = 16,
        Path = 32,

        DirectoryExists = Failure | Directory | Exists,
        LabelExists = Failure | Label | Exists,
        PathExists = Failure | Path | Exists
    }

    [Flags]
    public enum CatalogStatus {
        None = 0,
        Success = 1,
        Failure = 2,

        Open = 4,
        Closed = 8,
        Has = 16,
        Not = 32,
        Catalog = 64,

        FailureHasCatalog = Failure | Has | Catalog,
        FailureNoCatalog = Failure | Has | Not | Catalog,
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
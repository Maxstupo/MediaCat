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
        Location = 16,
        Label = 32,

        DirectoryExists = Failure | Exists | Directory,
        DirectoryAlreadyUsed = Failure | Exists | Location,
        LabelAlreadyUsed = Failure | Exists | Label
    }

    [Flags]
    public enum CatalogStatus {
        None = 0,
        Success = 1,
        Failure = 2,

        Open = 4,
        Closed = 8,
        HasCatalog = 16,
        NoCatalog = 32,

        Exists = Failure | HasCatalog,
        NotExists = Failure | NoCatalog,
        AlreadyOpen = Failure | Open,
        AlreadyClosed = Failure | Closed,

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
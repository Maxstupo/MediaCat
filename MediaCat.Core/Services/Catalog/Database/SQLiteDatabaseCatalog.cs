namespace MediaCat.Core.Services.Catalog.Database {
    using System;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading.Tasks;
    using MediaCat.Core.Model;
    using MediaCat.Core.Utility;
    using SQLite;
    using SQLiteNetExtensionsAsync.Extensions;

    public sealed class SQLiteDatabaseCatalog : ICatalog, IDatabase {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly SQLiteOpenFlags OpenFlags = SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite;

        public string Filepath => connection?.DatabasePath ?? null;

        public bool IsOpen => connection != null;


        private SQLiteAsyncConnection connection;

        private readonly IFileSystem fileSystem;


        public event EventHandler OnConnectionStateChanged;


        public SQLiteDatabaseCatalog(IFileSystem fileSystem) {
            this.fileSystem = fileSystem;
        }

        #region Connection/Creation Methods

        private static async Task CreateTablesAsync(SQLiteAsyncConnection connection) {

            CreateTableResult status = await connection.CreateTableAsync<Mime>().ConfigureAwait(false);

            if (status == CreateTableResult.Created) {
                Logger.Debug("Defining default catalog mimes types...");
                await connection.InsertAllAsync(MimeTypesMap.MimeTypes.Where(x => x.Value.StartsWith("video") || x.Value.StartsWith("image") || x.Value.StartsWith("audio")).Select(x => new Mime {
                    Extension = $".{x.Key}",
                    Type = x.Value,
                    Label = string.Empty,
                    Viewer = 0
                }));
            }


            await connection.CreateTableAsync<StorageLocation>().ConfigureAwait(false);
            await connection.CreateTableAsync<File>().ConfigureAwait(false);

        }

        public async Task<CatalogStatus> CreateAsync(string path) {
            if (fileSystem.File.Exists(path))
                return CatalogStatus.Exists;

            Logger.Debug("Creating SQLite database...");

            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(path, OpenFlags);

            await CreateTablesAsync(connection).ConfigureAwait(false);

            await connection.CloseAsync().ConfigureAwait(false);

            return CatalogStatus.Success;
        }

        public async Task<CatalogStatus> OpenAsync(string path) {
            if (IsOpen)
                return CatalogStatus.AlreadyOpen;

            if (!fileSystem.File.Exists(path))
                return CatalogStatus.NotExists;

            Logger.Debug("Opening SQLite database...");

            connection = new SQLiteAsyncConnection(path);

            await CreateTablesAsync(connection).ConfigureAwait(false);

            NotifyConnectionStateChanged();

            return CatalogStatus.Success;
        }

        public async Task<CatalogStatus> CloseAsync() {
            if (!IsOpen)
                return CatalogStatus.AlreadyClosed;

            Logger.Debug("Closing SQLite database...");

            await connection.CloseAsync().ConfigureAwait(false);

            connection = null;

            NotifyConnectionStateChanged();

            return CatalogStatus.Success;
        }

        #endregion

        private void NotifyConnectionStateChanged() {
            OnConnectionStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose() {
            if (!IsOpen)
                return;

            Logger.Debug("Disposing SQLite database...");

            connection.GetConnection().Close();
            connection = null;

            NotifyConnectionStateChanged();
        }

    }

}
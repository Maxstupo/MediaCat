namespace MediaCat.Core.Services.Catalog.Database {
    using System;
    using System.IO.Abstractions;
    using System.Threading.Tasks;
    using SQLite;

    public interface ISQLiteDatabase : IDatabase {
        SQLiteAsyncConnection Connection { get; }
    }

    public abstract class SQLiteDatabase : ISQLiteDatabase {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static readonly SQLiteOpenFlags OpenFlags = SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex | SQLiteOpenFlags.ReadWrite;

        public string Filepath => Connection?.DatabasePath ?? null;

        public bool IsOpen => Connection != null;

        public SQLiteAsyncConnection Connection { get; private set; }

        protected readonly IFileSystem fileSystem;

        public event EventHandler OnConnectionStateChanged;

        public SQLiteDatabase(IFileSystem fileSystem) {
            this.fileSystem = fileSystem;
        }

        protected abstract Task CreateTablesAsync(SQLiteAsyncConnection connection);

        #region Connection/Creation Methods

        public async Task<CatalogStatus> CreateAsync(string path) {
            if (fileSystem.File.Exists(path))
                return CatalogStatus.FailureFileExists;

            Logger.Debug("Creating SQLite database: {filepath}", path);

            SQLiteAsyncConnection connection = new SQLiteAsyncConnection(path, OpenFlags);

            await CreateTablesAsync(connection);

            await connection.CloseAsync().ConfigureAwait(false);

            return CatalogStatus.Success;
        }

        public async Task<CatalogStatus> OpenAsync(string path) {
            if (IsOpen)
                return CatalogStatus.FailureOpen;

            path = fileSystem.Path.GetFullPath(path);

            if (!fileSystem.File.Exists(path))
                return CatalogStatus.FailureFileNotExists;

            Logger.Debug("Opening SQLite database: {filepath}", path);

            Connection = new SQLiteAsyncConnection(path);

            await CreateTablesAsync(Connection).ConfigureAwait(false);

            NotifyConnectionStateChanged();

            return CatalogStatus.Success;
        }

        public async Task<CatalogStatus> CloseAsync() {
            if (!IsOpen)
                return CatalogStatus.FailureClosed;

            Logger.Debug("Closing SQLite database: {filepath}", Filepath);

            await Connection.CloseAsync().ConfigureAwait(false);

            Connection = null;

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

            Connection.GetConnection().Close();
            Connection = null;

            NotifyConnectionStateChanged();
        }


    }

}
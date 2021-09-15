namespace MediaCat.Core.Services.Catalog.Database {
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading.Tasks;
    using MediaCat.Core.Model;
    using MediaCat.Core.Utility;
    using SQLite;

    public sealed class MediaCatDatabase : SQLiteDatabase {

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public MediaCatDatabase(IFileSystem fileSystem) : base(fileSystem) {
        }

        protected override async Task CreateTablesAsync(SQLiteAsyncConnection connection) {

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

            await connection.CreateTableAsync<Store>().ConfigureAwait(false);
            await connection.CreateTableAsync<File>().ConfigureAwait(false);

        }

    }

}
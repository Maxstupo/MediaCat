namespace MediaCat.Core.Services.Localization.Readers {
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using MediaCat.Core.Utility.Extensions;
    using Newtonsoft.Json;

    /// <summary>Read the locale resource stream as JSON text. Specifically in the format of key to value pairs.</summary>
    public sealed class JsonKvpReader : ILocaleReader {

        // private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        public Dictionary<string, string> Read(Stream stream) {

            using (StreamReader streamReader = new StreamReader(stream)) {
                string json = streamReader.ReadToEnd();

                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                    .ToDictionary(x => x.Key.Trim(), x => x.Value.Trim().UnescapeLineBreaks());
            }

        }

    }
}

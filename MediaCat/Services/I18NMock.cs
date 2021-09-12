namespace MediaCat.Services {
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;
    using MediaCat.Core.Services.Localization;
    using MediaCat.Core.Services.Localization.Readers;

    /// <summary>A class for translating text between multiple languages. This is a dummy version for use with the WPF designer.</summary>
    public sealed class I18NMock : II18N {

        public string this[string key] => Get(key);

        public Language Language { get; set; }
        public List<Language> Languages { get; }
        public string Locale { get; set; }
        public string Variant { get; set; }
        public string FallbackLocale { get; set; }
        public string UnknownKeySymbol { get; set; }

#pragma warning disable CS0067 // warning CS0067: The event 'I18NMock.PropertyChanged' is never used
        public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

        public ILocaleReader Reader { get; }

        public string LocaleRelativePath { get; }

        private static string GetSourcePath([CallerFilePath] string from = null) => from;

        public I18NMock(string localePath, ILocaleReader reader = null) {
            this.LocaleRelativePath = localePath;
            this.Reader = reader ?? new JsonTreeReader();
        }

        private string Filepath {
            get {
                string souceFilepath = Path.GetDirectoryName(GetSourcePath());
                return Path.GetFullPath(Path.Combine(souceFilepath, LocaleRelativePath));
            }
        }

        public string Get(string key, params object[] args) {

            if (File.Exists(Filepath)) {
                using (Stream stream = File.OpenRead(Filepath)) {
                    Dictionary<string, string> translations = Reader.Read(stream);

                    if (translations.TryGetValue(key, out string value))
                        return args.Length == 0 ? value : string.Format(value, args);

                }
            }

            return $"${key}$";
        }

        public string GetOrNull(string key, params object[] args) {
            return Get(key, args);
        }

        #region Unused

        public string GetDefaultLocale() {
            return string.Empty;
        }

        public II18N Init(string initialLocale = null, string initialVariant = null) {
            return this;
        }

        public II18N RegisterProvider(ILocaleProvider provider) {
            return this;
        }

        public II18N RegisterReader(ILocaleReader reader, string extension) {
            return this;
        }

        #endregion

    }

}

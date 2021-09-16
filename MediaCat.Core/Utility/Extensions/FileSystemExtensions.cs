namespace MediaCat.Core.Utility.Extensions {
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Security.Cryptography;
    using System.Threading;

    public static class FileSystemExtensions {

        public static bool IsFullPath(this IPath src, string path) {
            return src.IsPathRooted(path) && !src.GetPathRoot(path).Equals(src.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public static bool CanBeRelative(this IPath src, string path, string baseDirectory) {
            return path.StartsWith(baseDirectory);
        }

        public static string TryMakeRelative(this IPath src, string path, string baseDirectory) {
            if (src.CanBeRelative(path, baseDirectory)) {
                if (path.Length < baseDirectory.Length + 1) {
                    return $".{src.DirectorySeparatorChar}";
                } else {
                    return $".{src.DirectorySeparatorChar}{path.Substring(baseDirectory.Length + 1)}";
                }
            }
            return path;
        }

        /// <summary>
        /// An EnumerateFiles that works the same way as Directory.EnumerateFiles, but has support for cancellation tokens.
        /// </summary>
        public static IEnumerable<string> EnumerateFiles(this IDirectory src, string directory, string searchPattern, System.IO.SearchOption searchOption, CancellationToken ct) {

            foreach (string file in src.EnumerateFiles(directory, searchPattern, searchOption)) {
                if (ct.IsCancellationRequested)
                    break;

                yield return file;
            }

        }

        /// <summary>
        /// A brute force approach to test if the specified directory is writable by creating a temp file at that location. Note: This will update the directory date modified value.
        /// </summary>
        /// <returns>True if the directory is writable.</returns>
        public static bool IsDirectoryWritable(this IFileSystem fileSystem, string directory) {
            try {
                string tempFilename = fileSystem.Path.Combine(directory, fileSystem.Path.GetRandomFileName());

                using (System.IO.Stream s = fileSystem.File.Create(tempFilename, 1, System.IO.FileOptions.DeleteOnClose)) { }

                return true;
            } catch {
                return false;
            }
        }

        public static string GetHash<T>(this IFileSystem src, string filepath, int blockSize = 8, CancellationToken ct = default) where T : HashAlgorithm, new() {
            using (System.IO.Stream s = src.File.OpenRead(filepath))
                return s.GetHash<T>(blockSize, ct);
        }

    }
}
namespace MediaCat.Core.Utility.Extensions {
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;

    public static class PathExtensions {

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

    }

    public static class DirectoryExtensions {

        public static IEnumerable<string> EnumerateFiles(this IDirectory src, string directory, string searchPattern, System.IO.SearchOption searchOption, CancellationToken ct) {

            foreach (string file in src.EnumerateFiles(directory, searchPattern, searchOption)) {
                if (ct.IsCancellationRequested)
                    break;

                yield return file;
            }

        }

    }

}
namespace MediaCat.Core.Utility.Extensions {
    using System;
    using System.IO.Abstractions;

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

}
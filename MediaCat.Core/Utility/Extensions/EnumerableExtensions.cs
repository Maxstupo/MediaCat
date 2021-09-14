namespace MediaCat.Core.Utility.Extensions {
    using System;
    using System.Collections.Generic;

    public static class EnumerableExtensions {

        public static void ForEach<T>(this IEnumerable<T> src, Action<T> action) {
            foreach (T item in src)
                action(item);
        }

    }
}

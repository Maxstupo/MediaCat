namespace MediaCat.Core.Utility {
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;

    public static class Utility {

        /// <summary>
        /// Calculates the hash of this stream as a hexadecimal string.
        /// </summary>
        /// <param name="blockSize">The buffer size in megabytes before checking the cancellation token.</param>
        public static string GetHash<T>(this Stream stream, int blockSize = 8, CancellationToken ct = default) where T : HashAlgorithm, new() {
            using (T alogirthm = new T()) {
                byte[] buffer = new byte[1024 * blockSize];

                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0) {
                    if (ct.IsCancellationRequested)
                        return null;
                    alogirthm.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                }
                alogirthm.TransformFinalBlock(buffer, 0, bytesRead);

                StringBuilder sb = new StringBuilder(alogirthm.HashSize / 4);
                foreach (byte c in alogirthm.Hash)
                    sb.AppendFormat("{0:x2}", c);

                return sb.ToString();
            }
        }

    }

}
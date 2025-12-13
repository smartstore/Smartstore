using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace Smartstore;

public static class ByteArrayExtensions
{
    extension(byte[] value)
    {
        /// <summary>
        /// Converts bytes into hex characters.
        /// </summary>
        /// <param name="toUpperCase">Indicates whether to convert to lowercase or uppercase hex characters.</param>
        /// <param name="maxLength">The maximum length of the returned string. <c>null</c> to convert all of <paramref name="value"/>.</param>
        /// <remarks>
        /// ToHexString(true) produces the same result as <see cref="Convert.ToHexString(byte[])"/>.
        /// </remarks>
        [DebuggerStepThrough]
        public string ToHexString(bool toUpperCase = false, int? maxLength = null)
        {
            if (value == null || value.Length <= 0)
            {
                return string.Empty;
            }

            if (maxLength.HasValue && maxLength < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(maxLength), "maxLength must be greater than 1.");
            }

            var sb = new StringBuilder(value.Length * 2);
            var format = toUpperCase ? "X2" : "x2";

            for (var i = 0; i < value.Length; i++)
            {
                sb.Append(value[i].ToString(format));

                if (maxLength > 0 && sb.Length >= maxLength)
                {
                    break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Compresses the input buffer with <see cref="GZipStream"/>
        /// </summary>
        /// <param name="buffer">Decompressed input</param>
        /// <returns>The compressed result</returns>
        public byte[] Zip()
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(value, 0, value.Length);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        /// <summary>
        /// Compresses the input buffer with <see cref="GZipStream"/>
        /// </summary>
        /// <param name="buffer">Decompressed input</param>
        /// <returns>The compressed result</returns>
        public async Task<byte[]> ZipAsync()
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                await zipStream.WriteAsync(value);
                zipStream.Close();
                return compressedStream.ToArray();
            }
        }

        /// <summary>
        /// Decompresses the input buffer with <see cref="GZipStream"/> decompression
        /// </summary>
        /// <param name="buffer">Compressed input</param>
        /// <returns>The decompressed result</returns>
        public byte[] Unzip()
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            using (var compressedStream = new MemoryStream(value))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }

        /// <summary>
        /// Decompresses the input buffer with <see cref="GZipStream"/> decompression
        /// </summary>
        /// <param name="buffer">Compressed input</param>
        /// <returns>The decompressed result</returns>
        public async Task<byte[]> UnzipAsync()
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            using (var compressedStream = new MemoryStream(value))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                await zipStream.CopyToAsync(resultStream);
                return resultStream.ToArray();
            }
        }
    }
}

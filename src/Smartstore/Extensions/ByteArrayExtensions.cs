#nullable enable

using System.Buffers;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace Smartstore;

public static class ByteArrayExtensions
{
    const int CopyBufferSize = 64 * 1024;
    const int CompressionThresholdBytes = 512;

    // Simple codec header to avoid ambiguity.
    const byte CodecNone = 0;
    const byte CodecGZip = 1;

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
        /// Compresses the current byte array using GZip compression, or returns the original data if compression is
        /// not beneficial.
        /// </summary>
        /// <remarks>If the input data is empty or below a predefined threshold, compression is skipped
        /// and the original data is returned with a marker indicating no compression. This method is optimized for
        /// typical JSON payloads and may not compress very small inputs.</remarks>
        /// <param name="level">The compression level to use when compressing the data. Defaults to <see cref="CompressionLevel.Fastest"/>.</param>
        /// <returns>A byte array containing the compressed data with a codec marker, or the original data with a marker if
        /// compression is skipped.</returns>
        public byte[] Zip(CompressionLevel level = CompressionLevel.Fastest)
        {
            Guard.NotNull(value);

            if (value.Length == 0)
                return [CodecNone];

            // Small payloads: skip compression (often faster + smaller overall).
            if (value.Length < CompressionThresholdBytes)
            {
                var result = new byte[1 + value.Length];
                result[0] = CodecNone;
                Buffer.BlockCopy(value, 0, result, 1, value.Length);
                return result;
            }

            // Heuristic capacity: compressed output usually smaller than input for JSON.
            using var ms = new MemoryStream(1 + Math.Min(value.Length, 64 * 1024));
            ms.WriteByte(CodecGZip);

            using (var gzip = new GZipStream(ms, level, leaveOpen: true))
            {
                gzip.Write(value, 0, value.Length);
            }

            return ms.ToArray();
        }

        /// <inheritdoc cref="Zip(byte[], CompressionLevel)" />
        public async Task<byte[]> ZipAsync(CompressionLevel level = CompressionLevel.Fastest, CancellationToken cancelToken = default)
        {
            Guard.NotNull(value);

            if (value.Length == 0)
                return [CodecNone];

            if (value.Length < CompressionThresholdBytes)
            {
                var result = new byte[1 + value.Length];
                result[0] = CodecNone;
                Buffer.BlockCopy(value, 0, result, 1, value.Length);
                return result;
            }

            using var ms = new MemoryStream(1 + Math.Min(value.Length, 64 * 1024));
            ms.WriteByte(CodecGZip);

            await using (var gzip = new GZipStream(ms, level, leaveOpen: true))
            {
                await gzip.WriteAsync(value.AsMemory(), cancelToken).ConfigureAwait(false);
            }

            return ms.ToArray();
        }

        /// <summary>
        /// Decompresses a byte array that may be compressed using GZip or no compression at all, returning the
        /// uncompressed data.
        /// </summary>
        /// <remarks>The first byte of the input array indicates the compression codec used. If the codec
        /// is 'CodecNone', the method returns the input array excluding the codec byte. If the codec is 'CodecGZip',
        /// the method attempts to decompress the data using GZip compression.</remarks>
        /// <returns>A byte array containing the uncompressed data. If the input array is empty or only contains the codec
        /// identifier, an empty array is returned.</returns>
        /// <exception cref="InvalidDataException">Thrown if the input array indicates an unknown compression codec or if the GZip codec is specified but the
        /// compressed payload is missing data.</exception>
        public byte[] Unzip()
        {
            Guard.NotNull(value);

            if (value.Length == 0)
                return [];

            var codec = value[0];

            if (codec == CodecNone)
            {
                if (value.Length == 1)
                    return [];

                var result = new byte[value.Length - 1];
                Buffer.BlockCopy(value, 1, result, 0, result.Length);
                return result;
            }

            if (codec == CodecGZip && value.Length == 1)
                throw new InvalidDataException("Compressed payload is missing data.");

            if (codec != CodecGZip)
                throw new InvalidDataException($"Unknown compression codec: {codec}");

            using var source = new MemoryStream(value, 1, value.Length - 1, writable: false);
            using var gzip = new GZipStream(source, CompressionMode.Decompress);

            // Inflated size unknown. For JSON, 2-4x is a common ballpark.
            using var ms = new MemoryStream((value.Length - 1) * 3);

            CopyToPooled(gzip, ms);

            return ms.ToArray();
        }

        /// <inheritdoc cref="Unzip(byte[])" />
        public async Task<byte[]> UnzipAsync(CancellationToken cancelToken = default)
        {
            Guard.NotNull(value);

            if (value.Length == 0)
                return [];

            var codec = value[0];

            if (codec == CodecNone)
            {
                if (value.Length == 1)
                    return [];

                var result = new byte[value.Length - 1];
                Buffer.BlockCopy(value, 1, result, 0, result.Length);
                return result;
            }

            if (codec == CodecGZip && value.Length == 1)
                throw new InvalidDataException("Compressed payload is missing data.");

            if (codec != CodecGZip)
                throw new InvalidDataException($"Unknown compression codec: {codec}");

            using var source = new MemoryStream(value, 1, value.Length - 1, writable: false);
            await using var gzip = new GZipStream(source, CompressionMode.Decompress);

            using var ms = new MemoryStream((value.Length - 1) * 3);

            await CopyToPooledAsync(gzip, ms, cancelToken).ConfigureAwait(false);

            return ms.ToArray();
        }
    }

    private static void CopyToPooled(Stream source, Stream destination)
    {
        byte[]? buffer = null;

        try
        {
            buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);

            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
            {
                destination.Write(buffer, 0, read);
            }
        }
        finally
        {
            if (buffer != null)
                ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task CopyToPooledAsync(Stream source, Stream destination, CancellationToken cancelToken)
    {
        byte[]? buffer = null;

        try
        {
            buffer = ArrayPool<byte>.Shared.Rent(CopyBufferSize);

            int read;
            while ((read = await source.ReadAsync(buffer, cancelToken).ConfigureAwait(false)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancelToken).ConfigureAwait(false);
            }
        }
        finally
        {
            if (buffer != null)
                ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}

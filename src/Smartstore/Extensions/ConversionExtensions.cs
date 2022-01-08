using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class ConversionExtensions
    {
        #region Object

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Convert<T>(this object value)
        {
            if (!CommonHelper.TryConvert(value, typeof(T), CultureInfo.InvariantCulture, out object result))
            {
                return default;
            }

            return (T)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Convert<T>(this object value, T defaultValue)
        {
            if (!CommonHelper.TryConvert(value, typeof(T), CultureInfo.InvariantCulture, out object result))
            {
                return defaultValue;
            }

            return (T)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Convert<T>(this object value, CultureInfo culture)
        {
            if (!CommonHelper.TryConvert(value, typeof(T), culture, out object result))
            {
                return default;
            }

            return (T)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Convert<T>(this object value, T defaultValue, CultureInfo culture)
        {
            if (!CommonHelper.TryConvert(value, typeof(T), culture, out object result))
            {
                return defaultValue;
            }

            return (T)result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Convert(this object value, Type to)
        {
            if (!CommonHelper.TryConvert(value, to, CultureInfo.InvariantCulture, out object result))
            {
                return null;
                //throw Error.InvalidCast(value?.GetType(), to);
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Convert(this object value, Type to, CultureInfo culture)
        {
            if (!CommonHelper.TryConvert(value, to, culture, out object result))
            {
                return null;
                //throw Error.InvalidCast(value?.GetType(), to);
            }

            return result;
        }

        #endregion

        #region int

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToHex(this int value)
        {
            if (value <= 9)
            {
                return (char)(value + 48);
            }

            return (char)((value - 10) + 97);
        }

        #endregion

        #region String

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt(this string value, int defaultValue = 0)
        {
            if (CommonHelper.TryConvert(value, typeof(int), CultureInfo.InvariantCulture, out object result))
            {
                return (int)result;
            }

            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToChar(this string value, bool unescape = false, char defaultValue = '\0')
        {
            if (value.HasValue() && char.TryParse(unescape ? Regex.Unescape(value) : value, out char result))
            {
                return result;
            }

            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFloat(this string value, float defaultValue = 0)
        {
            if (CommonHelper.TryConvert(value, typeof(float), CultureInfo.InvariantCulture, out object result))
            {
                return (float)result;
            }

            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBool(this string value, bool defaultValue = false)
        {
            if (CommonHelper.TryConvert(value, typeof(bool), out object result))
            {
                return (bool)result;
            }

            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime? ToDateTime(this string value, DateTime? defaultValue)
        {
            return value.ToDateTime(null, defaultValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime? ToDateTime(this string value, string[] formats, DateTime? defaultValue)
        {
            return value.ToDateTime(formats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowWhiteSpaces, defaultValue);
        }

        public static DateTime? ToDateTime(this string value, string[] formats, IFormatProvider provider, DateTimeStyles styles, DateTime? defaultValue)
        {
            DateTime result;

            if (formats.IsNullOrEmpty())
            {
                if (DateTime.TryParse(value, provider, styles, out result))
                {
                    return result;
                }
            }

            if (DateTime.TryParseExact(value, formats, provider, styles, out result))
            {
                return result;
            }

            return defaultValue;
        }

        /// <summary>
        /// Parse ISO-8601 UTC timestamp including milliseconds.
        /// </summary>
        /// <remarks>
        /// Dublicate can be found in HmacAuthentication class.
        /// </remarks>
        public static DateTime? ToDateTimeIso8601(this string value)
        {
            if (value.HasValue())
            {
                if (DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime dt))
                    return dt;

                if (DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dt))
                    return dt;
            }

            return null;
        }

        [DebuggerStepThrough]
        public static Version ToVersion(this string value, Version defaultVersion = null)
        {
            try
            {
                return new Version(value);
            }
            catch
            {
                return defaultVersion ?? new Version("1.0");
            }
        }

        #endregion

        #region Stream

        public static byte[] ToByteArray(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (stream is MemoryStream mem)
            {
                if (mem.TryGetBuffer(out var buffer))
                {
                    return buffer.Array;
                }
                return mem.ToArray();
            }
            else
            {
                try
                {
                    var len = stream.Length;
                    if (len > Int32.MaxValue)
                    {
                        return ToByteArrayCopy(stream);
                    }

                    var buffer = new byte[(int)len];
                    stream.Read(buffer, 0, (int)len);
                    return buffer;
                }
                catch
                {
                    return ToByteArrayCopy(stream);
                }
            }
        }

        public static async Task<byte[]> ToByteArrayAsync(this Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (stream is MemoryStream mem)
            {
                if (mem.TryGetBuffer(out var buffer))
                {
                    return buffer.Array;
                }
                return mem.ToArray();
            }
            else
            {
                try
                {
                    var len = stream.Length;
                    if (len > Int32.MaxValue)
                    {
                        return await ToByteArrayCopyAsync(stream);
                    }

                    var buffer = new byte[(int)len];
                    stream.Read(buffer, 0, (int)len);
                    return buffer;
                }
                catch
                {
                    return await ToByteArrayCopyAsync(stream);
                }
            }
        }

        private static byte[] ToByteArrayCopy(Stream stream, int capacity = 0)
        {
            using (var memStream = new MemoryStream(capacity))
            {
                stream.CopyTo(memStream);
                return memStream.ToArray();
            }
        }

        private static async Task<byte[]> ToByteArrayCopyAsync(Stream stream, int capacity = 0)
        {
            using (var memStream = new MemoryStream(capacity))
            {
                await stream.CopyToAsync(memStream);
                return memStream.ToArray();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AsString(this Stream stream)
        {
            return stream.AsString(Encoding.UTF8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<string> AsStringAsync(this Stream stream)
        {
            return stream.AsStringAsync(Encoding.UTF8);
        }

        public static string AsString(this Stream stream, Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            // convert stream to string
            string result;

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using (var sr = new StreamReader(stream, encoding))
            {
                result = sr.ReadToEnd();
            }

            return result;
        }

        public static async Task<string> AsStringAsync(this Stream stream, Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using (var sr = new StreamReader(stream, encoding))
            {
                var result = await sr.ReadToEndAsync();
                return result;
            }
        }

        #endregion

        #region ByteArray

        public static Stream ToStream(this byte[] bytes)
        {
            return new MemoryStream(bytes);
        }

        /// <summary>
        /// Computes the MD5 hash of a byte array
        /// </summary>
        /// <param name="value">The byte array to compute the hash for</param>
        /// <returns>The hash value</returns>
        [DebuggerStepThrough]
        public static string Hash(this byte[] value, bool toBase64 = false)
        {
            Guard.NotNull(value, nameof(value));

            using MD5 md5 = MD5.Create();

            if (toBase64)
            {
                byte[] hash = md5.ComputeHash(value);
                return System.Convert.ToBase64String(hash);
            }
            else
            {
                using var psb = StringBuilderPool.Instance.Get(out var sb);

                byte[] hashBytes = md5.ComputeHash(value);
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2").ToLower());
                }

                return sb.ToString();
            }
        }

        /// <summary>
        /// Converts bytes into a hex string.
        /// </summary>
        [DebuggerStepThrough]
        public static string ToHexString(this byte[] value, int length = 0)
        {
            if (value == null || value.Length <= 0)
            {
                return string.Empty;
            }

            using var psb = StringBuilderPool.Instance.Get(out var sb);

            foreach (byte b in value)
            {
                sb.Append(b.ToString("x2"));

                if (length > 0 && sb.Length >= length)
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
        public static byte[] Zip(this byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            using (var inStream = new MemoryStream(buffer))
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                inStream.CopyTo(zipStream);
                return compressedStream.ToArray();
            }
        }

        /// <summary>
        /// Compresses the input buffer with <see cref="GZipStream"/>
        /// </summary>
        /// <param name="buffer">Decompressed input</param>
        /// <returns>The compressed result</returns>
        public static async Task<byte[]> ZipAsync(this byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            using (var inStream = new MemoryStream(buffer))
            using (var compressedStream = new MemoryStream())
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                await inStream.CopyToAsync(zipStream);
                return compressedStream.ToArray();
            }
        }

        /// <summary>
        /// Decompresses the input buffer with <see cref="GZipStream"/> decompression
        /// </summary>
        /// <param name="buffer">Compressed input</param>
        /// <returns>The decompressed result</returns>
        public static byte[] Unzip(this byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            using (var compressedStream = new MemoryStream(buffer))
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
        public static async Task<byte[]> UnzipAsync(this byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            using (var compressedStream = new MemoryStream(buffer))
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                await zipStream.CopyToAsync(resultStream);
                return resultStream.ToArray();
            }
        }

        #endregion

        #region IFormattable

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToStringInvariant(this IFormattable source)
        {
            return source.ToString(null, CultureInfo.InvariantCulture);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToStringInvariant(this IFormattable source, string format)
        {
            return source.ToString(format, CultureInfo.InvariantCulture);
        }

        #endregion

        #region Enumerable: Collections/List/Dictionary...

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ToObject<T>(this IDictionary<string, object> values) where T : class
        {
            return (T)values.ToObject(typeof(T));
        }

        public static object ToObject(this IDictionary<string, object> values, Type objectType)
        {
            Guard.NotEmpty(values, nameof(values));
            Guard.NotNull(objectType, nameof(objectType));

            if (!DictionaryConverter.CanCreateType(objectType))
            {
                throw Error.Argument(
                    nameof(objectType),
                    "The type '{0}' must be a class and have a parameterless default constructor in order to deserialize properly.",
                    objectType.FullName);
            }

            return DictionaryConverter.SafeCreateAndPopulate(objectType, values);
        }

        #endregion
    }
}

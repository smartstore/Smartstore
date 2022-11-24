#nullable enable

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Utilities;

namespace Smartstore
{
    public static partial class StringExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ToInt(this string? value, int defaultValue = 0)
        {
            if (ConvertUtility.TryConvert(value, typeof(int), CultureInfo.InvariantCulture, out object? result))
            {
                return (int)result!;
            }

            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char ToChar(this string? value, bool unescape = false, char defaultValue = '\0')
        {
            if (value.HasValue() && char.TryParse(unescape ? Regex.Unescape(value!) : value, out char result))
            {
                return result;
            }

            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ToFloat(this string? value, float defaultValue = 0)
        {
            if (ConvertUtility.TryConvert(value, typeof(float), CultureInfo.InvariantCulture, out object? result))
            {
                return (float)result!;
            }

            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ToBool(this string? value, bool defaultValue = false)
        {
            if (ConvertUtility.TryConvert(value, typeof(bool), out object? result))
            {
                return (bool)result!;
            }

            return defaultValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime? ToDateTime(this string? value, DateTime? defaultValue)
        {
            return ToDateTime(value.AsSpan(), null, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowWhiteSpaces, defaultValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime? ToDateTime(this ReadOnlySpan<char> value, DateTime? defaultValue)
        {
            return ToDateTime(value, null, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowWhiteSpaces, defaultValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime? ToDateTime(this string? value, string[]? formats, DateTime? defaultValue)
        {
            return ToDateTime(value.AsSpan(), formats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowWhiteSpaces, defaultValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime? ToDateTime(this ReadOnlySpan<char> value, string[]? formats, DateTime? defaultValue)
        {
            return ToDateTime(value, formats, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AllowWhiteSpaces, defaultValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime? ToDateTime(this string? value, string[]? formats, IFormatProvider provider, DateTimeStyles styles, DateTime? defaultValue)
        {
            return ToDateTime(value.AsSpan(), formats, provider, styles, defaultValue);
        }

        public static DateTime? ToDateTime(this ReadOnlySpan<char> value, string[]? formats, IFormatProvider? provider, DateTimeStyles styles, DateTime? defaultValue)
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
        public static DateTime? ToDateTimeIso8601(this string? value)
        {
            return ToDateTimeIso8601(value.AsSpan());
        }

        /// <summary>
        /// Parse ISO-8601 UTC timestamp including milliseconds.
        /// </summary>
        public static DateTime? ToDateTimeIso8601(this ReadOnlySpan<char> value)
        {
            if (!value.IsWhiteSpace())
            {
                if (DateTime.TryParseExact(value, "o", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime dt))
                    return dt;

                if (DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out dt))
                    return dt;
            }

            return null;
        }

        [DebuggerStepThrough]
        public static Version ToVersion(this string? value, Version? defaultVersion = null)
        {
            return ToVersion(value.AsSpan(), defaultVersion);
        }

        [DebuggerStepThrough]
        public static Version ToVersion(this ReadOnlySpan<char> value, Version? defaultVersion = null)
        {
            if (Version.TryParse(value, out var version))
            {
                return version;
            }
            
            return defaultVersion ?? new Version("1.0");
        }

        /// <summary>
        /// Encodes all the characters in the specified string into a sequence of bytes.
        /// </summary>
        /// <param name="value">String value to encode</param>
        /// <param name="encoding">The encoder to use. Default: <see cref="Encoding.UTF8"/></param>
        /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
        [DebuggerStepThrough]
        public static byte[] GetBytes(this string value, Encoding? encoding = null)
        {
            return (encoding ?? Encoding.UTF8).GetBytes(value);
        }

        [DebuggerStepThrough]
        public static T ToEnum<T>(this string? value, T defaultValue)
            where T : struct
        {
            return Enum.TryParse<T>(value, out var enumValue) ? enumValue : defaultValue;
        }

        [DebuggerStepThrough]
        public static T ToEnum<T>(this ReadOnlySpan<char> value, T defaultValue)
            where T : struct
        {
            return Enum.TryParse<T>(value, out var enumValue) ? enumValue : defaultValue;
        }

        /// <summary>
        /// Computes the xxHash of the input string. xxHash is an extremely fast non-cryptographic Hash algorithm.
        /// </summary>
        /// <param name="value">The input string</param>
        /// <returns>xxHash</returns>
        [DebuggerStepThrough]
        public static string? XxHash(this string? value)
        {
            if (value.IsEmpty())
            {
                return value;
            }

            return $"{XxHashUnsafe.ComputeHash(value):X}";
        }

        /// <summary>
        /// </summary>
        [DebuggerStepThrough]
        [Obsolete("Microsoft recommends SHA256 or SHA512 class instead of MD5. Use MD5 only for compatibility with legacy applications and data.")]
        public static string? Hash(this string? value, Encoding encoding, bool toBase64 = false)
        {
            if (value.IsEmpty())
            {
                return value;
            }

            using var md5 = MD5.Create();
            byte[] data = encoding.GetBytes(value!);

            if (toBase64)
            {
                byte[] hash = md5.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
            else
            {
                return md5.ComputeHash(data).ToHexString();
            }
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? UrlEncode(this string? value)
            => HttpUtility.UrlEncode(value);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? UrlDecode(this string? value)
            => HttpUtility.UrlDecode(value);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? AttributeEncode(this string? value)
            => HttpUtility.HtmlAttributeEncode(value);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? HtmlEncode(this string? value)
            => HttpUtility.HtmlEncode(value);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? HtmlDecode(this string? value)
            => HttpUtility.HtmlDecode(value);

        public static string? EncodeJsString(this string? value)
            => EncodeJsString(value, '"', true);

        public static string? EncodeJsString(this string? value, char delimiter, bool appendDelimiters)
        {
            var result = value.HasValue()
                ? JavaScriptEncoder.Default.Encode(value!)
                : value.EmptyNull();

            if (appendDelimiters)
            {
                result = delimiter + result + delimiter;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string? SanitizeHtmlId(this string? value)
        {
            return TagBuilder.CreateSanitizedId(value, "_");
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] ToIntArray(this string? value)
        {
            return value.Convert<int[]>() ?? Array.Empty<int>();
        }
    }
}

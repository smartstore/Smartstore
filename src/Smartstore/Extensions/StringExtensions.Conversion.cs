using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Utilities;

namespace Smartstore
{
    public static partial class StringExtensions
    {
        /// <summary>
        /// Encodes all the characters in the specified string into a sequence of bytes.
        /// </summary>
        /// <param name="value">String value to encode</param>
        /// <param name="encoding">The encoder to use. Default: <see cref="Encoding.UTF8"/></param>
        /// <returns>A byte array containing the results of encoding the specified set of characters.</returns>
        [DebuggerStepThrough]
        public static byte[] GetBytes(this string value, Encoding encoding = null)
        {
            return (encoding ?? Encoding.UTF8).GetBytes(value);
        }
        
        [DebuggerStepThrough]
        public static T ToEnum<T>(this string value, T defaultValue)
            where T : struct
        {
            if (Enum.TryParse<T>(value, out var enumValue))
            {
                return enumValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Computes the xxHash of the input string. xxHash is an extremely fast non-cryptographic Hash algorithm.
        /// </summary>
        /// <param name="value">The input string</param>
        /// <returns>xxHash</returns>
        [DebuggerStepThrough]
        public static string XxHash(this string value)
        {
            if (value.IsEmpty())
                return value;

            return $"{XxHashUnsafe.ComputeHash(value):X}";
        }

        /// <summary>
        /// </summary>
        [DebuggerStepThrough]
        [Obsolete("Microsoft recommends SHA256 or SHA512 class instead of MD5. Use MD5 only for compatibility with legacy applications and data.")]
        public static string Hash(this string value, Encoding encoding, bool toBase64 = false)
        {
            if (value.IsEmpty())
                return value;

            using var md5 = MD5.Create();
            byte[] data = encoding.GetBytes(value);

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
        public static string UrlEncode(this string value)
            => HttpUtility.UrlEncode(value);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string UrlDecode(this string value)
            => HttpUtility.UrlDecode(value);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AttributeEncode(this string value)
            => HttpUtility.HtmlAttributeEncode(value);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string HtmlEncode(this string value)
            => HttpUtility.HtmlEncode(value);

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string HtmlDecode(this string value)
            => HttpUtility.HtmlDecode(value);

        public static string EncodeJsString(this string value)
            => EncodeJsString(value, '"', true);

        public static string EncodeJsString(this string value, char delimiter, bool appendDelimiters)
        {
            var result = value.HasValue() ? JavaScriptEncoder.Default.Encode(value) : value.EmptyNull();

            if (appendDelimiters)
            {
                result = delimiter + result + delimiter;
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string SanitizeHtmlId(this string value)
        {
            return TagBuilder.CreateSanitizedId(value, "_");
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToValidFileName(this string input, string replacement = "-")
        {
            return string.Join(
                replacement ?? "-",
                input.ToSafe().Tokenize(Path.GetInvalidFileNameChars()));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToValidPath(this string input, string replacement = "-")
        {
            return string.Join(
                replacement ?? "-",
                input.ToSafe().Tokenize(Path.GetInvalidPathChars()));
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] ToIntArray(this string s)
        {
            return s.SplitSafe(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => int.Parse(v))
                .ToArray();
        }

        [DebuggerStepThrough]
        public static bool ToIntArrayContains(this string s, int value, bool defaultValue)
        {
            if (s == null)
                return defaultValue;
            var arr = s.ToIntArray();
            if (arr == null || arr.Length <= 0)
                return defaultValue;

            return arr.Contains(value);
        }
    }
}

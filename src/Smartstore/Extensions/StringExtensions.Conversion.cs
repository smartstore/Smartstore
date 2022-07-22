using System.Diagnostics;
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
        [DebuggerStepThrough]
        public static T ToEnum<T>(this string value, T defaultValue)
        {
            if (!value.HasValue())
            {
                return defaultValue;
            }
            try
            {
                return (T)Enum.Parse(typeof(T), value, true);
            }
            catch (ArgumentException)
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Computes the xxHash of the input string. xxHash is an extremely fast non-cryptographic Hash algorithm.
        /// </summary>
        /// <param name="value">The input string</param>
        /// <returns>xxHash</returns>
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
        {
            return HttpUtility.UrlEncode(value);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string UrlDecode(this string value)
        {
            return HttpUtility.UrlDecode(value);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string AttributeEncode(this string value)
        {
            return HttpUtility.HtmlAttributeEncode(value);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string HtmlEncode(this string value)
        {
            return HttpUtility.HtmlEncode(value);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string HtmlDecode(this string value)
        {
            return HttpUtility.HtmlDecode(value);
        }

        public static string EncodeJsString(this string value)
        {
            return EncodeJsString(value, '"', true);
        }

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

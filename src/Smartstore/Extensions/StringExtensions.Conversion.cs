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

        /// <remarks>
        /// To get equivalent result to PHPs md5 function call Hash("my value", Encoding.ASCII, false).
        /// </remarks>
        [DebuggerStepThrough]
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
                return md5.ComputeHash(data).ToHexString().ToLower();
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

        /// <summary>Smart way to create a HTML attribute with a leading space.</summary>
        /// <param name="value">Name of the attribute.</param>
        /// <param name="name"></param>
        /// <param name="htmlEncode"></param>
        public static string ToAttribute(this string value, string name, bool htmlEncode = true)
        {
            if (name.IsEmpty())
                return "";

            if (value == "" && name != "value" && !name.StartsWith("data"))
                return "";

            if (name == "maxlength" && (value == "" || value == "0"))
                return "";

            if (name == "checked" || name == "disabled" || name == "multiple")
            {
                if (value == "" || string.Compare(value, "false", true) == 0)
                    return "";
                value = (string.Compare(value, "true", true) == 0 ? name : value);
            }

            if (name.StartsWith("data"))
                name = name.Insert(4, "-");

            return string.Format(" {0}=\"{1}\"", name, htmlEncode ? HttpUtility.HtmlEncode(value) : value);
        }

        [DebuggerStepThrough]
        public static string Slugify(this string value, bool allowSpace = false, char[] allowChars = null)
        {
            string res = string.Empty;
            using var psb = StringBuilderPool.Instance.Get(out var sb);

            try
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    bool space = false;
                    char ch;

                    for (int i = 0; i < value.Length; ++i)
                    {
                        ch = value[i];

                        if (ch == ' ' || ch == '-')
                        {
                            if (allowSpace && ch == ' ')
                                sb.Append(' ');
                            else if (!space)
                                sb.Append('-');
                            space = true;
                            continue;
                        }

                        space = false;

                        if ((ch >= 48 && ch <= 57) || (ch >= 65 && ch <= 90) || (ch >= 97 && ch <= 122) || ch == '_')
                        {
                            sb.Append(ch);
                            continue;
                        }

                        if (allowChars != null && allowChars.Contains(ch))
                        {
                            sb.Append(ch);
                            continue;
                        }

                        if ((int)ch >= 128)
                        {
                            switch (ch)
                            {
                                case 'ä': sb.Append("ae"); break;
                                case 'ö': sb.Append("oe"); break;
                                case 'ü': sb.Append("ue"); break;
                                case 'ß': sb.Append("ss"); break;
                                case 'Ä': sb.Append("AE"); break;
                                case 'Ö': sb.Append("OE"); break;
                                case 'Ü': sb.Append("UE"); break;
                                default:
                                    var c2 = ch.TryRemoveDiacritic();
                                    if ((c2 >= 'a' && c2 <= 'z') || (c2 >= 'A' && c2 <= 'Z'))
                                    {
                                        sb.Append(c2);
                                    }
                                    break;
                            }
                        }
                    }   // for

                    if (sb.Length > 0)
                    {
                        res = sb.ToString().Trim(new char[] { ' ', '-' });

                        Regex pat = new(@"(-{2,})"); // remove double SpaceChar
                        res = pat.Replace(res, "-");
                        res = res.Replace("__", "_");
                    }
                }
            }
            catch (Exception ex)
            {
                ex.Dump();
            }

            return (res.Length > 0 ? res : "null");
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

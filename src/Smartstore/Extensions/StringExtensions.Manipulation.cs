#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Smartstore.Utilities.Html;

namespace Smartstore
{
    public static partial class StringExtensions
    {
        private static readonly Regex _rgInvalidXmlChars = new(@"[^\u0009\u000A\u000D\u0020-\uD7FF\uE000-\uFFFD]", RegexOptions.Compiled);

        /// <summary>
        /// Formats a string to an invariant culture
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="objects">The objects.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatInvariant(this string format, params object?[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);

        /// <summary>
        /// Formats a string to an invariant culture
        /// </summary>
        /// <param name="format">The parsed format string as <see cref="CompositeFormat"/>.</param>
        /// <param name="objects">The objects.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatInvariant(this CompositeFormat format, params object?[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);

        /// <summary>
        /// Formats a string to the current culture.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="objects">The objects.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatCurrent(this string format, params object?[] args)
            => string.Format(CultureInfo.CurrentCulture, format, args);

        /// <summary>
        /// Formats a string to the current culture.
        /// </summary>
        /// <param name="format">The parsed format string as <see cref="CompositeFormat"/>.</param>
        /// <param name="objects">The objects.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatCurrent(this CompositeFormat format, params object?[] args)
            => string.Format(CultureInfo.CurrentCulture, format, args);

        /// <summary>
        /// Formats a string to the current UI culture.
        /// </summary>
        /// <param name="format">The format string.</param>
        /// <param name="objects">The objects.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatCurrentUI(this string format, params object?[] args)
            => string.Format(CultureInfo.CurrentUICulture, format, args);

        /// <summary>
        /// Formats a string to the current UI culture.
        /// </summary>
        /// <param name="format">The parsed format string as <see cref="CompositeFormat"/>.</param>
        /// <param name="objects">The objects.</param>
        /// <returns></returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string FormatCurrentUI(this CompositeFormat format, params object?[] args)
            => string.Format(CultureInfo.CurrentUICulture, format, args);

        [DebuggerStepThrough]
        public static string FormatWith(this string format, IFormatProvider? provider, params object?[] args)
            => string.Format(provider, format, args);

        [DebuggerStepThrough]
        public static string FormatWith(this CompositeFormat format, IFormatProvider? provider, params object?[] args)
            => string.Format(provider, format, args);

        /// <summary>
        /// Mask by replacing characters with asterisks.
        /// </summary>
        /// <param name="value">The string</param>
        /// <param name="length">Number of characters to leave untouched.</param>
        /// <returns>The mask string</returns>
        [DebuggerStepThrough]
        [return: NotNullIfNotNull(nameof(value))]
        public static string? Mask(this string? value, int length)
        {
            if (!string.IsNullOrEmpty(value))
            {
                return string.Concat(value.AsSpan(0, length), new string('*', value.Length - length));
            }

            return value;
        }

        /// <summary>
        /// Ensures that a string only contains numeric values
        /// </summary>
        /// <param name="str">Input string</param>
        /// <returns>Input string with only numeric values, empty string if input is null or empty</returns>
        [DebuggerStepThrough]
        public static string EnsureNumericOnly(this string? str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return string.Empty;
            }
            
            return new string(str.Where(char.IsDigit).ToArray());
        }

        [DebuggerStepThrough]
        [return: NotNullIfNotNull(nameof(value))]
        public static string? Truncate(this string? value, int maxLength, string end = "")
        {
            Guard.NotNull(end);
            Guard.IsPositive(maxLength);

            int lenSubStr = maxLength - end.Length;

            if (lenSubStr <= 0)
            {
                throw new ArgumentException("Length of suffix string is greater than or equal to maxLength", nameof(maxLength));
            }

            if (value != null && value.Length > maxLength)
            {
                return value[..lenSubStr].Trim() + end;
            }
            else
            {
                return value;
            }
        }

        /// <summary>
        /// Removes all redundant whitespace (empty lines, double space etc.).
        /// Use ~! literal to keep whitespace wherever necessary.
        /// </summary>
        /// <param name="input">Input</param>
        /// <returns>The compacted string</returns>
        public static string Compact(this string input, bool removeEmptyLines = false)
        {
            Guard.NotNull(input);

            var sb = new StringBuilder(input.Length);
            var lines = ReadLines(input.Trim(), true, removeEmptyLines).ToArray();

            foreach (var line in lines)
            {
                var len = line.Length;
                var sbLine = new StringBuilder(len);
                var isChar = false;
                int i;

                for (i = 0; i < len; i++)
                {
                    var c = line[i];
                    bool eof = i == len - 1;
                    bool isLiteral;

                    if (char.IsWhiteSpace(c))
                    {
                        // Space, Tab etc.
                        if (isChar)
                        {
                            // If last char not empty, append the space.
                            sbLine.Append(' ');
                        }

                        isChar = false;
                    }
                    else
                    {
                        // Char or Literal (~!)

                        isLiteral = c == '~' && !eof && line[i + 1] == '!';
                        isChar = true;

                        if (isLiteral)
                        {
                            sbLine.Append(' ');
                            i++; // skip next "!" char
                        }
                        else
                        {
                            sbLine.Append(c);
                        }
                    }
                }

                // Append the compacted and trimmed line
                sb.AppendLine(sbLine.ToString().Trim().Trim(','));
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Ensure that a string starts with a given char.
        /// </summary>
        /// <param name="value">The target string</param>
        /// <param name="prefix">The char the target string should start with</param>
        /// <returns>The resulting string</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string EnsureStartsWith(this string value, char prefix)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.StartsWith(prefix) ? value : (prefix + value);
        }

        /// <summary>
        /// Ensure that a string starts with a string.
        /// </summary>
        /// <param name="value">The target string</param>
        /// <param name="prefix">The string the target string should start with</param>
        /// <param name="comparison">Comparison rule</param>
        /// <returns>The resulting string</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string EnsureStartsWith(this string value, string prefix, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (value.Length == 0)
            {
                return prefix;
            }

            if (prefix.Length == 0)
            {
                return value;
            }

            var valueSpan = value.AsSpan();
            var affixSpan = prefix.AsSpan();

            if (valueSpan.StartsWith(affixSpan, comparison))
            {
                return value;
            }

            if (prefix.Length > 1)
            {
                var offset = 1;

                while (offset < prefix.Length)
                {
                    var partialMatch = valueSpan.StartsWith(affixSpan[offset..], comparison);
                    if (partialMatch)
                    {
                        return affixSpan[..offset].ToString() + value;
                    }

                    offset++;
                }
            }

            return prefix + value;
        }

        /// <summary>
        /// Ensures the target string ends with the specified char.
        /// </summary>
        /// <param name="suffix">The char the target string should end with.</param>
        /// <param name="value">The value.</param>
        /// <returns>The target string with the value string at the end.</returns>
        [DebuggerStepThrough]
        public static string EnsureEndsWith(this string value, char suffix)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.EndsWith(suffix) ? value : (value + suffix);
        }

        /// <summary>
        /// Ensures the target string ends with the specified string.
        /// </summary>
        /// <param name="suffix">The target.</param>
        /// <param name="value">The value.</param>
        /// <param name="comparison">Comparison rule</param>
        /// <returns>The target string with the value string at the end.</returns>
        [DebuggerStepThrough]
        public static string EnsureEndsWith(this string value, string suffix, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (suffix == null)
            {
                throw new ArgumentNullException(nameof(suffix));
            }

            if (value.Length == 0)
            {
                return suffix;
            }

            if (suffix.Length == 0)
            {
                return value;
            }

            var valueSpan = value.AsSpan();
            var suffixSpan = suffix.AsSpan();

            if (valueSpan.EndsWith(suffixSpan, comparison))
            {
                return value;
            }

            if (suffix.Length > 1)
            {
                var offset = 0;

                while (offset < suffix.Length - 1)
                {
                    var partialMatch = valueSpan.EndsWith(suffixSpan[..(offset + 1)], comparison);
                    if (partialMatch)
                    {
                        return value + suffixSpan[(offset + 1)..].ToString();
                    }

                    offset++;
                }
            }

            return value + suffix;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RemoveHtml(this string source)
        {
            return HtmlUtility.StripTags(source).Trim().HtmlDecode()!;
        }

        [return: NotNullIfNotNull(nameof(value))]
        public static string? RemoveEncloser(this string? value, string? encloser, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (value.IsEnclosedIn(encloser, comparison))
            {
                int len = encloser!.Length / 2;
                return value!.Substring(
                    len,
                    value.Length - (len * 2));
            }

            return value;
        }

        public static string RemoveEncloser(this string value, string start, string end, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (value.IsEnclosedIn(start, end, comparison))
                return value.Substring(
                    start.Length,
                    value.Length - (start.Length + end.Length));

            return value;
        }

        /// <summary>
        /// Appends a string and uses the delimiter if the string is not empty.
        /// </summary>
        /// <param name="value">Source string</param>
        /// <param name="append">The string to be appended</param>
        /// <param name="delimiter">Delimiter</param>
        [DebuggerStepThrough]
        public static string Grow(this string? value, string? append, string delimiter = " ")
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.IsNullOrEmpty(append) ? string.Empty : append;
            }

            if (string.IsNullOrEmpty(append))
            {
                return string.IsNullOrEmpty(value) ? string.Empty : value;
            }

            return value + delimiter + append;
        }

        /// <summary>
        /// Appends a string and uses the delimiter if the string is not empty.
        /// </summary>
        /// <param name="sb">Target string builder</param>
        /// <param name="append">The string to be appended</param>
        /// <param name="delimiter">Delimiter</param>
        [DebuggerStepThrough]
        public static void Grow(this StringBuilder sb, string? append, string delimiter = " ")
        {
            if (!string.IsNullOrWhiteSpace(append))
            {
                if (sb.Length > 0 && delimiter != null)
                {
                    sb.Append(delimiter);
                }

                sb.Append(append);
            }
        }

        /// <summary>
        /// Left-pads a string. Always returns empty string if source <paramref name="value"/> is null or empty.
        /// </summary>
        [DebuggerStepThrough]
        public static string LeftPad(this string? value, string? format = null, char pad = ' ', int count = 1)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (count < 1)
            {
                return value;
            }

            var left = new string(pad, count);
            var right = value;

            if (!string.IsNullOrWhiteSpace(format))
            {
                right = string.Format(CultureInfo.InvariantCulture, format, value);
            }

            if (right.StartsWith(left))
            {
                return right;
            }
            else
            {
                return left + right;
            }
        }

        /// <summary>
        /// Right-pads a string. Always returns empty string if source <paramref name="value"/> is null or empty.
        /// </summary>
        [DebuggerStepThrough]
        public static string RightPad(this string? value, string? format = null, char pad = ' ', int count = 1)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }


            if (count < 1)
            {
                return value;
            }

            var left = value;
            var right = new string(pad, count);

            if (!string.IsNullOrWhiteSpace(format))
            {
                left = string.Format(CultureInfo.InvariantCulture, format, value);
            }

            if (left.EndsWith(right))
            {
                return left;
            }
            else
            {
                return left + right;
            }
        }

        /// <summary>
        /// Replaces digits in a string with culture native digits (if digit substitution for culture is required)
        /// </summary>
        //[DebuggerStepThrough]
        public static string ReplaceNativeDigits(this string value, IFormatProvider? provider = null)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            provider ??= NumberFormatInfo.CurrentInfo;
            var nfi = NumberFormatInfo.GetInstance(provider);

            if (nfi.DigitSubstitution == DigitShapes.None)
            {
                return value;
            }

            var nativeDigits = nfi.NativeDigits;

            var result = RegularExpressions.IsDigit.Replace(value, m => nativeDigits[Convert.ToByte(m.Value)]);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RegexRemove(this string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline)
        {
            return Regex.Replace(input, pattern, string.Empty, options);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string RegexReplace(this string input, string pattern, string replacement, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline)
        {
            return Regex.Replace(input, pattern, replacement, options);
        }

        [DebuggerStepThrough]
        public static string RemoveInvalidXmlChars(this string value)
        {
            if (value.IsEmpty())
            {
                return value;
            }

            return _rgInvalidXmlChars.Replace(value, string.Empty);
        }

        [DebuggerStepThrough]
        public static string ReplaceCsvChars(this string value)
        {
            if (value.IsEmpty())
            {
                return string.Empty;
            }

            return value
                .Replace(';', ',')
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Replace("'", string.Empty);
        }

        [DebuggerStepThrough]
        [return: NotNullIfNotNull(nameof(input))]
        public static string? HighlightKeywords(this string? input, string? keywords, string preMatch = "<strong>", string postMatch = "</strong>")
        {
            Guard.NotNull(preMatch);
            Guard.NotNull(postMatch);

            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(keywords))
            {
                return input;
            }

            var pattern = string.Join('|', keywords.Tokenize(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Regex.Escape)
                .Distinct());

            if (!string.IsNullOrWhiteSpace(pattern))
            {
                var rg = new Regex(pattern, RegexOptions.IgnoreCase);
                input = rg.Replace(input, m => preMatch + m.Value.EmptyNull().HtmlEncode() + postMatch);
            }

            return input;
        }

        /// <summary>
        /// Capitalizes the word.
        /// </summary>
        /// <param name="word">The word to capitalize.</param>
        /// <returns>The Capitalized word.</returns>
        [return: NotNullIfNotNull(nameof(word))]
        public static string? Capitalize(this string? word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return word;
            }

            return word[..1].ToUpper() + word[1..].ToLower();
        }

        /// <summary>
        /// Revers of <see cref="Capitalize"/>
        /// </summary>
        /// <param name="word">string. The word to un-capitalize.</param>
        /// <returns>The uncapitalized word</returns>
        [return: NotNullIfNotNull(nameof(word))]
        public static string? Uncapitalize(this string? word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return word;
            }

            return word[..1].ToLower() + word[1..];
        }

        public static string RemoveDiacritics(this string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            for (int i = 0; i < normalized.Length; i++)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(normalized[i]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(normalized[i]);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}

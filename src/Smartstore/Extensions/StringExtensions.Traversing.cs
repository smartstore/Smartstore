#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Primitives;
using Smartstore.Utilities;

namespace Smartstore
{
    public static partial class StringExtensions
    {
        /// <summary>
        /// Splits the input string by carriage return.
        /// </summary>
        /// <param name="input">The string to split</param>
        /// <returns>A sequence with string items per line</returns>
        [Obsolete("Call the ReadLines() method instead.")]
        public static IEnumerable<string> GetLines(this string? input, bool trimLines = false, bool removeEmptyLines = false)
            => ReadLines(input, trimLines, removeEmptyLines);

        /// <summary>
        /// Splits the input string by carriage return.
        /// </summary>
        /// <param name="input">The string to split</param>
        /// <returns>A sequence with string items per line</returns>
        public static IEnumerable<string> ReadLines(this string? input, bool trimLines = false, bool removeEmptyLines = false)
        {
            if (input.IsEmpty())
            {
                yield break;
            }

            using var sr = new StringReader(input!);
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                var segment = new StringSegment(line);

                if (trimLines)
                {
                    segment = segment.Trim();
                }

                if (removeEmptyLines && IsEmpty(line))
                {
                    continue;
                }

                yield return segment.Value!;
            }
        }

        /// <summary>
        /// Replaces pascal casing with spaces. For example "CustomerId" would become "Customer Id".
        /// Strings that already contain spaces are ignored.
        /// </summary>
        /// <param name="value">String to split</param>
        /// <returns>The string after being split</returns>
        [DebuggerStepThrough]
        [return: NotNullIfNotNull(nameof(value))]
        public static string? SplitPascalCase(this string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var sb = new ValueStringBuilder(value.Length + 5);
            char[] ca = value.ToCharArray();

            sb.Append(ca[0]);

            for (int i = 1; i < ca.Length - 1; i++)
            {
                char c = ca[i];
                if (char.IsUpper(c) && (char.IsLower(ca[i + 1]) || char.IsLower(ca[i - 1])))
                {
                    sb.Append(' ');
                }
                sb.Append(c);
            }

            if (ca.Length > 1)
            {
                sb.Append(ca[^1]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Splits a string into a string array
        /// </summary>
        /// <param name="input">String value to split</param>
        /// <param name="separator">If <c>null</c> then value is searched for a common delimiter like pipe, semicolon or comma</param>
        /// <returns>Separated string tokens</returns>
        [DebuggerStepThrough]
        public static IEnumerable<string> SplitSafe(this string? input, string? separator, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        {
            if (string.IsNullOrEmpty(input))
            {
                return Enumerable.Empty<string>();
            }

            // Do not use separator.IsEmpty() here because whitespace like " " is a valid separator.
            // an empty separator "" returns array with value.
            if (separator == null)
            {
                // Find separator
                for (var i = 0; i < input.Length; i++)
                {
                    var c = input[i];
                    if (c == ';' || c == ',' || c == '|')
                    {
                        // Split by common delims
                        return Tokenize(input, c, options);
                    }
                    if (c == '\r' && (i + 1) < input.Length & input[i + 1] == '\n')
                    {
                        // Split by lines
                        return input.ReadLines(false, true);
                    }
                }

                if (options.HasFlag(StringSplitOptions.TrimEntries))
                {
                    input = input.Trim();
                }

                return options.HasFlag(StringSplitOptions.RemoveEmptyEntries) && string.IsNullOrWhiteSpace(input)
                    ? Enumerable.Empty<string>()
                    : new[] { input };
            }
            else
            {
                return separator.Length == 1
                    ? Tokenize(input, separator[0], options)
                    : input.Split(new string[] { separator }, options);
            }
        }

        /// <summary>
        /// Splits a string into a string array
        /// </summary>
        /// <param name="input">String value to split</param>
        /// <param name="separator">The char to separate by</param>
        /// <returns>Separated string tokens</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerable<string> SplitSafe(this string? input, char separator, StringSplitOptions options = StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        {
            return string.IsNullOrEmpty(input)
                ? Enumerable.Empty<string>()
                : Tokenize(input, separator, options);
        }

        /// <summary>
        /// Splits a string into two strings
        /// </summary>
        /// <returns>true: success, false: failure</returns>
        [DebuggerStepThrough]
        public static bool SplitToPair(this string? value, [MaybeNullWhen(false)] out string leftPart, out string rightPart, string? delimiter, bool splitAfterLast = false)
        {
            leftPart = value;
            rightPart = string.Empty;

            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(delimiter))
            {
                return false;
            }

            var idx = splitAfterLast
                ? value.LastIndexOf(delimiter)
                : value.IndexOf(delimiter);

            if (idx == -1)
            {
                return false;
            }

            leftPart = value[..idx];
            rightPart = value[(idx + delimiter.Length)..];

            return true;
        }

        /// <summary>
        /// Tokenizes/splits a <see cref="string"/> into <see cref="StringSegment"/>s.
        /// </summary>
        /// <param name="input">The <see cref="string"/> to split.</param>
        /// <param name="separator">The separator controlling the split.</param>
        /// <param name="options">One of the enumeration values that determines whether the split operation should omit empty substrings from the return value.</param>
        public static IEnumerable<string> Tokenize(this string input, char separator, StringSplitOptions options = StringSplitOptions.None)
        {
            return Tokenize(input, new[] { separator }, options);
        }

        /// <summary>
        /// Tokenizes/splits a <see cref="string"/> into <see cref="StringSegment"/>s.
        /// </summary>
        /// <param name="input">The <see cref="string"/> to split.</param>
        /// <param name="separators">The collection of separator <see cref="char"/>s controlling the split.</param>
        public static IEnumerable<string> Tokenize(this string input, params char[] separators)
        {
            return Tokenize(input, separators, StringSplitOptions.None);
        }

        /// <summary>
        /// Tokenizes/splits a <see cref="string"/> into <see cref="StringSegment"/>s.
        /// </summary>
        /// <param name="input">The <see cref="string"/> to split.</param>
        /// <param name="separators">The collection of separator <see cref="char"/>s controlling the split.</param>
        /// <param name="options">One of the enumeration values that determines whether the split operation should omit empty substrings from the return value.</param>
        public static IEnumerable<string> Tokenize(this string input, char[] separators, StringSplitOptions options)
        {
            IEnumerable<StringSegment> tokenizer = options.HasFlag(StringSplitOptions.TrimEntries)
                ? new TrimmingTokenizer(input, separators)
                : new StringTokenizer(input, separators);

            if (options.HasFlag(StringSplitOptions.RemoveEmptyEntries))
            {
                return tokenizer.Where(x => x.Length > 0).Select(x => x.Value!);
            }

            return tokenizer.Select(x => x.Value!);
        }
    }
}
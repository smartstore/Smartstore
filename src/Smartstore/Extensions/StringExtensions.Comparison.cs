#nullable enable

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Smartstore.Utilities;

namespace Smartstore
{
    public static partial class StringExtensions
    {
        #region ...NoCase

        /// <summary>
        /// Determines whether this instance and given <paramref name="other"/> have the same value (ignoring case)
        /// </summary>
        /// <param name="value">The string to check equality.</param>
        /// <param name="other">The second string to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of the comparing parameter is the same as this string; otherwise, <c>false</c>.
        /// </returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsNoCase(this string? value, string? other)
        {
            return string.Compare(value, other, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Determines whether this instance and given <paramref name="other"/> have the same value (ignoring case)
        /// </summary>
        /// <param name="value">The span to check equality.</param>
        /// <param name="other">The second span to compare.</param>
        /// <returns>
        /// <c>true</c> if the value of the comparing parameter is the same as this span; otherwise, <c>false</c>.
        /// </returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EqualsNoCase(this ReadOnlySpan<char> value, ReadOnlySpan<char> other)
        {
            return value.CompareTo(other, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Determines whether the beginning of this string instance matches the specified
        /// string when compared ignoring case.
        /// </summary>
        /// <param name="value">The string to compare.</param>
        /// <param name="other">The second string to compare.</param>
        /// <returns>true if this instance begins with value; otherwise false.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWithNoCase(this string value, string other)
        {
            return value.StartsWith(other, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the beginning of this string instance matches the specified
        /// string when compared ignoring case.
        /// </summary>
        /// <param name="value">The string to compare.</param>
        /// <param name="other">The second string to compare.</param>
        /// <returns>true if this instance begins with value; otherwise false.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWithNoCase(this ReadOnlySpan<char> value, ReadOnlySpan<char> other)
        {
            return value.StartsWith(other, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the end of this string instance matches the specified
        /// string when compared ignoring case.
        /// </summary>
        /// <param name="value">The string to compare.</param>
        /// <param name="other">The second string to compare.</param>
        /// <returns>true if this instance ends with value; otherwise false.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWithNoCase(this string value, string other)
        {
            return value.EndsWith(other, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the ends of this string instance matches the specified
        /// string when compared ignoring case.
        /// </summary>
        /// <param name="value">The string to compare.</param>
        /// <param name="other">The second string to compare.</param>
        /// <returns>true if this instance ends with value; otherwise false.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWithNoCase(this ReadOnlySpan<char> value, ReadOnlySpan<char> other)
        {
            return value.EndsWith(other, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a value indicating whether the <paramref name="other"/> 
        /// string occurs within this string (ignoring case).
        /// </summary>
        /// <param name="other">The string to seek.</param>
        /// <returns>true if <paramref name="other"/> occurs within this string; otherwise false.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsNoCase(this string value, string other)
        {
            return value.Contains(other, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a value indicating whether the <paramref name="other"/> 
        /// string occurs within this string (ignoring case).
        /// </summary>
        /// <param name="other">The string to seek.</param>
        /// <returns>true if <paramref name="other"/> occurs within this string; otherwise false.</returns>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsNoCase(this ReadOnlySpan<char> value, ReadOnlySpan<char> other)
        {
            return value.Contains(other, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        /// <summary>
        /// Determines whether the string is null, empty or all whitespace.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Determines whether the string is all white space. Empty string will return false.
        /// </summary>
        /// <param name="value">The string to test whether it is all white space.</param>
        /// <returns>
        /// 	<c>true</c> if the string is all white space; otherwise, <c>false</c>.
        /// </returns>
        [DebuggerStepThrough]
        public static bool IsWhiteSpace(this string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value.Length == 0)
            {
                return false;
            }

            for (int i = 0; i < value.Length; i++)
            {
                if (!char.IsWhiteSpace(value[i]))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the string contains at least one non-whitespace char.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasValue(this string? value)
        {
            return !string.IsNullOrWhiteSpace(value);
        }

        /// <summary>
        /// Determines whether the span contains at least one non-whitespace char.
        /// </summary>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasValue(this ReadOnlySpan<char> value)
        {
            return !value.IsWhiteSpace();
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWebUrl(this string? value, bool schemeIsOptional = false)
        {
            return IsWebUrlInternal(value, schemeIsOptional);
        }

        private static bool IsWebUrlInternal(string? value, bool schemeIsOptional)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            
            value = value.Trim().ToLowerInvariant();

            if (schemeIsOptional && value.StartsWith("//"))
            {
                value = "http:" + value;
            }

            return Uri.IsWellFormedUriString(value, UriKind.Absolute) &&
                (value.StartsWith("http://") || value.StartsWith("https://") || value.StartsWith("ftp://"));
        }

        [DebuggerStepThrough]
        public static bool IsEmail(this string? value)
        {
            return IsEmail(value.AsSpan());
        }

        [DebuggerStepThrough]
        public static bool IsEmail(this ReadOnlySpan<char> value)
        {
            if (value.IsWhiteSpace())
            {
                return false;
            }

            // Only return true if there is only 1 '@' character
            // and it is neither the first nor the last character
            int index = value.IndexOf('@');

            return
                index > 0 &&
                index != value.Length - 1 &&
                index == value.LastIndexOf('@');
        }

        [DebuggerStepThrough]
        public static bool IsNumeric(this string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return ConvertUtility.TryConvert(value, typeof(decimal), CultureInfo.InvariantCulture, out _);
        }

        [DebuggerStepThrough]
        public static bool IsAlpha(this string value)
        {
            return RegularExpressions.IsAlpha.IsMatch(value);
        }

        [DebuggerStepThrough]
        public static bool IsAlphaNumeric(this string value)
        {
            return RegularExpressions.IsAlphaNumeric.IsMatch(value);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnclosedIn(this string? value, string? enclosedIn, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return IsEnclosedIn(value.AsSpan(), enclosedIn.AsSpan(), comparison);
        }

        [DebuggerStepThrough]
        public static bool IsEnclosedIn(this ReadOnlySpan<char> value, ReadOnlySpan<char> enclosedIn, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            if (enclosedIn.IsWhiteSpace())
            {
                return false;
            }

            if (enclosedIn.Length == 1)
            {
                return IsEnclosedIn(value, enclosedIn, enclosedIn, comparison);
            }

            if (enclosedIn.Length % 2 == 0)
            {
                int len = enclosedIn.Length / 2;
                return IsEnclosedIn(
                    value,
                    enclosedIn[..len],
                    enclosedIn.Slice(len, len),
                    comparison);

            }

            return false;
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnclosedIn(this string value, string start, string end, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return value.StartsWith(start, comparison) && value.EndsWith(end, comparison);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnclosedIn(this ReadOnlySpan<char> value, ReadOnlySpan<char> start, ReadOnlySpan<char> end, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
        {
            return value.StartsWith(start, comparison) && value.EndsWith(end, comparison);
        }

        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMatch(this string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline)
        {
            return Regex.IsMatch(input, pattern, options);
        }

        [DebuggerStepThrough]
        public static bool IsMatch(this string input, string pattern, out Match match, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Multiline)
        {
            match = Regex.Match(input, pattern, options);
            return match.Success;
        }
    }
}

using System.Buffers;
using Smartstore.Collections;

namespace Smartstore
{
    public static partial class NaturalSortExtensions
    {
        /// <summary>
        /// Sorts the strings of a sequence in a natural, human-friendly, ascending order.
        /// </summary>
        /// <example>
        /// alphabetical: z1, z11, z2.
        /// natural: z1, z2, z11.
        /// </example>
        /// <remarks>
        /// Should only be applied to materialized and localized data, otherwise the sorting may become broken.
        /// </remarks>
        public static IOrderedEnumerable<TSource> OrderNaturalBy<TSource>(this IEnumerable<TSource> source,
            Func<TSource, string> keySelector,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
            => source.OrderBy(keySelector, new NaturalSorter(comparison));

        /// <summary>
        /// Sorts the strings of a sequence in a natural, human-friendly, descending order.
        /// </summary>
        /// <example>
        /// alphabetical: z2, z11, z1.
        /// natural: z11, z2, z1.
        /// </example>
        /// <remarks>
        /// Should only be applied to materialized and localized data, otherwise the sorting may become broken.
        /// </remarks>
        public static IOrderedEnumerable<TSource> OrderNaturalByDescending<TSource>(this IEnumerable<TSource> source,
            Func<TSource, string> keySelector,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
            => source.OrderByDescending(keySelector, new NaturalSorter(comparison));


        /// <summary>
        /// Performs a subsequent ordering of the strings in a sequence in a natural, human-friendly, ascending order.
        /// </summary>
        /// <example>
        /// alphabetical: z2, z11, z1.
        /// natural: z11, z2, z1.
        /// </example>
        /// <remarks>
        /// Should only be applied to materialized and localized data, otherwise the sorting may become broken.
        /// </remarks>
        public static IOrderedEnumerable<TSource> ThenNaturalBy<TSource>(this IOrderedEnumerable<TSource> source,
            Func<TSource, string> keySelector,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
            => keySelector != null ? source.ThenBy(keySelector, new NaturalSorter(comparison)) : source;

        /// <summary>
        /// Performs a subsequent ordering of the strings in a sequence in a natural, human-friendly, descending order.
        /// </summary>
        /// <example>
        /// alphabetical: z2, z11, z1.
        /// natural: z11, z2, z1.
        /// </example>
        /// <remarks>
        /// Should only be applied to materialized and localized data, otherwise the sorting may become broken.
        /// </remarks>
        public static IOrderedEnumerable<TSource> ThenNaturalByDescending<TSource>(this IOrderedEnumerable<TSource> source,
            Func<TSource, string> keySelector,
            StringComparison comparison = StringComparison.OrdinalIgnoreCase)
            => keySelector != null ? source.ThenByDescending(keySelector, new NaturalSorter(comparison)) : source;
    }
}

namespace Smartstore.Collections
{
    /// <summary>
    /// Origin source: https://github.com/tompazourek/NaturalSort.Extension
    /// It has been modified to pass all unit tests (see NaturalSortTests).
    /// </summary>
    internal class NaturalSorter : IComparer<string>
    {
        // Token values (not an enum as a performance micro-optimization)
        const byte TokenNone = 0;
        const byte TokenOther = 1;
        const byte TokenDigits = 2;
        const byte TokenLetters = 3;

        /// <summary>
        /// String comparison used for comparing strings.
        /// Used if <see cref="_stringComparer" /> is null.
        /// </summary>
        private readonly StringComparison _stringComparison;

        /// <summary>
        /// String comparer used for comparing strings.
        /// </summary>
        private readonly IComparer<string> _stringComparer;

        /// <summary>
        /// Constructs comparer with a <seealso cref="StringComparison" /> as the inner mechanism.
        /// Prefer this to <see cref="NaturalSorter(IComparer{string})" /> if possible.
        /// </summary>
        /// <param name="stringComparison">String comparison to use</param>
        public NaturalSorter(StringComparison stringComparison)
            => _stringComparison = stringComparison;

        /// <summary>
        /// Constructs comparer with a <seealso cref="IComparer{T}" /> string comparer as the inner mechanism.
        /// Prefer <see cref="NaturalSorter(StringComparison)" /> if possible.
        /// </summary>
        /// <param name="stringComparer">String comparer to wrap</param>
        public NaturalSorter(IComparer<string> stringComparer)
            => _stringComparer = stringComparer;

        public int Compare(string str1, string str2)
        {
            if (str1 == str2) return 0;
            if (str1 == null) return -1;
            if (str2 == null) return 1;

            int strLength1 = str1.Length;
            int strLength2 = str2.Length;
            int startIndex1 = 0;
            int startIndex2 = 0;

            while (true)
            {
                // Get next token for both strings
                int endIndex1 = GetNextTokenEndIndex(str1, startIndex1, strLength1, out byte token1);
                int endIndex2 = GetNextTokenEndIndex(str2, startIndex2, strLength2, out byte token2);

                // Different token kinds decide immediately
                int tokenCompare = token1.CompareTo(token2);
                if (tokenCompare != 0) return tokenCompare;

                // No more tokens -> equal
                if (token1 == TokenNone) return 0;

                int rangeLength1 = endIndex1 - startIndex1;
                int rangeLength2 = endIndex2 - startIndex2;

                if (token1 == TokenDigits)
                {
                    // Optionally extend across ".###" thousand groups ONLY for "number + unit" shapes
                    int extEnd1 = TryExtendThousandsForUnits(str1, startIndex1, endIndex1, strLength1);
                    int extEnd2 = TryExtendThousandsForUnits(str2, startIndex2, endIndex2, strLength2);

                    var span1 = str1.AsSpan(startIndex1, extEnd1 - startIndex1);
                    var span2 = str2.AsSpan(startIndex2, extEnd2 - startIndex2);

                    bool hasSeparator1 = ContainsSeparator(span1);
                    bool hasSeparator2 = ContainsSeparator(span2);

                    if (hasSeparator1 || hasSeparator2)
                    {
                        // Extract only digits; return pooled arrays ONLY if they were rented
                        ReadOnlySpan<char> digits1 = ExtractDigits(span1, out char[] buf1, out bool rented1);
                        ReadOnlySpan<char> digits2 = ExtractDigits(span2, out char[] buf2, out bool rented2);
                        try
                        {
                            int digitCompare = CompareDigitSpans(digits1, digits2);
                            if (digitCompare != 0) return digitCompare;
                        }
                        finally
                        {
                            if (rented1 && buf1 is not null) ArrayPool<char>.Shared.Return(buf1);
                            if (rented2 && buf2 is not null) ArrayPool<char>.Shared.Return(buf2);
                        }
                    }
                    else
                    {
                        int digitCompare = CompareDigitSpans(span1, span2);
                        if (digitCompare != 0) return digitCompare;
                    }

                    // Advance indices to the extended ends
                    endIndex1 = extEnd1;
                    endIndex2 = extEnd2;
                }
                else if (_stringComparer is not null)
                {
                    // Compare non-digit tokens using provided comparer
                    string tokenString1 = str1.Substring(startIndex1, rangeLength1);
                    string tokenString2 = str2.Substring(startIndex2, rangeLength2);
                    int stringCompare = _stringComparer.Compare(tokenString1, tokenString2);
                    if (stringCompare != 0) return stringCompare;
                }
                else
                {
                    // Allocation-free string comparison
                    int minLength = Math.Min(rangeLength1, rangeLength2);
                    int stringCompare = string.Compare(
                        str1, startIndex1,
                        str2, startIndex2,
                        minLength, _stringComparison
                    );
                    if (stringCompare == 0) stringCompare = rangeLength1 - rangeLength2;
                    if (stringCompare != 0) return stringCompare;
                }

                startIndex1 = endIndex1;
                startIndex2 = endIndex2;
            }
        }

        private static int GetNextTokenEndIndex(string str, int startIndex, int strLength, out byte token)
        {
            token = TokenNone;
            if (startIndex >= strLength) return startIndex;

            token = GetTokenFromChar(str[startIndex]);
            int endIndex = startIndex + 1;

            if (token == TokenDigits)
            {
                // Consume contiguous digits only; do not cross '.' or ',' here.
                while (endIndex < strLength && char.IsDigit(str[endIndex]))
                    endIndex++;
            }
            else
            {
                while (endIndex < strLength && GetTokenFromChar(str[endIndex]) == token)
                    endIndex++;
            }

            return endIndex;
        }

        /// <summary>
        /// Extends a digit token across ".###" groups ONLY if that grouped number is directly followed
        /// by whitespace and then a letter (unit-like suffix, e.g., "mm"). This avoids merging versions,
        /// IP addresses, and decimals.
        /// Returns the new end index if accepted; otherwise the original end index.
        /// </summary>
        private static int TryExtendThousandsForUnits(string s, int start, int currentEnd, int len)
        {
            int i = currentEnd;
            int last = currentEnd;
            bool sawGroup = false;

            while (i < len)
            {
                int j = i;
                if (s[j] != '.') break;
                j++; // skip '.'

                // Require exactly three digits after the dot
                if (j + 2 >= len) 
                {
                    sawGroup = false; 
                    break;
                }
                if (!char.IsDigit(s[j]) || !char.IsDigit(s[j + 1]) || !char.IsDigit(s[j + 2]))
                {
                    sawGroup = false; 
                    break;
                }

                j += 3;
                sawGroup = true;
                last = j;
                i = j; // continue scanning for further ".###"
            }

            if (!sawGroup) return currentEnd;

            // Accept only if followed by (one or more) spaces and then a letter (unit)
            int k = last;
            bool sawWs = false;
            while (k < len && IsSpaceGroupSep(s[k])) { k++; sawWs = true; }
            if (sawWs && k < len && char.IsLetter(s[k]))
                return last; // do not include trailing whitespace/letters

            // Otherwise reject (e.g., "v1.100", "192.168.0.1", "1.23")
            return currentEnd;
        }

        private static bool IsSeparator(char c) 
            => c == '.' || c == ',';

        // Space-like characters used when validating a unit suffix.
        private static bool IsSpaceGroupSep(char c)
            => c == ' ' || c == '\u00A0' || c == '\u202F' || c == '\u2009'; // space, NBSP, NNBSP, thin

        private static bool ContainsSeparator(ReadOnlySpan<char> span)
        {
            foreach (char c in span)
                if (IsSeparator(c)) return true;
            return false;
        }

        /// <summary>
        /// Extracts only digits from a span.
        /// For small sizes, returns a new array (NOT returned to pool).
        /// For large sizes, rents from ArrayPool and sets 'rented' = true so the caller can return it.
        /// </summary>
        private static ReadOnlySpan<char> ExtractDigits(ReadOnlySpan<char> span, out char[] buffer, out bool rented)
        {
            int digitCount = 0;
            foreach (char c in span)
                if (char.IsDigit(c)) digitCount++;

            if (digitCount == 0)
            {
                buffer = null;
                rented = false;
                return [];
            }

            if (digitCount <= 128)
            {
                // Use a normal array; do NOT return to ArrayPool
                buffer = new char[digitCount];
                rented = false;
                int index = 0;
                foreach (char c in span)
                    if (char.IsDigit(c)) buffer[index++] = c;

                return new ReadOnlySpan<char>(buffer, 0, digitCount);
            }
            else
            {
                // Rent from the pool and mark as rented
                buffer = ArrayPool<char>.Shared.Rent(digitCount);
                rented = true;
                int index = 0;
                foreach (char c in span)
                    if (char.IsDigit(c)) buffer[index++] = c;

                return new ReadOnlySpan<char>(buffer, 0, digitCount);
            }
        }

        /// <summary>
        /// Compares two digit-only spans as big integers by left-padding with '0'.
        /// If numerically equal, the one that required more padding (fewer digits) sorts smaller.
        /// </summary>
        private static int CompareDigitSpans(ReadOnlySpan<char> span1, ReadOnlySpan<char> span2)
        {
            const char paddingChar = '0';
            int len1 = span1.Length;
            int len2 = span2.Length;
            int max = Math.Max(len1, len2);
            int pad1 = max - len1;
            int pad2 = max - len2;

            for (int i = 0; i < max; i++)
            {
                char d1 = i < pad1 ? paddingChar : span1[i - pad1];
                char d2 = i < pad2 ? paddingChar : span2[i - pad2];

                if (d1 is >= '0' and <= '9' && d2 is >= '0' and <= '9')
                {
                    int cmp = d1.CompareTo(d2);
                    if (cmp != 0) return cmp;
                }
                else
                {
                    // Fallback for non-ASCII digits
                    double n1 = char.GetNumericValue(d1);
                    double n2 = char.GetNumericValue(d2);
                    int cmpNum = n1.CompareTo(n2);
                    if (cmpNum != 0) return cmpNum;

                    int cmp = d1.CompareTo(d2);
                    if (cmp != 0) return cmp;
                }
            }

            return pad1.CompareTo(pad2);
        }

        private static byte GetTokenFromChar(char c)
        {
            if (c >= 'a')
            {
                if (c <= 'z') return TokenLetters;
                if (c < 128) return TokenOther;
                if (char.IsLetter(c)) return TokenLetters;
                if (char.IsDigit(c)) return TokenDigits;
                return TokenOther;
            }

            if (c >= 'A')
            {
                if (c <= 'Z') return TokenLetters;
                return TokenOther;
            }

            if (c is >= '0' and <= '9') return TokenDigits;
            return TokenOther;
        }
    }



    /// <summary>
    /// Source: https://github.com/tompazourek/NaturalSort.Extension
    /// </summary>
    //internal class NaturalSorter : IComparer<string>
    //{
    //    // Token values (not an enum as a performance micro-optimization)
    //    const byte TokenNone = 0;
    //    const byte TokenOther = 1;
    //    const byte TokenDigits = 2;
    //    const byte TokenLetters = 3;

    //    /// <summary>
    //    /// String comparison used for comparing strings.
    //    /// Used if <see cref="_stringComparer" /> is null.
    //    /// </summary>
    //    private readonly StringComparison _stringComparison;

    //    /// <summary>
    //    /// String comparer used for comparing strings.
    //    /// </summary>
    //    private readonly IComparer<string> _stringComparer;

    //    /// <summary>
    //    /// Constructs comparer with a <seealso cref="StringComparison" /> as the inner mechanism.
    //    /// Prefer this to <see cref="NaturalSorter(IComparer{string})" /> if possible.
    //    /// </summary>
    //    /// <param name="stringComparison">String comparison to use</param>
    //    public NaturalSorter(StringComparison stringComparison)
    //        => _stringComparison = stringComparison;

    //    /// <summary>
    //    /// Constructs comparer with a <seealso cref="IComparer{T}" /> string comparer as the inner mechanism.
    //    /// Prefer <see cref="NaturalSorter(StringComparison)" /> if possible.
    //    /// </summary>
    //    /// <param name="stringComparer">String comparer to wrap</param>
    //    public NaturalSorter(IComparer<string> stringComparer)
    //        => _stringComparer = stringComparer;

    //    public int Compare(string str1, string str2)
    //    {
    //        if (str1 == str2)
    //        {
    //            return 0;
    //        }
    //        if (str1 == null)
    //        {
    //            return -1;
    //        }
    //        if (str2 == null)
    //        {
    //            return 1;
    //        }

    //        var strLength1 = str1.Length;
    //        var strLength2 = str2.Length;
    //        var startIndex1 = 0;
    //        var startIndex2 = 0;

    //        while (true)
    //        {
    //            // get next token from string 1
    //            var endIndex1 = startIndex1;
    //            var token1 = TokenNone;
    //            while (endIndex1 < strLength1)
    //            {
    //                var charToken = GetTokenFromChar(str1[endIndex1]);
    //                if (token1 == TokenNone)
    //                {
    //                    token1 = charToken;
    //                }
    //                else if (token1 != charToken)
    //                {
    //                    break;
    //                }

    //                endIndex1++;
    //            }

    //            // get next token from string 2
    //            var endIndex2 = startIndex2;
    //            var token2 = TokenNone;
    //            while (endIndex2 < strLength2)
    //            {
    //                var charToken = GetTokenFromChar(str2[endIndex2]);
    //                if (token2 == TokenNone)
    //                {
    //                    token2 = charToken;
    //                }
    //                else if (token2 != charToken)
    //                {
    //                    break;
    //                }

    //                endIndex2++;
    //            }

    //            // if the token kinds are different, compare just the token kind
    //            var tokenCompare = token1.CompareTo(token2);
    //            if (tokenCompare != 0)
    //            {
    //                return tokenCompare;
    //            }

    //            // now we know that both tokens are the same kind

    //            // didn't find any more tokens, return that they're equal
    //            if (token1 == TokenNone)
    //            {
    //                return 0;
    //            }

    //            var rangeLength1 = endIndex1 - startIndex1;
    //            var rangeLength2 = endIndex2 - startIndex2;

    //            if (token1 == TokenDigits)
    //            {
    //                // compare both tokens as numbers
    //                var maxLength = Math.Max(rangeLength1, rangeLength2);

    //                // both spans will get padded by zeroes on the left to be the same length
    //                const char paddingChar = '0';
    //                var paddingLength1 = maxLength - rangeLength1;
    //                var paddingLength2 = maxLength - rangeLength2;

    //                for (var i = 0; i < maxLength; i++)
    //                {
    //                    var digit1 = i < paddingLength1 ? paddingChar : str1[startIndex1 + i - paddingLength1];
    //                    var digit2 = i < paddingLength2 ? paddingChar : str2[startIndex2 + i - paddingLength2];

    //                    if (digit1 is >= '0' and <= '9' && digit2 is >= '0' and <= '9')
    //                    {
    //                        // both digits are ordinary 0 to 9
    //                        var digitCompare = digit1.CompareTo(digit2);
    //                        if (digitCompare != 0)
    //                        {
    //                            return digitCompare;
    //                        }
    //                    }
    //                    else
    //                    {
    //                        // one or both digits is unicode, compare parsed numeric values, and only if they are same, compare as char
    //                        var digitNumeric1 = char.GetNumericValue(digit1);
    //                        var digitNumeric2 = char.GetNumericValue(digit2);
    //                        var digitNumericCompare = digitNumeric1.CompareTo(digitNumeric2);
    //                        if (digitNumericCompare != 0)
    //                        {
    //                            return digitNumericCompare;
    //                        }

    //                        var digitCompare = digit1.CompareTo(digit2);
    //                        if (digitCompare != 0)
    //                        {
    //                            return digitCompare;
    //                        }
    //                    }
    //                }

    //                // if the numbers are equal, we compare how much we padded the strings
    //                var paddingCompare = paddingLength1.CompareTo(paddingLength2);
    //                if (paddingCompare != 0)
    //                {
    //                    return paddingCompare;
    //                }
    //            }
    //            else if (_stringComparer is not null)
    //            {
    //                // compare both tokens as strings
    //                var tokenString1 = str1.Substring(startIndex1, rangeLength1);
    //                var tokenString2 = str2.Substring(startIndex2, rangeLength2);
    //                var stringCompare = _stringComparer.Compare(tokenString1, tokenString2);
    //                if (stringCompare != 0)
    //                {
    //                    return stringCompare;
    //                }
    //            }
    //            else
    //            {
    //                // use string comparison
    //                var minLength = Math.Min(rangeLength1, rangeLength2);
    //                var stringCompare = string.Compare(str1, startIndex1, str2, startIndex2, minLength, _stringComparison);
    //                if (stringCompare == 0)
    //                {
    //                    stringCompare = rangeLength1 - rangeLength2;
    //                }

    //                if (stringCompare != 0)
    //                {
    //                    return stringCompare;
    //                }
    //            }

    //            startIndex1 = endIndex1;
    //            startIndex2 = endIndex2;
    //        }
    //    }

    //    private static byte GetTokenFromChar(char c)
    //    {
    //        if (c >= 'a')
    //        {
    //            if (c <= 'z')
    //            {
    //                return TokenLetters;
    //            }
    //            else if (c < 128)
    //            {
    //                return TokenOther;
    //            }
    //            else if (char.IsLetter(c))
    //            {
    //                return TokenLetters;
    //            }
    //            else if (char.IsDigit(c))
    //            {
    //                return TokenDigits;
    //            }
    //            else
    //            {
    //                return TokenOther;
    //            }
    //        }
    //        else
    //        {
    //            if (c >= 'A')
    //            {
    //                if (c <= 'Z')
    //                {
    //                    return TokenLetters;
    //                }
    //                else
    //                {
    //                    return TokenOther;
    //                }
    //            }
    //            else
    //            {
    //                if (c is >= '0' and <= '9')
    //                {
    //                    return TokenDigits;
    //                }
    //                else
    //                {
    //                    return TokenOther;
    //                }
    //            }
    //        }
    //    }
    //}
}

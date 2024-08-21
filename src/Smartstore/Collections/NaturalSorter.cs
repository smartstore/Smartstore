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
            => source.ThenBy(keySelector, new NaturalSorter(comparison));

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
            => source.ThenByDescending(keySelector, new NaturalSorter(comparison));
    }
}

namespace Smartstore.Collections
{
    /// <summary>
    /// Source: https://github.com/tompazourek/NaturalSort.Extension
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
            if (str1 == str2)
            {
                return 0;
            }
            if (str1 == null)
            {
                return -1;
            }
            if (str2 == null)
            {
                return 1;
            }

            var strLength1 = str1.Length;
            var strLength2 = str2.Length;
            var startIndex1 = 0;
            var startIndex2 = 0;

            while (true)
            {
                // get next token from string 1
                var endIndex1 = startIndex1;
                var token1 = TokenNone;
                while (endIndex1 < strLength1)
                {
                    var charToken = GetTokenFromChar(str1[endIndex1]);
                    if (token1 == TokenNone)
                    {
                        token1 = charToken;
                    }
                    else if (token1 != charToken)
                    {
                        break;
                    }

                    endIndex1++;
                }

                // get next token from string 2
                var endIndex2 = startIndex2;
                var token2 = TokenNone;
                while (endIndex2 < strLength2)
                {
                    var charToken = GetTokenFromChar(str2[endIndex2]);
                    if (token2 == TokenNone)
                    {
                        token2 = charToken;
                    }
                    else if (token2 != charToken)
                    {
                        break;
                    }

                    endIndex2++;
                }

                // if the token kinds are different, compare just the token kind
                var tokenCompare = token1.CompareTo(token2);
                if (tokenCompare != 0)
                {
                    return tokenCompare;
                }

                // now we know that both tokens are the same kind

                // didn't find any more tokens, return that they're equal
                if (token1 == TokenNone)
                {
                    return 0;
                }

                var rangeLength1 = endIndex1 - startIndex1;
                var rangeLength2 = endIndex2 - startIndex2;

                if (token1 == TokenDigits)
                {
                    // compare both tokens as numbers
                    var maxLength = Math.Max(rangeLength1, rangeLength2);

                    // both spans will get padded by zeroes on the left to be the same length
                    const char paddingChar = '0';
                    var paddingLength1 = maxLength - rangeLength1;
                    var paddingLength2 = maxLength - rangeLength2;

                    for (var i = 0; i < maxLength; i++)
                    {
                        var digit1 = i < paddingLength1 ? paddingChar : str1[startIndex1 + i - paddingLength1];
                        var digit2 = i < paddingLength2 ? paddingChar : str2[startIndex2 + i - paddingLength2];

                        if (digit1 is >= '0' and <= '9' && digit2 is >= '0' and <= '9')
                        {
                            // both digits are ordinary 0 to 9
                            var digitCompare = digit1.CompareTo(digit2);
                            if (digitCompare != 0)
                            {
                                return digitCompare;
                            }
                        }
                        else
                        {
                            // one or both digits is unicode, compare parsed numeric values, and only if they are same, compare as char
                            var digitNumeric1 = char.GetNumericValue(digit1);
                            var digitNumeric2 = char.GetNumericValue(digit2);
                            var digitNumericCompare = digitNumeric1.CompareTo(digitNumeric2);
                            if (digitNumericCompare != 0)
                            {
                                return digitNumericCompare;
                            }

                            var digitCompare = digit1.CompareTo(digit2);
                            if (digitCompare != 0)
                            {
                                return digitCompare;
                            }
                        }
                    }

                    // if the numbers are equal, we compare how much we padded the strings
                    var paddingCompare = paddingLength1.CompareTo(paddingLength2);
                    if (paddingCompare != 0)
                    {
                        return paddingCompare;
                    }
                }
                else if (_stringComparer is not null)
                {
                    // compare both tokens as strings
                    var tokenString1 = str1.Substring(startIndex1, rangeLength1);
                    var tokenString2 = str2.Substring(startIndex2, rangeLength2);
                    var stringCompare = _stringComparer.Compare(tokenString1, tokenString2);
                    if (stringCompare != 0)
                    {
                        return stringCompare;
                    }
                }
                else
                {
                    // use string comparison
                    var minLength = Math.Min(rangeLength1, rangeLength2);
                    var stringCompare = string.Compare(str1, startIndex1, str2, startIndex2, minLength, _stringComparison);
                    if (stringCompare == 0)
                    {
                        stringCompare = rangeLength1 - rangeLength2;
                    }

                    if (stringCompare != 0)
                    {
                        return stringCompare;
                    }
                }

                startIndex1 = endIndex1;
                startIndex2 = endIndex2;
            }
        }

        private static byte GetTokenFromChar(char c)
        {
            if (c >= 'a')
            {
                if (c <= 'z')
                {
                    return TokenLetters;
                }
                else if (c < 128)
                {
                    return TokenOther;
                }
                else if (char.IsLetter(c))
                {
                    return TokenLetters;
                }
                else if (char.IsDigit(c))
                {
                    return TokenDigits;
                }
                else
                {
                    return TokenOther;
                }
            }
            else
            {
                if (c >= 'A')
                {
                    if (c <= 'Z')
                    {
                        return TokenLetters;
                    }
                    else
                    {
                        return TokenOther;
                    }
                }
                else
                {
                    if (c is >= '0' and <= '9')
                    {
                        return TokenDigits;
                    }
                    else
                    {
                        return TokenOther;
                    }
                }
            }
        }
    }
}

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Smartstore.Utilities
{
    /// <summary>
    /// https://stackoverflow.com/a/33554746/5947268
    /// </summary>
    internal static class NumberRangeRegexGenerator
    {
        private static readonly ConcurrentDictionary<string, string> _patternCache = new();

        /// <summary>
        /// Generates a regular expression that matches the numbers
        /// that fall within the range of the given numbers, inclusive.
        /// Assumes the given strings are numbers of the the same length,
        /// and 0-left-pads the resulting expressions, if necessary, to the
        /// same length.
        /// </summary>
        public static string Generate(string min, string max)
        {
            Guard.NotEmpty(min, nameof(min));
            Guard.NotEmpty(max, nameof(max));

            return Generate(min.ToInt(), max.ToInt(), min.Length);
        }

        /// <summary>
        /// Generates a regular expression that matches the numbers
        /// that fall within the range of the given numbers, inclusive.
        /// </summary>
        public static string Generate(int min, int max, int? minWidth = null)
        {
            var cacheKey = $"nr:{min}-{max}-{minWidth}";

            return _patternCache.GetOrAdd(cacheKey, key =>
            {
                var pairs = GetRegexPairs(min, max);
                var regexes = ToRegex(pairs, minWidth ?? min.NumDigits());
                var pattern = string.Join('|', regexes);

                // Add a negative look behind and a negative look ahead in order
                // to avoid that 122-321 is found in 2308.
                return @$"((?<!\d)({pattern})(?!\d))";
            });
        }

        /// <summary>
        /// Return the list of integers that are the paired integers
        /// used to generate the regular expressions for the given
        /// range.Each pair of integers in the list -- 0,1, then 2,3,
        /// etc., represents a range for which a single regular expression
        /// is generated.
        /// </summary>
        private static List<int> GetRegexPairs(int min, int max)
        {
            var pairs = new List<int>();
            var leftPairs = new List<int>();
            var middleStartPoint = FillLeftPairs(leftPairs, min, max);
            var rightPairs = new List<int>();
            int middleEndPoint = FillRightPairs(rightPairs, middleStartPoint, max);

            pairs.AddRange(leftPairs);

            if (middleEndPoint > middleStartPoint)
            {
                pairs.Add(middleStartPoint);
                pairs.Add(middleEndPoint);
            }

            pairs.AddRange(rightPairs);
            return pairs;
        }

        /// <summary>
        /// Return the regular expressions that match the ranges in the given
        /// list of integers. The list is in the form firstRangeStart, firstRangeEnd, 
        /// secondRangeStart, secondRangeEnd, etc. Each regular expression is 0-left-padded,
        /// if necessary, to match strings of the given width.
        /// </summary>
        private static List<string> ToRegex(List<int> pairs, int minWidth = 0)
        {
            var list = new List<string>();
            var format = 'D' + minWidth.ToStringInvariant();

            for (var i = 0; i < pairs.Count; i++)
            {
                var min = pairs[i].ToString(format);
                i++;
                var max = pairs[i].ToString(format);

                list.Add(ToRegex(min, max));
            }

            return list;
        }

        /// <summary>
        /// Return the regular expressions that match the ranges in the given 
        /// list of integers. The list is in the form firstRangeStart, firstRangeEnd, 
        /// secondRangeStart, secondRangeEnd, etc. Each regular expression is 0-left-padded,
        /// if necessary, to match strings of the given width.
        /// </summary>
        private static string ToRegex(string min, string max)
        {
            Debug.Assert(min.Length == max.Length);

            var sb = new StringBuilder();

            for (int pos = 0; pos < min.Length; pos++)
            {
                if (min[pos] == max[pos])
                {
                    sb.Append(min[pos]);
                }
                else
                {
                    sb.Append('[')
                        .Append(min[pos])
                        .Append('-')
                        .Append(max[pos])
                        .Append(']');
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Return the integer at the start of the range that is not covered 
        /// by any pairs added to its list.
        /// </summary>
        private static int FillRightPairs(List<int> rightPairs, int min, int max)
        {
            // The end of the range not covered by pairs
            // from this routine.
            int firstBeginRange = max;

            int y = max;
            int x = GetPreviousBeginRange(y);

            while (x >= min)
            {
                rightPairs.Add(y);
                rightPairs.Add(x);
                y = x - 1;
                firstBeginRange = y;
                x = GetPreviousBeginRange(y);
            }

            rightPairs.Reverse();
            return firstBeginRange;
        }

        /// <summary>
        /// Return the integer at the start of the range that is not covered 
        /// by any pairs added to its list.
        /// </summary>
        private static int FillLeftPairs(List<int> leftPairs, int min, int max)
        {
            int x = min;
            int y = GetNextLeftEndRange(x);

            while (y < max)
            {
                leftPairs.Add(x);
                leftPairs.Add(y);
                x = y + 1;
                y = GetNextLeftEndRange(x);
            }

            return x;
        }

        /// <summary>
        /// Given a number, return the number altered such that any 9s 
        /// at the end of the number remain, and one more 9 replaces 
        /// the number before the other 9s.
        /// </summary>
        private static int GetNextLeftEndRange(int num)
        {
            var chars = num.ToStringInvariant().ToCharArray();
            for (int i = chars.Length - 1; i >= 0; i--)
            {
                if (chars[i] == '0')
                {
                    chars[i] = '9';
                }
                else
                {
                    chars[i] = '9';
                    break;
                }
            }

            return (new string(chars)).ToInt();
        }

        /// <summary>
        /// Given a number, return the number altered such that any 9 
        /// at the end of the number is replaced by a 0, 
        /// and the number preceding any 9s is also replaced by a 0.
        /// </summary>
        private static int GetPreviousBeginRange(int num)
        {
            var chars = num.ToStringInvariant().ToCharArray();
            for (int i = chars.Length - 1; i >= 0; i--)
            {
                if (chars[i] == '9')
                {
                    chars[i] = '0';
                }
                else
                {
                    chars[i] = '0';
                    break;
                }
            }

            return (new string(chars)).ToInt();
        }
    }
}

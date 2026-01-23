using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace Smartstore.Utilities;

/// <summary>
/// https://stackoverflow.com/a/33554746/5947268
/// </summary>
internal static class NumberRangeRegexGenerator
{
    private static readonly ConcurrentDictionary<string, string> _patternCache = new();

    private const string CacheKeyPrefix = "nr:";

    /// <summary>
    /// Generates a regular expression that matches the numbers
    /// that fall within the range of the given numbers, inclusive.
    /// Assumes the given strings are numbers of the the same length,
    /// and 0-left-pads the resulting expressions, if necessary, to the
    /// same length.
    /// </summary>
    public static string Generate(string min, string max)
    {
        Guard.NotEmpty(min);
        Guard.NotEmpty(max);

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
            var ranges = GetRegexRanges(min, max);
            var regexes = ToRegex(ranges, minWidth ?? min.NumDigits());
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
    private static List<(int Min, int Max)> GetRegexRanges(int min, int max)
    {
        // A small range typically yields a small number of regex segments.
        var ranges = new List<(int Min, int Max)>(capacity: 8);

        var middleStartPoint = FillLeftRanges(ranges, min, max);
        int middleEndPoint = FillRightRanges(ranges, middleStartPoint, max);

        if (middleEndPoint > middleStartPoint)
        {
            ranges.Add((middleStartPoint, middleEndPoint));
        }

        // Left ranges were appended first, right ranges appended next, so final order is preserved.
        return ranges;
    }

    /// <summary>
    /// Return the regular expressions that match the ranges in the given
    /// list of integers. The list is in the form firstRangeStart, firstRangeEnd, 
    /// secondRangeStart, secondRangeEnd, etc. Each regular expression is 0-left-padded,
    /// if necessary, to match strings of the given width.
    /// </summary>
    private static List<string> ToRegex(List<(int Min, int Max)> ranges, int minWidth = 0)
    {
        var list = new List<string>(ranges.Count);
        var format = 'D' + minWidth.ToStringInvariant();

        foreach (var (min, max) in ranges)
        {
            list.Add(ToRegex(min.ToString(format), max.ToString(format)));
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

        // Worst case every position is a range: "[0-9]" (5 chars)
        // so a conservative capacity reduces reallocations for unpredictable distributions.
        var sb = new StringBuilder(min.Length * 2);

        for (var pos = 0; pos < min.Length; pos++)
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
    private static int FillRightRanges(List<(int Min, int Max)> ranges, int min, int max)
    {
        // The end of the range not covered by pairs
        // from this routine.
        var firstBeginRange = max;

        var y = max;
        var x = GetPreviousRangeStart(y);

        // Collect right ranges in reverse and insert after left-ranges.
        // Inserting into the end keeps allocations down.
        var tmp = new List<(int Min, int Max)>(capacity: 8);

        while (x >= min)
        {
            tmp.Add((x, y));
            y = x - 1;
            firstBeginRange = y;
            x = GetPreviousRangeStart(y);
        }

        // Reverse once and append.
        for (var i = tmp.Count - 1; i >= 0; i--)
        {
            ranges.Add(tmp[i]);
        }
        return firstBeginRange;
    }

    /// <summary>
    /// Return the integer at the start of the range that is not covered 
    /// by any pairs added to its list.
    /// </summary>
    private static int FillLeftRanges(List<(int Min, int Max)> ranges, int min, int max)
    {
        var x = min;
        var y = GetNextRangeEnd(x);

        while (y < max)
        {
            ranges.Add((x, y));
            x = y + 1;
            y = GetNextRangeEnd(x);
        }

        return x;
    }

    /// <summary>
    /// Given a number, return the number altered such that any 9s 
    /// at the end of the number remain, and one more 9 replaces 
    /// the number before the other 9s.
    /// </summary>
    private static int GetNextRangeEnd(int num)
    {
        var s = num.ToStringInvariant();
        Span<char> chars = s.Length <= 64 ? stackalloc char[s.Length] : new char[s.Length];
        s.AsSpan().CopyTo(chars);

        for (var i = chars.Length - 1; i >= 0; i--)
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

        return new string(chars).ToInt();
    }

    /// <summary>
    /// Given a number, return the number altered such that any 9 
    /// at the end of the number is replaced by a 0, 
    /// and the number preceding any 9s is also replaced by a 0.
    /// </summary>
    private static int GetPreviousRangeStart(int num)
    {
        var s = num.ToStringInvariant();
        Span<char> chars = s.Length <= 64 ? stackalloc char[s.Length] : new char[s.Length];
        s.AsSpan().CopyTo(chars);

        for (var i = chars.Length - 1; i >= 0; i--)
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

        return new string(chars).ToInt();
    }
}

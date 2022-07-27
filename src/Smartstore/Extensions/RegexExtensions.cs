using System.Text;
using System.Text.RegularExpressions;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class RegexExtensions
    {
        public static string ReplaceGroup(this Regex regex, string input, string groupName, string replacement)
        {
            return ReplaceGroupInternal(regex, input, replacement, m => m.Groups[groupName]);
        }

        public static string ReplaceGroup(this Regex regex, string input, int groupNum, string replacement)
        {
            return ReplaceGroupInternal(regex, input, replacement, m => m.Groups[groupNum]);
        }

        private static string ReplaceGroupInternal(this Regex regex, string input, string replacement, Func<Match, Group> groupSelector)
        {
            return regex.Replace(input, match =>
            {
                var group = groupSelector(match);
                var sb = new StringBuilder(input.Length);
                var previousCaptureEnd = 0;

                foreach (var capture in group.Captures.Cast<Capture>())
                {
                    var currentCaptureEnd = capture.Index + capture.Length - match.Index;
                    var currentCaptureLength = capture.Index - match.Index - previousCaptureEnd;

                    sb.Append(match.Value.AsSpan(previousCaptureEnd, currentCaptureLength));
                    sb.Append(replacement);

                    previousCaptureEnd = currentCaptureEnd;
                }

                sb.Append(match.Value.AsSpan(previousCaptureEnd));

                return sb.ToString();
            });
        }

        public static async Task<string> ReplaceAsync(this Regex regex, string input, Func<Match, Task<string>> evaluator)
        {
            Guard.NotNull(regex, nameof(regex));
            Guard.NotNull(input, nameof(input));
            Guard.NotNull(evaluator, nameof(evaluator));

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            var lastIndex = 0;
            var matches = regex.Matches(input);

            if (matches.Count == 0)
            {
                return input;
            }

            foreach (Match match in matches)
            {
                sb.Append(input, lastIndex, match.Index - lastIndex)
                  .Append(await evaluator(match));

                lastIndex = match.Index + match.Length;
            }

            sb.Append(input, lastIndex, input.Length - lastIndex);
            return sb.ToString();
        }
    }
}

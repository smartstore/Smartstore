using System.Text.RegularExpressions;
using Smartstore.Utilities.Html.CodeFormatter;

namespace Smartstore.Forums.Services
{
    public partial class BBCodeHelper
    {
        private static readonly Regex regexBold = new(@"\[b\](.+?)\[/b\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex regexItalic = new(@"\[i\](.+?)\[/i\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex regexUnderLine = new(@"\[u\](.+?)\[/u\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex regexUrl1 = new(@"\[url\=([^\]]+)\]([^\]]+)\[/url\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex regexUrl2 = new(@"\[url\](.+?)\[/url\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex regexQuote = new(@"\[quote(=.+?)?\](.+?)\[/quote\]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Formats the text
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="replaceBold">A value indicating whether to replace Bold</param>
        /// <param name="replaceItalic">A value indicating whether to replace Italic</param>
        /// <param name="replaceUnderline">A value indicating whether to replace Underline</param>
        /// <param name="replaceUrl">A value indicating whether to replace URL</param>
        /// <param name="replaceCode">A value indicating whether to replace Code</param>
        /// <param name="replaceQuote">A value indicating whether to replace Quote</param>
        /// <returns>Formatted text</returns>
        public static string ToHtml(
            string text,
            bool replaceBold = true,
            bool replaceItalic = true,
            bool replaceUnderline = true,
            bool replaceUrl = true,
            bool replaceCode = true,
            bool replaceQuote = true)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (replaceBold)
            {
                // format the bold tags: [b][/b]
                // becomes: <strong></strong>
                text = regexBold.Replace(text, "<strong>$1</strong>");
            }

            if (replaceItalic)
            {
                // format the italic tags: [i][/i]
                // becomes: <em></em>
                text = regexItalic.Replace(text, "<em>$1</em>");
            }

            if (replaceUnderline)
            {
                // format the underline tags: [u][/u]
                // becomes: <u></u>
                text = regexUnderLine.Replace(text, "<u>$1</u>");
            }

            if (replaceUrl)
            {
                // format the url tags: [url=http://www.smartstore.com]my site[/url]
                // becomes: <a href="http://www.smartstore.com">my site</a>
                text = regexUrl1.Replace(text, "<a href=\"$1\" rel=\"nofollow\">$2</a>");

                // format the url tags: [url]http://www.smartstore.com[/url]
                // becomes: <a href="http://www.smartstore.com">http://www.smartstore.com</a>
                text = regexUrl2.Replace(text, "<a href=\"$1\" rel=\"nofollow\">$1</a>");
            }

            if (replaceQuote)
            {
                while (regexQuote.IsMatch(text))
                {
                    text = regexQuote.Replace(text, (m) =>
                    {
                        var from = m.Groups[1].Value;
                        var quote = m.Groups[2].Value;
                        var result = string.Empty;

                        if (quote.HasValue())
                        {
                            if (from.HasValue())
                            {
                                result += $"<span class='forum-quote-from'>{from[1..]}:</span>";
                            }

                            result += $"<blockquote class='forum-quote muted'>{quote}</blockquote>";
                        }

                        return result;
                    });
                }

            }

            if (replaceCode)
            {
                text = CodeFormatHelper.FormatTextSimple(text);
            }

            return text;
        }

        /// <summary>
        /// Removes all quotes from string
        /// </summary>
        /// <param name="str">Source string</param>
        /// <returns>string</returns>
        public static string RemoveQuotes(string str)
        {
            str = Regex.Replace(str, @"\[quote=(.+?)\]", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            str = Regex.Replace(str, @"\[/quote\]", string.Empty, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return str;
        }
    }
}

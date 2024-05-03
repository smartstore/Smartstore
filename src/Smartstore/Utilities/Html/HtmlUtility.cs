#nullable enable

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using AngleSharp;
using AngleSharp.Css.Dom;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Ganss.Xss;

namespace Smartstore.Utilities.Html
{
    /// <summary>
    /// Utility class for html manipulation or creation
    /// </summary>
    public static partial class HtmlUtility
    {
        private readonly static SearchValues<char> _textReplacableChars = SearchValues.Create("\r\n\t");
        private readonly static SearchValues<char> _htmlReplacableChars = SearchValues.Create("<>&");

        private readonly static Regex _rgAnchor = new(@"<a\b[^>]+>([^<]*(?:(?!</a)<[^<]*)*)</a>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private readonly static Regex _rgParaStart = new("<p>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private readonly static Regex _rgParaEnd = new("</p>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        /// <inheritdoc cref="SanitizeHtml(string, HtmlSanitizerOptions, bool)"/>
        [return: NotNullIfNotNull(nameof(html))]
        public static string? SanitizeHtml(string? html, bool isFragment = true)
            => SanitizeHtml(html, HtmlSanitizerOptions.Default, isFragment);

        /// <summary>
        /// Cleans HTML fragments and documents from constructs that can lead to XSS attacks.
        /// </summary>
        /// <remarks>
        /// XSS attacks can occur at several levels within an HTML document or fragment:
        /// <list type="bullet">
        ///     <item>HTML Tags (e.g. the &lt;script&gt; tag)</item>
        ///     <item>HTML attributes (e.g. the "onload" attribute)</item>
        ///     <item>CSS styles (url property values)</item>
        ///     <item>malformed HTML or HTML that exploits parser bugs in specific browsers</item>
        /// </list>
        /// <param name="html">Input HTML</param>
        /// <param name="options">Sanitization options</param>
        /// <param name="isFragment">Whether given HTML is a partial fragment or a document.</param>
        /// <returns>Sanitized HTML</returns>
        [return: NotNullIfNotNull(nameof(html))]
        public static string? SanitizeHtml(string? html, HtmlSanitizerOptions options, bool isFragment = true)
        {
            if (string.IsNullOrEmpty(html))
            {
                return string.Empty;
            }   

            Guard.NotNull(options);

            var sanitizerOptions = new Ganss.Xss.HtmlSanitizerOptions();

            MergeSets(sanitizerOptions.AllowedTags, options.AllowedTags ?? HtmlSanitizerOptions.DefaultAllowedTags, options.DisallowedTags);
            MergeSets(sanitizerOptions.AllowedAttributes, options.AllowedAttributes ?? HtmlSanitizerOptions.DefaultAllowedAttributes, options.DisallowedAttributes);
            MergeSets(sanitizerOptions.UriAttributes, options.UriAttributes ?? HtmlSanitizerOptions.DefaultUriAttributes, null);

            if (options.AllowedCssClasses != null)
            {
                MergeSets(sanitizerOptions.AllowedCssClasses, options.AllowedCssClasses, null);
            }

            var sanitizer = new HtmlSanitizer(sanitizerOptions)
            {
                KeepChildNodes = options.KeepChildNodes,
                AllowDataAttributes = options.AllowDataAttributes
            };

            return isFragment
                ? sanitizer.Sanitize(html)
                : sanitizer.SanitizeDocument(html);

            static void MergeSets(ISet<string> target, ISet<string> allows, ISet<string>? disallows)
            {
                if (disallows == null)
                {
                    target.AddRange(allows);
                }
                else
                {
                    target.AddRange(allows.Except(disallows));
                }
            }
        }

        /// <summary>
        /// Strips all tags from an HTML document or fragment and returns just the text content. Also
        /// removes all <c>script</c>, <c>style</c>, <c>svg</c> and <c>img</c> tags completely from DOM.
        /// </summary>
        /// <param name="html">Input HTML</param>
        /// <returns>Clean text</returns>
        public static string StripTags(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            } 

            var removeTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "script", "style", "svg", "img" };
            var parser = new HtmlParser();

            using var doc = parser.ParseDocument(html);
            List<IElement> removeElements = new();

            foreach (var el in doc.All)
            {
                if (removeTags.Contains(el.TagName))
                {
                    removeElements.Add(el);
                }
            }

            foreach (var el in removeElements)
            {
                el.Remove();
            }

            return doc.Body?.TextContent ?? string.Empty;
        }

        /// <summary>
        /// Checks whether HTML code only contains whitespace stuff (<![CDATA[<p>&nbsp;</p>]]>)
        /// </summary>
        public static bool IsEmptyHtml(string? html)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return true;
            }

            if (html.Length > 500)
            {
                // (perf) we simply assume content if length is larger
                return false;
            }

            var context = BrowsingContext.New(Configuration.Default);
            var parser = context.GetService<IHtmlParser>();
            using var doc = parser!.ParseDocument(html);

            foreach (var el in doc.All)
            {
                switch (el.TagName.ToLower())
                {
                    case "html":
                    case "head":
                    case "br":
                        continue;
                    case "body":
                        if (el.ChildElementCount > 0)
                        {
                            continue;
                        }
                        else
                        {
                            return el.Text().Trim().IsEmpty();
                        }
                    case "p":
                    case "div":
                    case "span":
                        var text = el.Text().Trim();
                        if (text.IsEmpty() || text == "&nbsp;")
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    default:
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Replace anchor text (remove &lt;a&gt; tag from the following url <a href="http://example.com">Name</a> and output only the string "Name")
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Text</returns>
        public static string ReplaceAnchorTags(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
                
            text = _rgAnchor.Replace(text, "$1");
            return text;
        }

        /// <summary>
        /// Converts plain text to HTML.
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Formatted text</returns>
        public static string ConvertPlainTextToHtml(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (text.AsSpan().IndexOfAny(_textReplacableChars) == -1 && !text.Contains("  "))
            {
                // Nothing to replace, return as is.
                return text;
            }

            text = text
                .Replace("\r\n", "<br />")
                .Replace("\r", "<br />")
                .Replace("\n", "<br />")
                .Replace("\t", "&nbsp;&nbsp;")
                .Replace("  ", "&nbsp;&nbsp;");

            return text;
        }

        /// <summary>
        /// Converts HTML to plain text
        /// </summary>
        /// <param name="text">Text</param>
        /// <param name="decode">A value indicating whether to decode text</param>
        /// <param name="replaceAnchorTags">
        /// A value indicating whether to replace anchor text (remove a tag from the following url <a href="http://example.com">Name</a> and output only the string "Name")
        /// </param>
        /// <returns>Formatted text</returns>
        public static string ConvertHtmlToPlainText(string? text, bool decode = false, bool replaceAnchorTags = false)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }  

            if (decode)
            {
                text = HttpUtility.HtmlDecode(text);
            } 

            if (text.AsSpan().IndexOfAny(_htmlReplacableChars) == -1)
            {
                // Nothing to replace, return as is.
                return text;
            }

            text = text
                .Replace("<br>", "\n")
                .Replace("<br >", "\n")
                .Replace("<br />", "\n")
                .Replace("&nbsp;&nbsp;", "\t")
                .Replace("&nbsp;&nbsp;", "  ");

            if (replaceAnchorTags && text.IndexOf('<') > -1)
            {
                text = ReplaceAnchorTags(text);
            }

            return text;
        }

        /// <summary>
        /// Converts an attribute string spec to an html table putting each new line in a TR and each attr name/value in a TD.
        /// </summary>
        /// <param name="text">The text to convert</param>
        /// <returns>The formatted (html) string</returns>
        public static string ConvertPlainTextToTable(string? text, string? tableCssClass = null)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }


            var lines = text.ReadLines(true, true);
            if (!lines.Any())
            {
                return string.Empty;
            }

            var sb = new StringBuilder(text.Length * 4);

            sb.AppendFormat("<table{0}>", tableCssClass.HasValue() ? "class='" + tableCssClass + "'" : "");

            lines.Each(x =>
            {
                sb.Append("<tr>");
                var tokens = x.Split(new char[] { ':' }, 2);

                if (tokens.Length > 1)
                {
                    sb.AppendFormat("<td class='attr-caption'>{0}</td>", tokens[0]);
                    sb.AppendFormat("<td class='attr-value'>{0}</td>", tokens[1]);
                }
                else
                {
                    sb.Append("<td>&nbsp;</td>");
                    sb.AppendFormat("<td class='attr-value'>{0}</td>", tokens[0]);
                }

                sb.Append("</tr>");
            });

            sb.Append("</table>");

            return sb.ToString();
        }

        /// <summary>
        /// Formats an attribute string spec by applying <paramref name="lineFormat"/> to each line
        /// and <paramref name="pairFormat"/> for the name/value pair, where {0} represents name, and {1} represents value.
        /// </summary>
        /// <param name="text">The text to format</param>
        /// <returns>The formatted (html) string</returns>
        public static string FormatPlainText(string? text, string lineFormat = "{0}", string pairFormat = "<span>{0}:</span><span>{1}</span>")
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            var lines = text.ReadLines(true, true);
            if (!lines.Any())
            {
                return string.Empty;
            }

            var sb = new StringBuilder(text.Length * 4);

            lines.Each(x =>
            {
                var tokens = x.Split([':'], 2);
                var pair = tokens.Length > 1 
                    ? string.Format(pairFormat, tokens[0], tokens[1])
                    : string.Format(pairFormat, "&nbsp;", tokens[0]);

                sb.AppendFormat(lineFormat, pair);
            });

            return sb.ToString();
        }

        /// <summary>
        /// Converts text to paragraph
        /// </summary>
        /// <param name="text">Text</param>
        /// <returns>Formatted text</returns>
        public static string ConvertPlainTextToParagraph(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            if (text.IndexOfAny(['<', '\r', '\n']) == -1)
            {
                // Nothing to replace, return as is.
                return text;
            }

            text = _rgParaStart.Replace(text, string.Empty);
            text = _rgParaEnd.Replace(text, "\n");
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            text += "\n\n";
            text = text.Replace("\n\n", "\n");

            var lines = text.Tokenize('\n', StringSplitOptions.TrimEntries);
            var sb = new StringBuilder(text.Length * 2);
            foreach (string str in lines)
            {
                if (str != null && str.Length > 0)
                {
                    sb.AppendFormat("<p>{0}</p>\n", str);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts all occurences of pixel-based inline font-size expression to relative 'em'
        /// </summary>
        /// <param name="html"></param>
        /// <param name="baseFontSizePx"></param>
        /// <returns></returns>
        public static string RelativizeFontSizes(string html, int baseFontSizePx = 16)
        {
            Guard.NotEmpty(html);
            Guard.IsPositive(baseFontSizePx);

            var context = BrowsingContext.New(Configuration.Default.WithCss());
            var parser = context.GetService<IHtmlParser>();
            using var doc = parser!.ParseDocument(html);

            var nodes = doc.QuerySelectorAll("*[style]");
            foreach (var node in nodes)
            {
                var styles = node.GetStyle();
                if (styles.GetFontSize() is string s && s.EndsWith("px"))
                {
                    var size = s.Substring(0, s.Length - 2).Convert<double>();
                    if (size > 0)
                    {
                        //node.Style.FontSize = Math.Round(((double)size / (double)baseFontSizePx), 4) + "em";
                        styles.SetFontSize("{0}em".FormatInvariant(Math.Round(((double)size / (double)baseFontSizePx), 4)));
                    }
                }
            }

            return doc.Body?.InnerHtml ?? string.Empty;
        }
    }
}

using Ganss.XSS;

namespace Smartstore.Utilities.Html
{
    public class HtmlSanitizerOptions
    {
        public static HtmlSanitizerOptions Default { get; } = new();

        public static HtmlSanitizerOptions UserCommentSuitable { get; } = new HtmlSanitizerOptions
        {
            AllowedTags = new[]
            {
                "abbr", "acronym", "address", "b", "big", "blockquote", "br", "cite", "dd", "del", "dfn", "dir", "dl", "dt", "em", "hr", "i", "ins",
                "kbd", "ul", "ol", "li", "pre", "samp", "small", "span", "strike", "strong", "sub", "sup", "tt", "u", "var",
                // Text-level semantics
                "data", "time", "mark", "ruby", "rt", "rp", "bdi", "wbr",
            },
            DisallowedAttributes = new[] { "style", "class" }
        };

        public static ISet<string> DefaultAllowedTags { get; } = HtmlSanitizer.DefaultAllowedTags;
        public static ISet<string> DefaultAllowedAttributes { get; } = HtmlSanitizer.DefaultAllowedAttributes;
        public static ISet<string> DefaultUriAttributes { get; } = HtmlSanitizer.DefaultUriAttributes;

        /// <summary>
        /// Gets or sets a value indicating whether to keep child nodes of elements that are removed. Default is <c>false</c>.
        /// </summary>
        public bool KeepChildNodes { get; set; } = HtmlSanitizer.DefaultKeepChildNodes;

        /// <summary>
        /// Allow all HTML5 data attributes; the attributes prefixed with data-. Default: <c>false</c>.
        /// </summary>
        public bool AllowDataAttributes { get; set; } = HtmlSanitizer.DefaultKeepChildNodes;

        /// <summary>
        /// The allowed tag names such as "a" and "div". When <c>null</c>, uses <see cref="DefaultAllowedTags"/>
        /// </summary>
        public IEnumerable<string> AllowedTags { get; set; }

        /// <summary>
        /// The disallowed tag names such as "a" and "div".
        /// </summary>
        public IEnumerable<string> DisallowedTags { get; set; }

        /// <summary>
        /// The allowed HTML attributes such as "href" and "alt". When <c>null</c>, uses <see cref="DefaultAllowedAttributes"/>
        /// </summary>
        public IEnumerable<string> AllowedAttributes { get; set; }

        /// <summary>
        /// The disallowed HTML attributes such as "href" and "alt".
        /// </summary>
        public IEnumerable<string> DisallowedAttributes { get; set; }

        /// <summary>
        /// The allowed CSS classes. When <c>null</c>, all classes are allowed.
        /// </summary>
        public IEnumerable<string> AllowedCssClasses { get; set; }

        /// <summary>
        /// The HTML attributes that can contain a URI such as "href". When <c>null</c>, uses <see cref="DefaultUriAttributes"/>
        /// </summary>
        public IEnumerable<string> UriAttributes { get; set; }
    }
}

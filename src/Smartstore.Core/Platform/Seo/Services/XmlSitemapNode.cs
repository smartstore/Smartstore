namespace Smartstore.Core.Seo
{
    public class XmlSitemapNode
    {
        public string Loc { get; init; }
        public DateTime? LastMod { get; init; }
        public ChangeFrequency? ChangeFreq { get; init; }
        public float? Priority { get; init; }
        public IEnumerable<LinkEntry> Links { get; init; }

        public class LinkEntry
        {
            /// <summary>
            /// Gets or sets the language code consisting of a two-digit ISO 639-1 language code (e.g. de, en, tr)
            /// and a two-digit ISO 3166-1 Alpha-2 country code (e.g. DE, US, TR). The latter is optional.
            /// </summary>
            public string Lang { get; init; }

            public string Href { get; init; }
        }
    }

    /// <summary>
    /// Represents a sitemap update frequency
    /// </summary>
    public enum ChangeFrequency
    {
        /// <summary>
        /// Always
        /// </summary>
        Always,
        /// <summary>
        /// Hourly
        /// </summary>
        Hourly,
        /// <summary>
        /// Daily
        /// </summary>
        Daily,
        /// <summary>
        /// Weekly
        /// </summary>
        Weekly,
        /// <summary>
        /// Monthly
        /// </summary>
        Monthly,
        /// <summary>
        /// Yearly
        /// </summary>
        Yearly,
        /// <summary>
        /// Never
        /// </summary>
        Never
    }
}

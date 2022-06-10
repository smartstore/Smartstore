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

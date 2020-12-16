namespace Smartstore.Core.Search
{
    /// <summary>
    /// Represents a hint for the facet template to be used.
    /// </summary>
    public enum FacetTemplateHint
    {
        /// <summary>
        /// Render facets as checkboxes.
        /// </summary>
        Checkboxes = 0,

        /// <summary>
        /// Custom facet rendering like color or picture boxes.
        /// </summary>
        Custom,

        /// <summary>
        /// Render facets as a numeric range filter.
        /// </summary>
        NumericRange
    }

    public enum SearchMode
    {
        /// <summary>
        /// Term search
        /// </summary>
        ExactMatch = 0,

        /// <summary>
        /// Prefix term search
        /// </summary>
        StartsWith,

        /// <summary>
        /// Wildcard search
        /// </summary>
        Contains
    }
}

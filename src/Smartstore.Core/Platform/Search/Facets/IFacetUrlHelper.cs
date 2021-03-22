namespace Smartstore.Core.Search.Facets
{
    /// <summary>
    /// Represents a helper to modify facet URLs.
    /// </summary>
    public partial interface IFacetUrlHelper
    {
        /// <summary>
        /// Order of helper implementations.
        /// </summary>
        int Order { get; }

        /// <summary>
        /// Index scope name.
        /// </summary>
        string Scope { get; }

        /// <summary>
        /// Gets the query name.
        /// </summary>
        string GetQueryName(Facet facet);

        /// <summary>
        /// Adds query string parts for facets.
        /// </summary>
        string Add(params Facet[] facets);

        /// <summary>
        /// Removes query string parts for facets.
        /// </summary>
        string Remove(params Facet[] facets);

        /// <summary>
        /// Removes a query string part if the facet is selected, otherwise the part is added.
        /// </summary>
        string Toggle(Facet facet);
    }
}

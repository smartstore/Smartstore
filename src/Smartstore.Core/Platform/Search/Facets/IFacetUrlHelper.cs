using System.Threading.Tasks;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Platform.Search.Facets
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
        Task<string> GetQueryNameAsync(Facet facet);

        /// <summary>
        /// Adds query string parts for facets.
        /// </summary>
        Task<string> AddAsync(params Facet[] facets);

        /// <summary>
        /// Removes query string parts for facets.
        /// </summary>
        Task<string> RemoveAsync(params Facet[] facets);

        /// <summary>
        /// Removes a query string part if the facet is selected, otherwise the part is added.
        /// </summary>
        Task<string> ToggleAsync(Facet facet);
    }
}

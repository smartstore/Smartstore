using System.Collections.Generic;
using System.Linq;

namespace Smartstore.Core.Platform.Search.Facets
{
    /// <summary>
    /// Represents a provider to get facet helpers.
    /// </summary>
    public partial interface IFacetHelperProvider
    {
        /// <summary>
        /// Gets a facet URL helper.
        /// </summary>
        /// <param name="scope">Index scope name.</param>
        /// <returns>Facet URL helper.</returns>
        IFacetUrlHelper GetUrlHelper(string scope);
    }


    public partial class FacetHelperProvider : IFacetHelperProvider
    {
        protected readonly IFacetUrlHelper[] _facetUrlHelpers;

        public FacetHelperProvider(IEnumerable<IFacetUrlHelper> facetUrlHelpers)
        {
            _facetUrlHelpers = facetUrlHelpers.OrderBy(x => x.Order).ToArray();
        }

        public IFacetUrlHelper GetUrlHelper(string scope)
        {
            Guard.NotEmpty(scope, nameof(scope));

            return _facetUrlHelpers.FirstOrDefault(x => x.Scope == scope);
        }
    }
}

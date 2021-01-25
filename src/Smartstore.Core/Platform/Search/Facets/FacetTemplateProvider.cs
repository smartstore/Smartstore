using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Web;

namespace Smartstore.Core.Search.Facets
{
    public class FacetTemplateProvider : IFacetTemplateProvider
    {
        private readonly IEnumerable<IFacetTemplateSelector> _selectors;

        public FacetTemplateProvider(IEnumerable<IFacetTemplateSelector> selectors)
        {
            _selectors = selectors;
        }

        public RouteInfo GetTemplateRoute(FacetGroup facetGroup)
        {
            var route = _selectors
                .OrderByDescending(x => x.Ordinal)
                .Select(x => x.GetTemplateRoute(facetGroup))
                .FirstOrDefault(x => x != null);

            return route;
        }
    }
}

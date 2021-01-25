using Smartstore.Core.Web;

namespace Smartstore.Core.Search.Facets
{
    public interface IFacetTemplateProvider
    {
        RouteInfo GetTemplateRoute(FacetGroup facetGroup);
    }
}

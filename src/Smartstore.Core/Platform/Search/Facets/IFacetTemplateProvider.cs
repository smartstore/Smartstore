using Smartstore.Core.Web;

namespace Smartstore.Core.Search.Facets
{
    public interface IFacetTemplateProvider
    {
        // TODO: (mg) (core) (info) No routing for partial templates anymore in Core. This has to be refactored substantially. TBD.
        RouteInfo GetTemplateRoute(FacetGroup facetGroup);
    }
}

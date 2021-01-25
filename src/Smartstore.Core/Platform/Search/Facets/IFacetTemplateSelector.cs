using Smartstore.Core.Web;
using Smartstore.Domain;

namespace Smartstore.Core.Search.Facets
{
    public interface IFacetTemplateSelector : IOrdered
    {
        // TODO: (mg) (core) (info) No routing for partial templates anymore in Core. This has to be refactored substantially. TBD.
        RouteInfo GetTemplateRoute(FacetGroup facetGroup);
    }
}

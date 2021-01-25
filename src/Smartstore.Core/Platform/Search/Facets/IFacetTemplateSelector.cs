using Smartstore.Core.Web;
using Smartstore.Domain;

namespace Smartstore.Core.Search.Facets
{
    public interface IFacetTemplateSelector : IOrdered
    {
        RouteInfo GetTemplateRoute(FacetGroup facetGroup);
    }
}

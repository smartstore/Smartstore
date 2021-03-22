using Smartstore.Core.Widgets;
using Smartstore.Domain;

namespace Smartstore.Core.Search.Facets
{
    public interface IFacetTemplateSelector : IOrdered
    {
        WidgetInvoker GetTemplateWidget(FacetGroup facetGroup);
    }
}

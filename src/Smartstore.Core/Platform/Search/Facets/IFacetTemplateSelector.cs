using Smartstore.Core.Widgets;

namespace Smartstore.Core.Search.Facets
{
    public interface IFacetTemplateSelector : IOrdered
    {
        WidgetInvoker GetTemplateWidget(FacetGroup facetGroup);
    }
}

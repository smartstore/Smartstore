using Smartstore.Core.Widgets;

namespace Smartstore.Core.Search.Facets
{
    public interface IFacetTemplateProvider
    {
        WidgetInvoker GetTemplateWidget(FacetGroup facetGroup);
    }
}

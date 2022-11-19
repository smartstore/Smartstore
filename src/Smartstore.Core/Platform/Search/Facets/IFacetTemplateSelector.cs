using Smartstore.Core.Widgets;

namespace Smartstore.Core.Search.Facets
{
    public interface IFacetTemplateSelector : IOrdered
    {
        Widget GetTemplateWidget(FacetGroup facetGroup);
    }
}

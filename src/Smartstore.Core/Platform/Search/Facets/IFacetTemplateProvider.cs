using Smartstore.Core.Widgets;

namespace Smartstore.Core.Search.Facets
{
    public interface IFacetTemplateProvider
    {
        Widget GetTemplateWidget(FacetGroup facetGroup);
    }
}

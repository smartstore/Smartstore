using Smartstore.Core.Widgets;

namespace Smartstore.Core.Search.Facets
{
    public class FacetTemplateProvider : IFacetTemplateProvider
    {
        private readonly IEnumerable<IFacetTemplateSelector> _selectors;

        public FacetTemplateProvider(IEnumerable<IFacetTemplateSelector> selectors)
        {
            _selectors = selectors;
        }

        public Widget GetTemplateWidget(FacetGroup facetGroup)
        {
            var widget = _selectors
                .OrderByDescending(x => x.Ordinal)
                .Select(x => x.GetTemplateWidget(facetGroup))
                .FirstOrDefault(x => x != null);

            return widget;
        }
    }
}

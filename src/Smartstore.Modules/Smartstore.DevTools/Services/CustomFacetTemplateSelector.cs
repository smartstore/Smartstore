using Smartstore.Core;
using Smartstore.Core.Search.Facets;
using Smartstore.Core.Widgets;
using Smartstore.DevTools.Components;

namespace Smartstore.DevTools.Services
{
    public class CustomFacetTemplateSelector : IFacetTemplateSelector
    {
        private readonly IWorkContext _workContext;

        public CustomFacetTemplateSelector(IWorkContext workContext)
        {
            _workContext = workContext;
        }

        // Order in case of multiple implementations (like MegaSearchPlus).
        public int Ordinal => 99;

        public Widget GetTemplateWidget(FacetGroup facetGroup)
        {
            // Provide template for catalog only.
            if (facetGroup.Scope != "Catalog")
            {
                return null;
            }

            // Provide template for specifications attributes only.
            if (facetGroup.Kind != FacetGroupKind.Attribute)
            {
                return null;
            }

            // Provide template for admin only (because this is a developer demo).
            if (!_workContext.CurrentCustomer.IsAdmin())
            {
                return null;
            }

            // TODO: filter by what your template is made for.
            // E.g. merchant configures a specification attribute in your plugin, then you can filter by facetGroup.Key == "attrid<ConfiguredId>"
            if (facetGroup.Label.EqualsNoCase("Dimensions") || facetGroup.Label.EqualsNoCase("Maße"))
            {
                return new ComponentWidget(typeof(CustomFacetViewComponent), new { templateName = "MyCustomFacetTemplate", facetGroup });
            }

            return null;
        }
    }
}
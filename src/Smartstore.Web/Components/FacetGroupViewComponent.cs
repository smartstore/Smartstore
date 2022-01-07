using Smartstore.Core.Search.Facets;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Component to render facet templates.
    /// </summary>
    public class FacetGroupViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(FacetGroup facetGroup, string templateName)
        {
            Guard.NotNull(facetGroup, nameof(facetGroup));
            Guard.NotEmpty(templateName, nameof(templateName));

            return View(templateName, facetGroup);
        }
    }
}

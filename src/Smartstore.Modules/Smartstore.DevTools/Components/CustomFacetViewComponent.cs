using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Search.Facets;
using Smartstore.Web.Components;

namespace Smartstore.DevTools.Components
{
    public class CustomFacetViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(FacetGroup facetGroup, string templateName)
        {
            return View(facetGroup);
        }
    }
}
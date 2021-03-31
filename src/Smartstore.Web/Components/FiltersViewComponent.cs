using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog.Search;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Component to render filters for product lists.
    /// </summary>
    public class FiltersViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(CatalogSearchResult model = null)
        {
            if (model == null)
            {
                return Empty();
            }

            return View(model);
        }
    }
}

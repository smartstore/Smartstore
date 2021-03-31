using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog.Search;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Component to render active filters in product lists.
    /// </summary>
    public class ActiveFiltersViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(CatalogSearchResult model)
        {
            Guard.NotNull(model, nameof(model));
            return View(model);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Smartstore.News.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.News.Components
{
    /// <summary>
    /// Component to render news item list via module partial & page builder block.
    /// </summary>
    public class NewsSummaryListViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(NewsItemListModel model)
        {
            return View(model);
        }
    }
}

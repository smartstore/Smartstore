using Microsoft.AspNetCore.Mvc;
using Smartstore.News.Controllers;
using Smartstore.Web.Components;

namespace Smartstore.News.Components
{
    /// <summary>
    /// Component to render news section via page builder block.
    /// </summary>
    public class NewsSummaryViewComponent : SmartViewComponent
    {
        private readonly NewsHelper _helper;

        public NewsSummaryViewComponent(NewsHelper helper)
        {
            _helper = helper;
        }

        public async Task<IViewComponentResult> InvokeAsync(
            bool renderHeading,
            string newsHeading,
            bool disableCommentCount,
            int? maxPostAmount = null,
            bool displayPaging = false,
            int? maxAgeInDays = null)
        {
            var model = await _helper.PrepareNewsItemListModelAsync(renderHeading, newsHeading, disableCommentCount, 0, maxPostAmount, displayPaging, maxAgeInDays);
            model.RssToLinkButton = true;

            return View(model);
        }
    }
}

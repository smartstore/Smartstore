using Microsoft.AspNetCore.Mvc;
using Smartstore.Blog.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.Blog.Components
{
    /// <summary>
    /// Component to render blog summary heading via module partial & page builder block.
    /// </summary>
    public class BlogSummaryHeadingViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(BlogPostListModel model)
        {
            return View(model);
        }
    }
}

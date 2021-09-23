using Microsoft.AspNetCore.Mvc;
using Smartstore.Blog.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.Blog.Components
{
    /// <summary>
    /// Component to render blog post list via module partial & page builder block.
    /// </summary>
    public class BlogSummaryListViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(BlogPostListModel model)
        {
            return View(model);
        }
    }
}

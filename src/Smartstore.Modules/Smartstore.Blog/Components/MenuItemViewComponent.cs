using Microsoft.AspNetCore.Mvc;
using Smartstore.Blog;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Component to render menu item in TopBar ViewComponent in widgte zone header_menu_special.
    /// </summary>
    public class MenuItemViewComponent : SmartViewComponent
    {
        private readonly BlogSettings _blogSettings;

        public MenuItemViewComponent(BlogSettings blogSettings)
        {
            _blogSettings = blogSettings;
        }

        public IViewComponentResult Invoke()
        {
            return View(_blogSettings.Enabled);
        }
    }
}

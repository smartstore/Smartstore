using Microsoft.AspNetCore.Mvc;
using Smartstore.Forums.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.Forums.Components
{
    public class ForumCustomerInfoViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke(ForumCustomerInfoModel model)
        {
            ViewData.TemplateInfo.HtmlFieldPrefix = "CustomProperties[ForumCustomerInfo]";

            return View(model);
        }
    }
}

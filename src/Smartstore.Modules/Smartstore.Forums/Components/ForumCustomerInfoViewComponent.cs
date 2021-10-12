using Microsoft.AspNetCore.Mvc;
using Smartstore.Core;
using Smartstore.Forums.Models.Public;
using Smartstore.Forums.Services;
using Smartstore.Web.Components;

namespace Smartstore.Forums.Components
{
    public class ForumCustomerInfoViewComponent : SmartViewComponent
    {
        private readonly IWorkContext _workContext;

        public ForumCustomerInfoViewComponent(IWorkContext workContext)
        {
            _workContext = workContext;
        }

        public IViewComponentResult Invoke()
        {
            ViewData.TemplateInfo.HtmlFieldPrefix = "CustomProperties[ForumCustomerInfo]";

            var customer = _workContext.CurrentCustomer;
            var model = new ForumCustomerInfoModel
            {
                Signature = customer.GenericAttributes.Get<string>(ForumService.SignatureKey)
            };

            return View(model);
        }
    }
}

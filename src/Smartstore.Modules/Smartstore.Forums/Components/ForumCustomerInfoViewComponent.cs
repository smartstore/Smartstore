using Microsoft.AspNetCore.Mvc;
using Smartstore.Core;
using Smartstore.Forums.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.Forums.Components
{
    /// <summary>
    /// Editable forum signature of a customer injected into the my-account info page.
    /// </summary>
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

using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components
{
    public class GdprConsentViewComponent : SmartViewComponent
    {
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly PrivacySettings _privacySettings;

        public GdprConsentViewComponent(IGenericAttributeService genericAttributeService, PrivacySettings privacySettings)
        {
            _genericAttributeService = genericAttributeService;
            _privacySettings = privacySettings;
        }

        public IViewComponentResult Invoke(bool isSmall)
        {
            if (!_privacySettings.DisplayGdprConsentOnForms)
            {
                return Empty();
            }

            var customer = Services.WorkContext.CurrentCustomer;

            // TODO: (mh) (core) remove test code
            //var db = Services.DbContext;
            //customer = db.Customers.FindById(1);

            var attrs = _genericAttributeService.GetAttributesForEntity(customer);
            var hasConsentedToGdpr = attrs.Get<bool>(SystemCustomerAttributeNames.HasConsentedToGdpr);

            if (hasConsentedToGdpr)
            {
                return Empty();
            }

            var model = new GdprConsentModel
            {
                GdprConsent = false,
                SmallDisplay = isSmall
            };

            return View("GdprConsent", model);
        }
    }
}

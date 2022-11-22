using Smartstore.Core.Catalog;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components
{
    public class TopBarViewComponent : SmartViewComponent
    {
        private readonly CatalogSettings _catalogSettings;
        private readonly CustomerSettings _customerSettings;

        public TopBarViewComponent(CatalogSettings catalogSettings, CustomerSettings customerSettings)
        {
            _catalogSettings = catalogSettings;
            _customerSettings = customerSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var isRegistered = customer.IsRegistered();

            var model = new TopBarModel
            {
                RecentlyAddedProductsEnabled = _catalogSettings.RecentlyAddedProductsEnabled,
                CustomerEmailUsername = isRegistered ? HttpContext.User.Identity.Name : string.Empty,
                IsCustomerImpersonated = Services.WorkContext.CurrentImpersonator != null,
                IsAuthenticated = isRegistered,
                DisplayAdminLink = Services.Permissions.Authorize(Permissions.System.AccessBackend),
                HasContactUsPage = (await Url.TopicAsync("ContactUs")).ToString().HasValue(),
                DisplayLoginLink = _customerSettings.UserRegistrationType != UserRegistrationType.Disabled
            };

            return View(model);
        }
    }
}

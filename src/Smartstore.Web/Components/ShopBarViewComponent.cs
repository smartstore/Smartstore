using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Identity;
using Smartstore.Core.Security;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components
{
    public class ShopBarViewComponent : SmartViewComponent
    {
        private readonly CustomerSettings _customerSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public ShopBarViewComponent(CustomerSettings customerSettings, CatalogSettings catalogSettings, ShoppingCartSettings shoppingCartSettings)
        {
            _customerSettings = customerSettings;
            _catalogSettings = catalogSettings;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public IViewComponentResult Invoke()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var isAdmin = customer.IsAdmin();
            var isRegistered = isAdmin || customer.IsRegistered();

            var model = new ShopBarModel
            {
                IsAuthenticated = isRegistered,
                CustomerEmailUsername = isRegistered ? (_customerSettings.CustomerLoginType != CustomerLoginType.Email ? customer.Username : customer.Email) : "",
                IsCustomerImpersonated = Services.WorkContext.CurrentImpersonator != null,
                DisplayAdminLink = Services.Permissions.Authorize(Permissions.System.AccessBackend),
                ShoppingCartEnabled = Services.Permissions.Authorize(Permissions.Cart.AccessShoppingCart) && _shoppingCartSettings.MiniShoppingCartEnabled,
                WishlistEnabled = Services.Permissions.Authorize(Permissions.Cart.AccessWishlist),
                CompareProductsEnabled = _catalogSettings.CompareProductsEnabled,
                PublicStoreNavigationAllowed = Services.Permissions.Authorize(Permissions.System.AccessShop)
            };

            return View(model);
        }
    }
}

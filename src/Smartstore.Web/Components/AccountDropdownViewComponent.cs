using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Security;
using Smartstore.Web.Models.Common;

namespace Smartstore.Web.Components
{
    public class AccountDropdownViewComponent : SmartViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            // TODO: (mh) (core) PMs must be prepared in Forum Module and links must be inserted into account_dropdown_after. 

            var model = new AccountDropdownModel
            {
                IsAuthenticated = customer.IsRegistered(),
                DisplayAdminLink = Services.Permissions.Authorize(Permissions.System.AccessBackend),
                ShoppingCartEnabled = Services.Permissions.Authorize(Permissions.Cart.AccessShoppingCart),
                WishlistEnabled = Services.Permissions.Authorize(Permissions.Cart.AccessWishlist),

                // TODO: (mh) (core) Why were these commented out?
                //ShoppingCartItems = customer.CountProductsInCart(ShoppingCartType.ShoppingCart, Services.StoreContext.CurrentStore.Id),
                //WishlistItems = customer.CountProductsInCart(ShoppingCartType.Wishlist, _serServicesvices.StoreContext.CurrentStore.Id)
            };

            return View(model);
        }
    }
}

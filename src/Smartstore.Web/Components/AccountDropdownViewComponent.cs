using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Security;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Rendering;
using Smartstore.Web.Rendering.Builders;

namespace Smartstore.Web.Components
{
    public class AccountDropdownViewComponent : SmartViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var customer = Services.WorkContext.CurrentCustomer;

            // TODO: (mh) (core) PMs must be prepared in Forum Module and links must be inserted into account_dropdown_after. 

            var model = new AccountDropdownModel
            {
                IsAuthenticated = customer.IsRegistered(),
                DisplayAdminLink = await Services.Permissions.AuthorizeAsync(Permissions.System.AccessBackend),
                ShoppingCartEnabled = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart),
                WishlistEnabled = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist),

                // TODO: (mh) (core) Why were these commented out? RE: for perf reasons.
                //ShoppingCartItems = customer.CountProductsInCart(ShoppingCartType.ShoppingCart, Services.StoreContext.CurrentStore.Id),
                //WishlistItems = customer.CountProductsInCart(ShoppingCartType.Wishlist, _serServicesvices.StoreContext.CurrentStore.Id)
            };

            model.MenuItems.Add(new MenuItem().ToBuilder()
                .Action("Info", "Customer")
                .LinkHtmlAttributes(new { @class = "dropdown-item", rel = "nofollow" })
                .Icon("fal fa-user fa-fw")
                .Text(T("Account.MyAccount"))
                .AsItem());

            if (model.DisplayAdminLink)
            {
                model.MenuItems.Add(new MenuItem().ToBuilder()
                    .Url("~/admin")
                    .LinkHtmlAttributes(new { @class = "dropdown-item", rel = "nofollow", target = "_admin" })
                    .Icon("fal fa-cog fa-fw")
                    .Text(T("Account.Administration"))
                    .AsItem());
            }

            if (model.WishlistEnabled)
            {
                model.MenuItems.Add(new MenuItem().ToBuilder()
                    .Route("Wishlist")
                    .LinkHtmlAttributes(new { @class = "dropdown-item" })
                    .Icon("fal fa-heart fa-fw")
                    .Text(T("Wishlist"))
                    .Badge(model.WishlistItems.ToString(), BadgeStyle.Success)
                    .BadgeHtmlAttributes("class", "wishlist-qty " + (model.WishlistItems > 0 ? "label-success" : "d-none"))
                    .AsItem());
            }

            if (model.ShoppingCartEnabled)
            {
                model.MenuItems.Add(new MenuItem().ToBuilder()
                    .Route("ShoppingCart")
                    .LinkHtmlAttributes(new { @class = "dropdown-item", id = "topcartlink" })
                    .Icon("fal fa-shopping-bag fa-fw")
                    .Text(T("ShoppingCart"))
                    .Badge(model.ShoppingCartItems.ToString(), BadgeStyle.Success)
                    .BadgeHtmlAttributes("class", "cart-qty " + (model.ShoppingCartItems > 0 ? "label-success" : "d-none"))
                    .AsItem());
            }

            model.MenuItems.Add(new MenuItem().ToBuilder().IsGroupHeader(true).AsItem());

            // TODO: (mh) (core) Working with asp-action & asp-controller might be better
            model.MenuItems.Add(new MenuItem().ToBuilder()
                .Route("Logout")
                .LinkHtmlAttributes(new { @class = "dropdown-item", rel = "nofollow" })
                .Icon("fal fa-sign-out-alt fa-fw")
                .Text(T("Account.Logout"))
                .AsItem());

            return View(model);
        }
    }
}

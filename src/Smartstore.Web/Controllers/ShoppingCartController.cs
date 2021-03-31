using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Security;
using Smartstore.Web.Models.ShoppingCart;

namespace Smartstore.Web.Controllers
{
    public class ShoppingCartController : PublicControllerBase
    {
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly OrderSettings _orderSettings;
        private readonly MediaSettings _mediaSettings;

        public ShoppingCartController(
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            OrderSettings orderSettings,
            MediaSettings mediaSettings)
        {
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
            _orderSettings = orderSettings;
            _mediaSettings = mediaSettings;
        }

        public async Task<IActionResult> OffCanvasShoppingCart()
        {
            if (!_shoppingCartSettings.MiniShoppingCartEnabled)
                return Content("");

            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
                return Content("");

            var model = await PrepareMiniShoppingCartModel();

            // TODO: (ms) (core) What about SafeSet
            //HttpContext.Session.Set(CheckoutState.CheckoutStateSessionKey, new CheckoutState());

            return PartialView(model);
        }

        [NonAction]
        protected async Task<MiniShoppingCartModel> PrepareMiniShoppingCartModel()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;

            var model = new MiniShoppingCartModel
            {
                ShowProductImages = _shoppingCartSettings.ShowProductImagesInMiniShoppingCart,
                ThumbSize = _mediaSettings.MiniCartThumbPictureSize,
                CurrentCustomerIsGuest = customer.IsGuest(),
                AnonymousCheckoutAllowed = _orderSettings.AnonymousCheckoutAllowed,
                DisplayMoveToWishlistButton = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist),
                ShowBasePrice = _shoppingCartSettings.ShowBasePrice
            };

            // TODO: (ms) (core) Finish the job.

            return model;
        }


        public IActionResult CartSummary()
        {
            // Stop annoying MiniProfiler report.
            return new EmptyResult();
        }
    }
}

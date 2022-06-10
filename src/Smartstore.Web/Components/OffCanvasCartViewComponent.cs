using Smartstore.Core.Catalog;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Security;
using Smartstore.Web.Models.Cart;

namespace Smartstore.Web.Components
{
    public class OffCanvasCartViewComponent : SmartViewComponent
    {
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;

        public OffCanvasCartViewComponent(
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings)
        {
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = new OffCanvasCartModel();

            if (await Services.Permissions.AuthorizeAsync(Permissions.System.AccessShop))
            {
                model.ShoppingCartEnabled = _shoppingCartSettings.MiniShoppingCartEnabled && await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart);
                model.WishlistEnabled = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist);
                model.CompareProductsEnabled = _catalogSettings.CompareProductsEnabled;
            }

            return View(model);
        }
    }
}

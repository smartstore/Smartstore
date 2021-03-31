using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.Catalog;

namespace Smartstore.Web.Components
{
    /// <summary>
    /// Component to render cross sell products.
    /// </summary>
    public class CrossSellProductsViewComponent : SmartViewComponent
    {
        private readonly IShoppingCartService _cartService;
        private readonly IProductService _productService;
        private readonly CatalogHelper _helper;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CrossSellProductsViewComponent(
            IShoppingCartService cartService, 
            IProductService productService,
            CatalogHelper helper,
            ShoppingCartSettings shoppingCartSettings)
        {
            _cartService = cartService;
            _productService = productService;
            _helper = helper;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Get customer shopping cart items
            var cart = await _cartService.GetCartItemsAsync(Services.WorkContext.CurrentCustomer, ShoppingCartType.ShoppingCart, Services.StoreContext.CurrentStore.Id);

            // Get cross-sell products by cart
            var products = await _productService.GetCrossSellProductsByShoppingCartAsync(cart, _shoppingCartSettings.CrossSellsNumber);

            // TODO: (mh) (core) Authorization can also be done in service method as it is only used in this viewcomponent?
            // ACL and store mapping
            //products = products.Where(p => _aclService.Authorize(p) && _storeMappingService.Authorize(p)).ToList();

            if (products.Any())
            {
                // Cross-sell products are displayed on the shopping cart page.
                // We know that the entire shopping cart page is not refreshed
                // even if "ShoppingCartSettings.DisplayCartAfterAddingProduct" setting  is enabled.
                // That's why we force page refresh (redirect) in this case
                var settings = _helper.GetBestFitProductSummaryMappingSettings(ProductSummaryViewMode.Grid, x =>
                {
                    x.ForceRedirectionAfterAddingToCart = true;
                });

                var model = await _helper.MapProductSummaryModelAsync(products, settings);

                return View(model);
            }

            return View(ProductSummaryModel.Empty);
        }
    }
}

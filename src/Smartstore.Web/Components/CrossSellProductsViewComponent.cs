
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
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
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly CatalogHelper _helper;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public CrossSellProductsViewComponent(
            IShoppingCartService cartService,
            IProductService productService,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            CatalogHelper helper,
            ShoppingCartSettings shoppingCartSettings)
        {
            _cartService = cartService;
            _productService = productService;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _helper = helper;
            _shoppingCartSettings = shoppingCartSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Get customer shopping cart.
            var cart = await _cartService.GetCartAsync(Services.WorkContext.CurrentCustomer, ShoppingCartType.ShoppingCart, Services.StoreContext.CurrentStore.Id);
            var cartProductIds = cart.Items
                .Select(x => x.Item.ProductId)
                .Distinct()
                .ToArray();

            var products = await _productService.GetCrossSellProductsByProductIdsAsync(cartProductIds, Convert.ToInt32(_shoppingCartSettings.CrossSellsNumber * 1.5));

            // ACL and store mapping
            products = await products
                .WhereAwait(async c => (await _aclService.AuthorizeAsync(c)) && (await _storeMappingService.AuthorizeAsync(c)))
                .Take(_shoppingCartSettings.CrossSellsNumber)
                .AsyncToList();

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

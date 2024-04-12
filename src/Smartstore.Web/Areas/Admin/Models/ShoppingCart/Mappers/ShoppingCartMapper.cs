using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Stores;

namespace Smartstore.Admin.Models.Cart
{
    internal static partial class ShoppingCartMappingExtensions
    {
        public static async Task<List<ShoppingCartItemModel>> MapAsync(this ShoppingCart cart)
        {
            Guard.NotNull(cart, nameof(cart));

            var models = new List<ShoppingCartItemModel>();
            await MapperFactory.MapAsync(cart, models);

            return models;
        }
    }

    internal class ShoppingCartMapper : Mapper<ShoppingCart, List<ShoppingCartItemModel>>
    {
        private readonly ICommonServices _services;
        private readonly IProductService _productService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IUrlHelper _urlHelper;

        public ShoppingCartMapper(
            ICommonServices services,
            IProductService productService,
            IPriceCalculationService priceCalculationService,
            IUrlHelper urlHelper)
        {
            _services = services;
            _productService = productService;
            _priceCalculationService = priceCalculationService;
            _urlHelper = urlHelper;
        }

        protected override void Map(ShoppingCart from, List<ShoppingCartItemModel> to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(ShoppingCart from, List<ShoppingCartItemModel> to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            var allProducts = from.GetAllProducts();
            var stores = _services.StoreContext.GetAllStores().ToDictionary(x => x.Id);

            foreach (var cartItem in from.Items)
            {
                var item = cartItem.Item;
                var store = stores.Get(item.StoreId);
                var batchContext = _productService.CreateProductBatchContext(allProducts, store, from.Customer, false);
                var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, from.Customer, null, batchContext);
                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(cartItem, calculationOptions);

                var (unitPrice, itemSubtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                var model = new ShoppingCartItemModel
                {
                    Id = item.Id,
                    Active = cartItem.Active,
                    Store = store?.Name ?? StringExtensions.NotAvailable,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    ProductName = item.Product.Name,
                    ProductTypeName = item.Product.GetProductTypeLabel(_services.Localization),
                    ProductTypeLabelHint = item.Product.ProductTypeLabelHint,
                    ProductEditUrl = _urlHelper.Action("Edit", "Product", new { id = item.ProductId, area = "Admin" }),
                    UpdatedOn = _services.DateTimeHelper.ConvertToUserTime(item.UpdatedOnUtc, DateTimeKind.Utc),
                    UnitPrice = unitPrice.FinalPrice,
                    Total = itemSubtotal.FinalPrice
                };

                to.Add(model);
            }
        }
    }
}

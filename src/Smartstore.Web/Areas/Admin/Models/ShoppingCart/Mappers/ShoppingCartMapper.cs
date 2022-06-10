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
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var products = from.Items
                .Select(x => x.Item.Product)
                .Union(from.Items.Select(x => x.ChildItems).SelectMany(child => child.Select(x => x.Item.Product)))
                .ToArray();

            var stores = _services.StoreContext.GetAllStores().ToDictionary(x => x.Id);

            foreach (var item in from.Items)
            {
                var sci = item.Item;
                var store = stores.Get(sci.StoreId);
                var batchContext = _productService.CreateProductBatchContext(products, store, from.Customer, false);
                var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, from.Customer, null, batchContext);
                var calculationContext = await _priceCalculationService.CreateCalculationContextAsync(item, calculationOptions);

                var (unitPrice, itemSubtotal) = await _priceCalculationService.CalculateSubtotalAsync(calculationContext);

                var model = new ShoppingCartItemModel
                {
                    Id = sci.Id,
                    Store = store?.Name ?? StringExtensions.NotAvailable,
                    ProductId = sci.ProductId,
                    Quantity = sci.Quantity,
                    ProductName = sci.Product.Name,
                    ProductTypeName = sci.Product.GetProductTypeLabel(_services.Localization),
                    ProductTypeLabelHint = sci.Product.ProductTypeLabelHint,
                    ProductEditUrl = _urlHelper.Action("Edit", "Product", new { id = sci.ProductId, area = "Admin" }),
                    UpdatedOn = _services.DateTimeHelper.ConvertToUserTime(sci.UpdatedOnUtc, DateTimeKind.Utc),
                    UnitPrice = unitPrice.FinalPrice,
                    Total = itemSubtotal.FinalPrice
                };

                to.Add(model);
            }
        }
    }
}

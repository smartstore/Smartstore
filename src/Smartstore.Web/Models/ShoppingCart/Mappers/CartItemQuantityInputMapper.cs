using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Models.Catalog.Mappers;

namespace Smartstore.Web.Models.Cart
{
    public static partial class CartItemQuantityInputMappingExtensions
    {
        public static Task MapQuantityInputAsync(this OrganizedShoppingCartItem item, IQuantityInput to)
        {
            Guard.NotNull(item);
            Guard.NotNull(to);

            var mapper = MapperFactory.GetMapper<OrganizedShoppingCartItem, IQuantityInput>();
            return mapper.MapAsync(item, to);
        }
    }

    public class CartItemQuantityInputMapper : QuantityInputMapperBase<OrganizedShoppingCartItem, IQuantityInput>
    {
        private readonly SmartDbContext _db;
        private readonly CatalogSettings _catalogSettings;

        public CartItemQuantityInputMapper(
            SmartDbContext db, 
            CatalogSettings catalogSettings,
            ShoppingCartSettings shoppingCartSettings)
            : base(shoppingCartSettings)
        {
            _db = db;
            _catalogSettings = catalogSettings;
        }

        protected override async Task MapCoreAsync(OrganizedShoppingCartItem source, IQuantityInput model, dynamic parameters = null)
        {
            var item = source.Item;
            var product = item.Product;

            model.EnteredQuantity = item.Quantity;
            model.MinOrderAmount = product.OrderMinimumQuantity;
            model.MaxOrderAmount = product.OrderMaximumQuantity;
            model.QuantityStep = product.QuantityStep;

            if (!product.IsAvailableByStock())
            {
                model.MaxInStock = product.StockQuantity;
            }

            MapCustomQuantities(model, product.ParseAllowedQuantities());

            var quantityUnit = await _db.QuantityUnits.GetQuantityUnitByIdAsync(product.QuantityUnitId ?? 0, _catalogSettings.ShowDefaultQuantityUnit);
            if (quantityUnit != null)
            {
                model.QuantityUnitName = quantityUnit.GetLocalized(x => x.Name);
                model.QuantityUnitNamePlural = quantityUnit.GetLocalized(x => x.NamePlural);
            }
        }
    }
}

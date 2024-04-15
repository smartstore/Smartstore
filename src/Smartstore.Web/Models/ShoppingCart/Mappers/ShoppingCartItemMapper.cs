using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Services;

namespace Smartstore.Web.Models.Cart
{
    public static partial class ShoppingCartMappingExtensions
    {
        public static async Task MapAsync(this OrganizedShoppingCartItem entity, ShoppingCartModel.ShoppingCartItemModel model, dynamic parameters = null)
        {
            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    public class ShoppingCartItemMapper : CartItemMapperBase<ShoppingCartModel.ShoppingCartItemModel>
    {
        public ShoppingCartItemMapper(
            ICommonServices services,
            IPriceCalculationService priceCalculationService,
            IDeliveryTimeService deliveryTimeService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            CatalogHelper catalogHelper)
            : base(services, 
                  priceCalculationService, 
                  deliveryTimeService,
                  productAttributeMaterializer, 
                  shoppingCartSettings, 
                  catalogSettings, 
                  catalogHelper)
        {
        }

        protected override void Map(OrganizedShoppingCartItem from, ShoppingCartModel.ShoppingCartItemModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(OrganizedShoppingCartItem from, ShoppingCartModel.ShoppingCartItemModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            var item = from.Item;
            var product = item.Product;

            await base.MapAsync(from, to, (object)parameters);

            to.Active = from.Active;
            to.IsShippingEnabled = product.IsShippingEnabled;
            to.IsDownload = product.IsDownload;
            to.IsEsd = product.IsEsd;
            to.HasUserAgreement = product.HasUserAgreement;
            to.DisableWishlistButton = product.DisableWishlistButton;

            if (from.ChildItems != null)
            {
                foreach (var childItem in from.ChildItems.Where(x => x.Item.Id != item.Id))
                {
                    var model = new ShoppingCartModel.ShoppingCartItemModel();

                    await childItem.MapAsync(model, (object)parameters);

                    // Inherit state from parent because only the parent item can be enabled/disabled.
                    model.Active = from.Active;

                    to.AddChildItems(model);
                }
            }
        }
    }
}

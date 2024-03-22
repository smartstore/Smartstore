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
        public static async Task MapAsync(this OrganizedShoppingCartItem entity, WishlistModel.WishlistItemModel model, dynamic parameters = null)
        {
            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    public class WishlistItemModelMapper : CartItemMapperBase<WishlistModel.WishlistItemModel>
    {
        public WishlistItemModelMapper(
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

        protected override void Map(OrganizedShoppingCartItem from, WishlistModel.WishlistItemModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(OrganizedShoppingCartItem from, WishlistModel.WishlistItemModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            await base.MapAsync(from, to, (object)parameters);

            if (from.ChildItems != null)
            {
                foreach (var childItem in from.ChildItems.Where(x => x.Item.Id != from.Item.Id))
                {
                    var model = new WishlistModel.WishlistItemModel
                    {
                        DisableBuyButton = childItem.Item.Product.DisableBuyButton
                    };

                    await childItem.MapAsync(model, (object)parameters);

                    to.AddChildItems(model);
                }
            }
        }
    }
}
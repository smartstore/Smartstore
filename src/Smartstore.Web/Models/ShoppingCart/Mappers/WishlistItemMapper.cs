using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Web.Models.ShoppingCart
{
    public static partial class WishlistMappingExtensions
    {
        public static async Task MapAsync(this OrganizedShoppingCartItem entity, WishlistModel.WishlistItemModel model)
        {
            await MapperFactory.MapAsync(entity, model, null);
        }
    }

    public class WishlistItemModelMapper : CartItemMapperBase<WishlistModel.WishlistItemModel>
    {
        public WishlistItemModelMapper(
            SmartDbContext db,
            ICommonServices services,
            ITaxService taxService,
            ICurrencyService currencyService,
            IPriceCalculationService priceCalculationService,
            IProductAttributeFormatter productAttributeFormatter,
            IProductAttributeMaterializer productAttributeMaterializer,
            IShoppingCartValidator shoppingCartValidator,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            ProductUrlHelper productUrlHelper,
            Localizer t)
            : base(db, services, taxService, currencyService, priceCalculationService, productAttributeFormatter, productAttributeMaterializer, 
                  shoppingCartValidator, shoppingCartSettings, catalogSettings, mediaSettings, productUrlHelper, t)
        {
        }

        protected override void Map(OrganizedShoppingCartItem from, WishlistModel.WishlistItemModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(OrganizedShoppingCartItem from, WishlistModel.WishlistItemModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            await base.MapAsync(from, to);

            if (from.ChildItems != null)
            {
                foreach (var childItem in from.ChildItems.Where(x => x.Item.Id != from.Item.Id))
                {
                    var model = new WishlistModel.WishlistItemModel
                    {
                        DisableBuyButton = childItem.Item.Product.DisableBuyButton
                    };

                    await childItem.MapAsync(model);

                    to.AddChildItems(model);
                }
            }
        }
    }
}
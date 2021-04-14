using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Security;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Web.Models.ShoppingCart
{
    public static class WishlistMappingExtensions
    {
        public static async Task MapAsync(this IList<OrganizedShoppingCartItem> entity, WishlistModel model, bool isEditable = true)
        {
            dynamic parameters = new ExpandoObject();
            parameters.IsEditable = isEditable;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    public class WishlistModelMapper : CartMapperBase<WishlistModel>
    {
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IProductAttributeFormatter _productAttributeFormatter;

        public WishlistModelMapper(
            ICommonServices services,
            IShoppingCartValidator shoppingCartValidator,
            IProductAttributeFormatter productAttributeFormatter,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings)
            : base(services, shoppingCartSettings, catalogSettings, mediaSettings)
        {
            _shoppingCartValidator = shoppingCartValidator;
            _productAttributeFormatter = productAttributeFormatter;
        }

        protected override void Map(List<OrganizedShoppingCartItem> from, WishlistModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(List<OrganizedShoppingCartItem> from, WishlistModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));

            var model = new WishlistModel
            {
                IsEditable = parameters?.IsEditable == true,
                EmailWishlistEnabled = _shoppingCartSettings.EmailWishlistEnabled,
                DisplayAddToCart = await _services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart)
            };

            if (from.Count == 0)
                return;

            var customer = from.FirstOrDefault().Item.Customer;
            model.CustomerGuid = customer.CustomerGuid;
            model.CustomerFullname = customer.GetFullName();
            model.ShowItemsFromWishlistToCartButton = _shoppingCartSettings.ShowItemsFromWishlistToCartButton;

            await base.MapAsync(from, to, null);

            // Cart warnings
            var warnings = new List<string>();
            var cartIsValid = await _shoppingCartValidator.ValidateCartItemsAsync(from, warnings);
            if (!cartIsValid)
            {
                model.Warnings.AddRange(warnings);
            }

            foreach (var item in from)
            {
                // TODO: (ms) (core) Implement WishlistItemModelMapper
                // model.AddItems(await PrepareWishlistItemModelAsync(item));
            }

            model.Items.Each(async x =>
            {
                // Do not display QuantityUnitName in OffCanvasWishlist
                x.QuantityUnitName = null;

                var item = from.Where(c => c.Item.Id == x.Id).FirstOrDefault();

                if (item != null)
                {
                    x.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
                        item.Item.AttributeSelection,
                        item.Item.Product,
                        null,
                        htmlEncode: false,
                        separator: ", ",
                        includePrices: false,
                        includeGiftCardAttributes: false,
                        includeHyperlinks: false);
                }
            });
        }
    }
}

using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Web.Models.ShoppingCart
{
    public static partial class WishlistMappingExtensions
    {
        public static async Task MapAsync(this IEnumerable<OrganizedShoppingCartItem> entity,
            WishlistModel model,
            bool isEditable = true,
            bool isOffcanvas = false)
        {
            dynamic parameters = new ExpandoObject();
            parameters.IsEditable = isEditable;
            parameters.IsOffcanvas = isOffcanvas;

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
            MediaSettings mediaSettings,
            Localizer T)
            : base(services, shoppingCartSettings, catalogSettings, mediaSettings, T)
        {
            _shoppingCartValidator = shoppingCartValidator;
            _productAttributeFormatter = productAttributeFormatter;
        }

        protected override void Map(IEnumerable<OrganizedShoppingCartItem> from, WishlistModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(IEnumerable<OrganizedShoppingCartItem> from, WishlistModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            if (!from.Any())
            {
                return;
            }

            await base.MapAsync(from, to, null);

            to.IsEditable = parameters?.IsEditable == true;
            to.EmailWishlistEnabled = _shoppingCartSettings.EmailWishlistEnabled;
            to.DisplayAddToCart = await _services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart);

            var customer = from.FirstOrDefault().Item.Customer;
            to.CustomerGuid = customer.CustomerGuid;
            to.CustomerFullname = customer.GetFullName();
            to.ShowItemsFromWishlistToCartButton = _shoppingCartSettings.ShowItemsFromWishlistToCartButton;

            // Cart warnings
            var warnings = new List<string>();
            var cartIsValid = await _shoppingCartValidator.ValidateCartItemsAsync(from, warnings);
            if (!cartIsValid)
            {
                to.Warnings.AddRange(warnings);
            }

            foreach (var cartItem in from)
            {
                var model = new WishlistModel.WishlistItemModel
                {
                    DisableBuyButton = cartItem.Item.Product.DisableBuyButton,
                };

                await cartItem.MapAsync(model);

                if (parameters?.IsOffcanvas == true)
                {
                    model.QuantityUnitName = null;

                    var item = from.Where(c => c.Item.Id == model.Id).FirstOrDefault();

                    if (item != null)
                    {
                        model.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
                            item.Item.AttributeSelection,
                            item.Item.Product,
                            null,
                            htmlEncode: false,
                            separator: ", ",
                            includePrices: false,
                            includeGiftCardAttributes: false,
                            includeHyperlinks: false);
                    }
                }

                to.AddItems(model);
            }
        }
    }
}

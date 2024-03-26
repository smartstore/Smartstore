using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;

namespace Smartstore.Web.Models.Cart
{
    public static partial class ShoppingCartMappingExtensions
    {
        public static async Task MapAsync(this ShoppingCart cart,
            WishlistModel model,
            bool isEditable = true,
            bool isOffcanvas = false)
        {
            dynamic parameters = new GracefulDynamicObject();
            parameters.IsEditable = isEditable;
            parameters.IsOffcanvas = isOffcanvas;

            await MapperFactory.MapAsync(cart, model, parameters);
        }
    }

    public class WishlistModelMapper : CartMapperBase<WishlistModel>
    {
        private readonly ITaxService _taxService;
        private readonly IProductService _productService;
        private readonly IShoppingCartValidator _shoppingCartValidator;

        public WishlistModelMapper(
            ICommonServices services,
            ITaxService taxService,
            IProductService productService,
            IShoppingCartValidator shoppingCartValidator,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            MeasureSettings measureSettings,
            Localizer T)
            : base(services, shoppingCartSettings, catalogSettings, mediaSettings, measureSettings, T)
        {
            _taxService = taxService;
            _productService = productService;
            _shoppingCartValidator = shoppingCartValidator;
        }

        protected override void Map(ShoppingCart from, WishlistModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(ShoppingCart from, WishlistModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            if (!from.HasItems)
            {
                return;
            }

            var isOffcanvas = parameters?.IsOffcanvas == true;

            await base.MapAsync(from, to, null);

            to.IsEditable = parameters?.IsEditable == true;
            to.EmailWishlistEnabled = _shoppingCartSettings.EmailWishlistEnabled;
            to.DisplayAddToCart = await _services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart);

            to.CustomerGuid = from.Customer.CustomerGuid;
            to.CustomerFullname = from.Customer.GetFullName();
            to.ShowItemsFromWishlistToCartButton = _shoppingCartSettings.ShowItemsFromWishlistToCartButton;

            // Cart warnings.
            var warnings = new List<string>();
            if (!await _shoppingCartValidator.ValidateCartAsync(from, warnings))
            {
                to.Warnings.AddRange(warnings);
            }

            var allProducts = from.Items
                .Select(x => x.Item.Product)
                .Union(from.Items.Select(x => x.ChildItems).SelectMany(child => child.Select(x => x.Item.Product)))
                .ToArray();

            var batchContext = _productService.CreateProductBatchContext(allProducts, null, from.Customer, false);

            dynamic itemParameters = new GracefulDynamicObject();
            itemParameters.TaxFormat = _taxService.GetTaxFormat();
            itemParameters.BatchContext = batchContext;
            itemParameters.ShowEssentialAttributes = !isOffcanvas || (isOffcanvas && _shoppingCartSettings.ShowEssentialAttributesInMiniShoppingCart);

            foreach (var cartItem in from.Items)
            {
                var model = new WishlistModel.WishlistItemModel
                {
                    DisableBuyButton = cartItem.Item.Product.DisableBuyButton,
                };

                await cartItem.MapAsync(model, (object)itemParameters);

                if (isOffcanvas)
                {
                    model.QuantityUnitName = null;
                }

                to.AddItems(model);
            }

            batchContext.Clear();
        }
    }
}

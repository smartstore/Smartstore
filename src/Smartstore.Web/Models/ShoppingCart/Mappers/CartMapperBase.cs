using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Cart
{
    public abstract class CartMapperBase<TModel> : Mapper<ShoppingCart, TModel>
       where TModel : CartModelBase
    {
        protected readonly ICommonServices _services;
        protected readonly ShoppingCartSettings _shoppingCartSettings;
        protected readonly CatalogSettings _catalogSettings;
        protected readonly MediaSettings _mediaSettings;
        protected readonly Localizer T;

        protected CartMapperBase(
            ICommonServices services,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            Localizer t)
        {
            _services = services;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
            _mediaSettings = mediaSettings;
            T = t;
        }

        public override Task MapAsync(ShoppingCart from, TModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            to.DisplayShortDesc = _shoppingCartSettings.ShowShortDesc;
            to.ShowProductImages = _shoppingCartSettings.ShowProductImagesOnShoppingCart;
            to.ShowProductBundleImages = _shoppingCartSettings.ShowProductBundleImagesOnShoppingCart;
            to.ShowSku = _catalogSettings.ShowProductSku;
            to.BundleThumbSize = _mediaSettings.CartThumbBundleItemPictureSize;

            return Task.CompletedTask;
        }
    }
}

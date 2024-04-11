using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Configuration;
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
        protected readonly MeasureSettings _measureSettings;
        protected readonly Localizer T;

        protected CartMapperBase(
            ICommonServices services,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            MeasureSettings measureSettings,
            Localizer t)
        {
            _services = services;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
            _mediaSettings = mediaSettings;
            _measureSettings = measureSettings;
            T = t;
        }

        public override async Task MapAsync(ShoppingCart from, TModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            to.AllowActivatableCartItems = _shoppingCartSettings.AllowActivatableCartItems;
            to.ShowProductImages = _shoppingCartSettings.ShowProductImagesOnShoppingCart;
            to.ShowProductBundleImages = _shoppingCartSettings.ShowProductBundleImagesOnShoppingCart;
            to.BundleThumbSize = _mediaSettings.CartThumbBundleItemPictureSize;
            to.ShoppingCartType = from.CartType;

            var measure = await _services.DbContext.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
            if (measure != null)
            {
                to.MeasureUnitName = measure.GetLocalized(x => x.Name);
            }
        }
    }
}

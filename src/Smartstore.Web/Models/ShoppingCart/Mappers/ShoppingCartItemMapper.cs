using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Localization;

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
        private readonly IDeliveryTimeService _deliveryTimeService;

        public ShoppingCartItemMapper(
            ICommonServices services,
            IDeliveryTimeService deliveryTimeService,
            IPriceCalculationService priceCalculationService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings)
            : base(services, priceCalculationService, productAttributeMaterializer, shoppingCartSettings, catalogSettings)
        {
            _deliveryTimeService = deliveryTimeService;
        }

        protected override void Map(OrganizedShoppingCartItem from, ShoppingCartModel.ShoppingCartItemModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(OrganizedShoppingCartItem from, ShoppingCartModel.ShoppingCartItemModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var item = from.Item;
            var product = item.Product;

            await base.MapAsync(from, to, (object)parameters);

            to.Weight = product.Weight;
            to.IsShipEnabled = product.IsShippingEnabled;
            to.IsDownload = product.IsDownload;
            to.HasUserAgreement = product.HasUserAgreement;
            to.IsEsd = product.IsEsd;
            to.DisableWishlistButton = product.DisableWishlistButton;

            if (product.DisplayDeliveryTimeAccordingToStock(_catalogSettings))
            {
                var deliveryTime = await _deliveryTimeService.GetDeliveryTimeAsync(product.GetDeliveryTimeIdAccordingToStock(_catalogSettings));
                if (deliveryTime != null)
                {
                    to.DeliveryTimeName = deliveryTime.GetLocalized(x => x.Name);
                    to.DeliveryTimeHexValue = deliveryTime.ColorHexValue;

                    if (_shoppingCartSettings.DeliveryTimesInShoppingCart is DeliveryTimesPresentation.DateOnly
                        or DeliveryTimesPresentation.LabelAndDate)
                    {
                        to.DeliveryTimeDate = _deliveryTimeService.GetFormattedDeliveryDate(deliveryTime);
                    }
                }
            }

            if (from.Item.BundleItem == null)
            {
                var selectedValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(item.AttributeSelection);
                selectedValues.Each(x => to.Weight += x.WeightAdjustment);
            }

            if (from.ChildItems != null)
            {
                foreach (var childItem in from.ChildItems.Where(x => x.Item.Id != item.Id))
                {
                    var model = new ShoppingCartModel.ShoppingCartItemModel();

                    await childItem.MapAsync(model, (object)parameters);

                    to.AddChildItems(model);
                }
            }
        }
    }
}

using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Web.Models.ShoppingCart
{
    public static partial class ShoppingCartMappingExtensions
    {
        public static async Task MapAsync(this OrganizedShoppingCartItem entity, ShoppingCartModel.ShoppingCartItemModel model)
        {
            await MapperFactory.MapAsync(entity, model, null);
        }
    }

    public class ShoppingCartItemMapper : CartItemMapperBase<ShoppingCartModel.ShoppingCartItemModel>
    {
        private readonly IDeliveryTimeService _deliveryTimeService;

        public ShoppingCartItemMapper(
            SmartDbContext db,
            ICommonServices services,
            ITaxService taxService,
            ICurrencyService currencyService,
            IPriceCalculationService priceCalculationService,
            IDeliveryTimeService deliveryTimeService,
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

            await base.MapAsync(from, to);

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

            var basePriceAdjustment = (await _priceCalculationService.GetFinalPriceAsync(product, null)
                - await _priceCalculationService.GetUnitPriceAsync(from, true)) * -1;

            to.BasePrice = await _priceCalculationService.GetBasePriceInfoAsync(product, item.Customer, _services.WorkContext.WorkingCurrency, basePriceAdjustment);

            if (from.Item.BundleItem == null)
            {
                var selectedAttributeValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(item.AttributeSelection);
                if (selectedAttributeValues != null)
                {
                    var weight = decimal.Zero;
                    foreach (var attributeValue in selectedAttributeValues)
                    {
                        weight += attributeValue.WeightAdjustment;
                    }

                    to.Weight += weight;
                }
            }

            if (from.ChildItems != null)
            {
                foreach (var childItem in from.ChildItems.Where(x => x.Item.Id != item.Id))
                {
                    var model = new ShoppingCartModel.ShoppingCartItemModel();

                    await childItem.MapAsync(model);

                    to.AddChildItems(model);
                }
            }
        }
    }
}

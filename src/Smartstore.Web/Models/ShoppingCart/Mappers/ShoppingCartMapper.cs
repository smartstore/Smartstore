using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Utilities.Html;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Rendering;
using Smartstore.Web.Rendering.Choices;

namespace Smartstore.Web.Models.Cart
{
    public static partial class ShoppingCartMappingExtensions
    {
        public static async Task<ShoppingCartModel> MapAsync(this ShoppingCart cart,
            bool isEditable = true,
            bool validateCheckoutAttributes = false,
            bool validateRequiredProducts = false,
            bool prepareEstimateShippingIfEnabled = true,
            bool setEstimateShippingDefaultAddress = true)
        {
            var model = new ShoppingCartModel();

            await cart.MapAsync(model,
                isEditable,
                validateCheckoutAttributes,
                validateRequiredProducts,
                prepareEstimateShippingIfEnabled,
                setEstimateShippingDefaultAddress);

            return model;
        }

        public static async Task MapAsync(this ShoppingCart cart,
            ShoppingCartModel model,
            bool isEditable = true,
            bool validateCheckoutAttributes = false,
            bool validateRequiredProducts = false,
            bool prepareEstimateShippingIfEnabled = true,
            bool setEstimateShippingDefaultAddress = true)
        {
            dynamic parameters = new GracefulDynamicObject();
            parameters.IsEditable = isEditable;
            parameters.ValidateCheckoutAttributes = validateCheckoutAttributes;
            parameters.ValidateRequiredProducts = validateRequiredProducts;
            parameters.PrepareEstimateShippingIfEnabled = prepareEstimateShippingIfEnabled;
            parameters.SetEstimateShippingDefaultAddress = setEstimateShippingDefaultAddress;

            await MapperFactory.MapAsync(cart, model, parameters);
        }
    }

    public class ShoppingCartModelMapper : CartMapperBase<ShoppingCartModel>
    {
        private readonly SmartDbContext _db;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IProductService _productService;
        private readonly IPaymentService _paymentService;
        private readonly IDiscountService _discountService;
        private readonly ICurrencyService _currencyService;
        private readonly ITaxService _taxService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly ShippingSettings _shippingSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;

        public ShoppingCartModelMapper(
            SmartDbContext db,
            ICommonServices services,
            ITaxCalculator taxCalculator,
            IProductService productService,
            IPaymentService paymentService,
            IDiscountService discountService,
            ICurrencyService currencyService,
            ITaxService taxService,
            IShoppingCartValidator shoppingCartValidator,
            IOrderCalculationService orderCalculationService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            MeasureSettings measureSettings,
            ShippingSettings shippingSettings,
            RewardPointsSettings rewardPointsSettings,
            Localizer T)
            : base(services, shoppingCartSettings, catalogSettings, mediaSettings, measureSettings, T)
        {
            _db = db;
            _taxCalculator = taxCalculator;
            _productService = productService;
            _paymentService = paymentService;
            _discountService = discountService;
            _currencyService = currencyService;
            _taxService = taxService;
            _shoppingCartValidator = shoppingCartValidator;
            _orderCalculationService = orderCalculationService;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _shippingSettings = shippingSettings;
            _rewardPointsSettings = rewardPointsSettings;
        }

        protected override void Map(ShoppingCart from, ShoppingCartModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(ShoppingCart from, ShoppingCartModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            if (!from.HasItems)
            {
                return;
            }

            await base.MapAsync(from, to, null);

            var store = _services.StoreContext.CurrentStore;
            var customer = _services.WorkContext.CurrentCustomer;
            var currency = _services.WorkContext.WorkingCurrency;

            var isEditable = parameters?.IsEditable == true;
            var isBillingAddresRequired = from.Requirements.HasFlag(CheckoutRequirements.BillingAddress);
            var isPaymentRequired = from.Requirements.HasFlag(CheckoutRequirements.Payment);
            var validateCheckoutAttributes = parameters?.ValidateCheckoutAttributes == true;
            var validateRequiredProducts = parameters?.ValidateRequiredProducts == true;
            var prepareEstimateShippingIfEnabled = parameters?.PrepareEstimateShippingIfEnabled == true;
            var setEstimateShippingDefaultAddress = parameters?.SetEstimateShippingDefaultAddress == true;

            #region Simple properties

            to.IsEditable = isEditable;
            to.MediaDimensions = _mediaSettings.CartThumbPictureSize;
            to.DisplayBasePrice = _shoppingCartSettings.ShowBasePrice;
            to.DisplayMoveToWishlistButton = await _services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist);
            to.DisplayCommentBox = _shoppingCartSettings.ShowCommentBox;
            to.DisplayEsdRevocationWaiverBox = _shoppingCartSettings.ShowEsdRevocationWaiverBox;
            to.ShowProductImages = _shoppingCartSettings.ShowProductImagesOnShoppingCart;
            to.ShowProductBundleImages = _shoppingCartSettings.ShowProductBundleImagesOnShoppingCart;
            to.HasItemsWithRecurringPayment = from.ContainsRecurringItem();

            to.CheckoutAttributeInfo = HtmlUtility.ConvertPlainTextToTable(HtmlUtility.ConvertHtmlToPlainText(
                await _checkoutAttributeFormatter.FormatAttributesAsync(customer.GenericAttributes.CheckoutAttributes, customer)));

            // Gift card and gift card boxes.
            to.DiscountBox.Display = _shoppingCartSettings.ShowDiscountBox;
            var discountCouponCode = customer.GenericAttributes.DiscountCouponCode;
            var discount = await _db.Discounts
                .AsNoTracking()
                .Where(x => x.CouponCode == discountCouponCode)
                .FirstOrDefaultAsync();

            if (discount != null
                && discount.RequiresCouponCode
                && await _discountService.IsDiscountValidAsync(discount, customer))
            {
                to.DiscountBox.CurrentCode = discount.CouponCode;
            }

            to.GiftCardBox.Display = _shoppingCartSettings.ShowGiftCardBox;

            // Reward points.
            if (_rewardPointsSettings.Enabled && !from.IncludesMatchingItems(x => x.IsRecurring) && !customer.IsGuest())
            {
                var rewardPointsBalance = customer.GetRewardPointsBalance();
                var rewardPointsAmountBase = _orderCalculationService.ConvertRewardPointsToAmount(rewardPointsBalance);
                var rewardPointsAmount = _currencyService.ConvertFromPrimaryCurrency(rewardPointsAmountBase.Amount, currency);

                if (rewardPointsAmount > decimal.Zero)
                {
                    to.RewardPoints.DisplayRewardPoints = true;
                    to.RewardPoints.RewardPointsAmount = rewardPointsAmount.ToString(true);
                    to.RewardPoints.RewardPointsBalance = rewardPointsBalance;
                    to.RewardPoints.UseRewardPoints = customer.GenericAttributes.UseRewardPointsDuringCheckout;
                }
            }

            // Cart warnings.
            await _shoppingCartValidator.ValidateCartAsync(from, to.Warnings, validateCheckoutAttributes, validateRequiredProducts);

            to.CheckoutNotAllowedWarning = T(_shoppingCartSettings.AllowActivatableCartItems ? "ShoppingCart.SelectAtLeastOneProduct" : "ShoppingCart.CartIsEmpty");

            #endregion

            #region Checkout attributes

            var checkoutAttributes = await _checkoutAttributeMaterializer.GetCheckoutAttributesAsync(from, store.Id);

            foreach (var attribute in checkoutAttributes)
            {
                var caModel = new ShoppingCartModel.CheckoutAttributeModel
                {
                    Id = attribute.Id,
                    Name = attribute.GetLocalized(x => x.Name),
                    TextPrompt = attribute.GetLocalized(x => x.TextPrompt),
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlType
                };

                if (attribute.IsListTypeAttribute)
                {
                    var taxFormat = _taxService.GetTaxFormat(null, null, PricingTarget.Product);
                    var caValues = await _db.CheckoutAttributeValues
                        .Include(x => x.MediaFile)
                        .AsNoTracking()
                        .Where(x => x.CheckoutAttributeId == attribute.Id)
                        .OrderBy(x => x.DisplayOrder)
                        .ToListAsync();

                    // Prepare each attribute with image and price
                    var valuesModels = await caValues
                        .SelectAwait(async x =>
                        {
                            var m = new ShoppingCartModel.CheckoutAttributeValueModel
                            {
                                Id = x.Id,
                                Name = x.GetLocalized(x => x.Name),
                                IsPreSelected = x.IsPreSelected,
                                Color = x.Color,
                                DisplayOrder = x.DisplayOrder
                            };

                            if (x.MediaFileId.HasValue && x.MediaFile != null)
                            {
                                m.ImageUrl = _services.MediaService.GetUrl(x.MediaFile, _mediaSettings.VariantValueThumbPictureSize, null, false);
                            }

                            // Display price if allowed.
                            if (await _services.Permissions.AuthorizeAsync(Permissions.Catalog.DisplayPrice))
                            {
                                var priceAdjustmentBase = await _taxCalculator.CalculateCheckoutAttributeTaxAsync(x);
                                var priceAdjustment = _currencyService.ConvertFromPrimaryCurrency(priceAdjustmentBase.Price, currency);

                                if (priceAdjustment > 0)
                                {
                                    m.PriceAdjustment = "+" + priceAdjustment.WithPostFormat(taxFormat).ToString();
                                }
                                else if (priceAdjustment < 0)
                                {
                                    m.PriceAdjustment = "-" + (priceAdjustment * -1).WithPostFormat(taxFormat).ToString();
                                }
                            }

                            return m;
                        })
                        .ToListAsync();

                    caModel.Values = [.. valuesModels
                        .Select(x => (ChoiceItemModel)x)
                        .OrderBy(x => x.DisplayOrder)
                        .ThenNaturalBy(x => x.Name)];
                }

                // Set already selected attributes.
                var selectedCheckoutAttributes = customer.GenericAttributes.CheckoutAttributes;
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.Boxes:
                    case AttributeControlType.Checkboxes:
                        if (selectedCheckoutAttributes.AttributesMap.Any())
                        {
                            // Clear default selection.
                            foreach (var item in caModel.Values)
                            {
                                item.IsPreSelected = false;
                            }

                            // Select new values.
                            var selectedCaValues = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributeValuesAsync(selectedCheckoutAttributes);
                            foreach (var caValue in selectedCaValues)
                            {
                                foreach (var item in caModel.Values)
                                {
                                    if (caValue.Id == item.Id)
                                    {
                                        item.IsPreSelected = true;
                                    }
                                }
                            }
                        }
                        break;

                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        if (selectedCheckoutAttributes.AttributesMap.Any())
                        {
                            var enteredText = selectedCheckoutAttributes.GetAttributeValues(attribute.Id)?
                                .Select(x => x.ToString())
                                .FirstOrDefault();

                            if (enteredText.HasValue())
                            {
                                caModel.TextValue = enteredText;
                            }
                        }
                        break;

                    case AttributeControlType.Datepicker:
                    {
                        var enteredDate = selectedCheckoutAttributes.AttributesMap
                            .Where(x => x.Key == attribute.Id)
                            .SelectMany(x => x.Value)
                            .FirstOrDefault()?
                            .ToString();

                        caModel.SelectedDate = enteredDate?.ToDateTime(null);
                    }
                    break;

                    case AttributeControlType.FileUpload:
                        if (selectedCheckoutAttributes.AttributesMap.Any())
                        {
                            var fileValue = selectedCheckoutAttributes.AttributesMap
                                .Where(x => x.Key == attribute.Id)
                                .Select(x => x.Value.ToString())
                                .FirstOrDefault();

                            if (fileValue.HasValue() && caModel.UploadedFileGuid.HasValue() && Guid.TryParse(caModel.UploadedFileGuid, out var guid))
                            {
                                var download = await _db.Downloads
                                    .Include(x => x.MediaFile)
                                    .FirstOrDefaultAsync(x => x.DownloadGuid == guid);

                                if (download != null && !download.UseDownloadUrl && download.MediaFile != null)
                                {
                                    caModel.UploadedFileName = download.MediaFile.Name;
                                }
                            }
                        }
                        break;

                    default:
                        break;
                }

                to.CheckoutAttributes.Add(caModel);
            }

            #endregion

            #region Estimate shipping

            if (prepareEstimateShippingIfEnabled)
            {
                to.EstimateShipping.Enabled = _shippingSettings.EstimateShippingEnabled && from.HasItems && from.IsShippingRequired;

                if (to.EstimateShipping.Enabled)
                {
                    // Countries and state provinces.
                    var countriesForShipping = await _db.Countries
                        .AsNoTracking()
                        .Where(x => x.AllowsShipping)
                        .ApplyStandardFilter(false, store.Id)
                        .ToListAsync();

                    var defaultCountryId = (setEstimateShippingDefaultAddress && customer.ShippingAddress != null
                        ? customer.ShippingAddress.CountryId
                        : to.EstimateShipping.CountryId) ?? countriesForShipping.FirstOrDefault()?.Id;

                    to.EstimateShipping.AvailableCountries = countriesForShipping.ToSelectListItems(defaultCountryId ?? 0);

                    var stateProvinces = await _db.StateProvinces.GetStateProvincesByCountryIdAsync(defaultCountryId ?? 0);

                    var defaultStateProvinceId = setEstimateShippingDefaultAddress && customer.ShippingAddress != null
                        ? customer.ShippingAddress.StateProvinceId
                        : to.EstimateShipping.StateProvinceId;

                    to.EstimateShipping.AvailableStates = stateProvinces.ToSelectListItems(defaultStateProvinceId ?? 0) ?? new List<SelectListItem>
                    {
                        new() { Text = T("Address.OtherNonUS"), Value = "0" }
                    };

                    if (setEstimateShippingDefaultAddress && customer.ShippingAddress != null)
                    {
                        to.EstimateShipping.ZipPostalCode = customer.ShippingAddress.ZipPostalCode;
                    }
                }
            }

            #endregion

            #region Cart items

            var batchContext = _productService.CreateProductBatchContext(from.GetAllProducts(), null, customer, false);
            var subtotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(from, null, batchContext);

            dynamic itemParameters = new GracefulDynamicObject();
            //itemParameters.TaxFormat = parameters?.IsOffcanvas == true ? _taxService.GetTaxFormat() : null;
            itemParameters.TaxFormat = _taxService.GetTaxFormat();
            itemParameters.BatchContext = batchContext;
            itemParameters.CartSubtotal = subtotal;
            itemParameters.Cart = from;
            itemParameters.CachedBrands = new Dictionary<int, BrandOverviewModel>();

            foreach (var cartItem in from.Items)
            {
                var model = new ShoppingCartModel.ShoppingCartItemModel();
                await cartItem.MapAsync(model, (object)itemParameters);
                to.AddItems(model);
            }

            #endregion

            var paymentMethods = await _paymentService.LoadActivePaymentProvidersAsync(
                from,
                store.Id,
                [PaymentMethodType.Button, PaymentMethodType.StandardAndButton],
                false);

            if (from.ContainsRecurringItem())
            {
                paymentMethods = paymentMethods.Where(x => x.Value.RecurringPaymentType > RecurringPaymentType.NotSupported);
            }

            foreach (var paymentMethod in paymentMethods)
            {
                var widget = paymentMethod.Value.GetPaymentInfoWidget();
                to.ButtonPaymentMethods.Items.Add(widget);
            }

            batchContext.Clear();
        }
    }
}

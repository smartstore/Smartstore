using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Utilities.Html;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Web.Models.ShoppingCart
{   
    public static class ShoppingCartMappingExtensions
    {
        public static async Task MapAsync(this IList<OrganizedShoppingCartItem> entity,
            ShoppingCartModel model,
            bool isEditable = true,
            bool validateCheckoutAttributes = false,
            bool prepareEstimateShippingIfEnabled = true,
            bool setEstimateShippingDefaultAddress = true,
            bool prepareAndDisplayOrderReviewData = false)
        {
            dynamic parameters = new ExpandoObject();
            parameters.IsEditable = isEditable;
            parameters.ValidateCheckoutAttribute = validateCheckoutAttributes;
            parameters.PrepareEstimateShippingIfEnabled = prepareEstimateShippingIfEnabled;
            parameters.SetEstimateShippingDefaultAddress = setEstimateShippingDefaultAddress;
            parameters.PrepareAndDisplayOrderReviewData = prepareAndDisplayOrderReviewData;

            await MapperFactory.MapAsync(entity, model, parameters);
        }
    }

    public class ShoppingCartModelMapper : CartMapperBase<ShoppingCartModel>
    {
        private readonly SmartDbContext _db;
        private readonly ITaxService _taxService;
        private readonly IMediaService _mediaService;
        private readonly IPaymentService _paymentService;
        private readonly IDiscountService _discountService;
        private readonly ICurrencyService _currencyService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly OrderSettings _orderSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;

        public ShoppingCartModelMapper(
            SmartDbContext db,
            ITaxService taxService,
            ICommonServices services,
            IMediaService mediaService,
            IPaymentService paymentService,
            IDiscountService discountService,
            ICurrencyService currencyService,
            IShoppingCartValidator shoppingCartValidator,
            IOrderCalculationService orderCalculationService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MediaSettings mediaSettings,
            OrderSettings orderSettings,
            MeasureSettings measureSettings,
            ShippingSettings shippingSettings,
            RewardPointsSettings rewardPointsSettings)
            : base(services, shoppingCartSettings, catalogSettings, mediaSettings)
        {
            _db = db;
            _taxService = taxService;
            _mediaService = mediaService;
            _paymentService = paymentService;
            _discountService = discountService;
            _currencyService = currencyService;
            _shoppingCartValidator = shoppingCartValidator;
            _orderCalculationService = orderCalculationService;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _shippingSettings = shippingSettings;
            _orderSettings = orderSettings;
            _measureSettings = measureSettings;
            _rewardPointsSettings = rewardPointsSettings;
        }

        protected override void Map(List<OrganizedShoppingCartItem> from, ShoppingCartModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(List<OrganizedShoppingCartItem> from, ShoppingCartModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            if (from.Count == 0)
            {
                return;
            }

            var store = _services.StoreContext.CurrentStore;
            var customer = _services.WorkContext.CurrentCustomer;
            var currency = _services.WorkContext.WorkingCurrency;

            var isEditable = parameters?.IsEditable == true;
            var validateCheckoutAttributes = parameters?.ValidateCheckoutAttributes == true;
            var prepareEstimateShippingIfEnabled = parameters?.PrepareEstimateShippingIfEnabled == true;
            var setEstimateShippingDefaultAddress = parameters?.SetEstimateShippingDefaultAddress == true;
            var prepareAndDisplayOrderReviewData = parameters?.PrepareAndDisplayOrderReviewData == true;

            #region Simple properties

            var model = new ShoppingCartModel
            {
                MediaDimensions = _mediaSettings.CartThumbPictureSize,
                DeliveryTimesPresentation = _shoppingCartSettings.DeliveryTimesInShoppingCart,
                DisplayBasePrice = _shoppingCartSettings.ShowBasePrice,
                DisplayWeight = _shoppingCartSettings.ShowWeight,
                DisplayMoveToWishlistButton = await _services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist),
                TermsOfServiceEnabled = _orderSettings.TermsOfServiceEnabled,
                DisplayCommentBox = _shoppingCartSettings.ShowCommentBox,
                DisplayEsdRevocationWaiverBox = _shoppingCartSettings.ShowEsdRevocationWaiverBox,
                IsEditable = isEditable
            };

            await base.MapAsync(from, to, null);

            var measure = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
            if (measure != null)
            {
                model.MeasureUnitName = measure.GetLocalized(x => x.Name);
            }

            model.CheckoutAttributeInfo = HtmlUtils.ConvertPlainTextToTable(
                HtmlUtils.ConvertHtmlToPlainText(
                    await _checkoutAttributeFormatter.FormatAttributesAsync(customer.GenericAttributes.CheckoutAttributes, customer)));

            // Gift card and gift card boxes.
            model.DiscountBox.Display = _shoppingCartSettings.ShowDiscountBox;
            var discountCouponCode = customer.GenericAttributes.DiscountCouponCode;
            var discount = await _db.Discounts
                .AsNoTracking()
                .Where(x => x.CouponCode == discountCouponCode)
                .FirstOrDefaultAsync();

            if (discount != null
                && discount.RequiresCouponCode
                && await _discountService.IsDiscountValidAsync(discount, customer))
            {
                model.DiscountBox.CurrentCode = discount.CouponCode;
            }

            model.GiftCardBox.Display = _shoppingCartSettings.ShowGiftCardBox;

            // Reward points.
            if (_rewardPointsSettings.Enabled && !from.IncludesMatchingItems(x => x.IsRecurring) && !customer.IsGuest())
            {
                var rewardPointsBalance = customer.GetRewardPointsBalance();
                var rewardPointsAmountBase = _orderCalculationService.ConvertRewardPointsToAmount(rewardPointsBalance);
                var rewardPointsAmount = _currencyService.ConvertFromPrimaryCurrency(rewardPointsAmountBase.Amount, currency);

                if (rewardPointsAmount > decimal.Zero)
                {
                    model.RewardPoints.DisplayRewardPoints = true;
                    model.RewardPoints.RewardPointsAmount = rewardPointsAmount.ToString(true);
                    model.RewardPoints.RewardPointsBalance = rewardPointsBalance;
                    model.RewardPoints.UseRewardPoints = customer.GenericAttributes.UseRewardPointsDuringCheckout;
                }
            }

            // Cart warnings.
            var warnings = new List<string>();
            var cartIsValid = await _shoppingCartValidator.ValidateCartItemsAsync(from, warnings, validateCheckoutAttributes, customer.GenericAttributes.CheckoutAttributes);
            if (!cartIsValid)
            {
                model.Warnings.AddRange(warnings);
            }

            #endregion

            #region Checkout attributes

            var checkoutAttributes = await _checkoutAttributeMaterializer.GetValidCheckoutAttributesAsync(from);

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
                    var caValues = await _db.CheckoutAttributeValues
                        .AsNoTracking()
                        .Where(x => x.CheckoutAttributeId == attribute.Id)
                        .ToListAsync();

                    // Prepare each attribute with image and price
                    foreach (var caValue in caValues)
                    {
                        var pvaValueModel = new ShoppingCartModel.CheckoutAttributeValueModel
                        {
                            Id = caValue.Id,
                            Name = caValue.GetLocalized(x => x.Name),
                            IsPreSelected = caValue.IsPreSelected,
                            Color = caValue.Color
                        };

                        if (caValue.MediaFileId.HasValue && caValue.MediaFile != null)
                        {
                            pvaValueModel.ImageUrl = _mediaService.GetUrl(caValue.MediaFile, _mediaSettings.VariantValueThumbPictureSize, null, false);
                        }

                        caModel.Values.Add(pvaValueModel);

                        // Display price if allowed.
                        if (await _services.Permissions.AuthorizeAsync(Permissions.Catalog.DisplayPrice))
                        {
                            var priceAdjustmentBase = await _taxService.GetCheckoutAttributePriceAsync(caValue);
                            var priceAdjustment = _currencyService.ConvertFromPrimaryCurrency(priceAdjustmentBase.Price.Amount, currency);

                            if (priceAdjustmentBase.Price > decimal.Zero)
                            {
                                pvaValueModel.PriceAdjustment = "+" + priceAdjustmentBase.Price.ToString();
                            }
                            else if (priceAdjustmentBase.Price < decimal.Zero)
                            {
                                pvaValueModel.PriceAdjustment = "-" + priceAdjustmentBase.Price.ToString();
                            }
                        }
                    }
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
                            var enteredText = selectedCheckoutAttributes.AttributesMap
                                .Where(x => x.Key == attribute.Id)
                                .SelectMany(x => x.Value)
                                .FirstOrDefault()
                                .ToString();

                            if (enteredText.HasValue())
                            {
                                caModel.TextValue = enteredText;
                            }
                        }
                        break;

                    case AttributeControlType.Datepicker:
                        {
                            // Keep in mind my that the code below works only in the current culture.
                            var enteredDate = selectedCheckoutAttributes.AttributesMap
                                .Where(x => x.Key == attribute.Id)
                                .SelectMany(x => x.Value)
                                .FirstOrDefault()
                                .ToString();

                            if (enteredDate.HasValue()
                                && DateTime.TryParseExact(enteredDate, "D", CultureInfo.CurrentCulture, DateTimeStyles.None, out var selectedDate))
                            {
                                caModel.SelectedDay = selectedDate.Day;
                                caModel.SelectedMonth = selectedDate.Month;
                                caModel.SelectedYear = selectedDate.Year;
                            }
                        }
                        break;

                    case AttributeControlType.FileUpload:
                        if (selectedCheckoutAttributes.AttributesMap.Any())
                        {
                            var FileValue = selectedCheckoutAttributes.AttributesMap
                                .Where(x => x.Key == attribute.Id)
                                .SelectMany(x => x.Value)
                                .FirstOrDefault()
                                .ToString();

                            if (FileValue.HasValue() && caModel.UploadedFileGuid.HasValue() && Guid.TryParse(caModel.UploadedFileGuid, out var guid))
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

                model.CheckoutAttributes.Add(caModel);
            }

            #endregion

            #region Estimate shipping

            if (prepareEstimateShippingIfEnabled)
            {
                model.EstimateShipping.Enabled = _shippingSettings.EstimateShippingEnabled
                    && from.Any()
                    && from.IncludesMatchingItems(x => x.IsShippingEnabled);

                if (model.EstimateShipping.Enabled)
                {
                    // Countries.
                    var defaultEstimateCountryId = setEstimateShippingDefaultAddress && customer.ShippingAddress != null
                        ? customer.ShippingAddress.CountryId
                        : model.EstimateShipping.CountryId;

                    var countriesForShipping = await _db.Countries
                        .AsNoTracking()
                        .ApplyStoreFilter(store.Id)
                        .Where(x => x.AllowsShipping)
                        .ToListAsync();

                    foreach (var countries in countriesForShipping)
                    {
                        model.EstimateShipping.AvailableCountries.Add(new SelectListItem
                        {
                            Text = countries.GetLocalized(x => x.Name),
                            Value = countries.Id.ToString(),
                            Selected = countries.Id == defaultEstimateCountryId
                        });
                    }

                    // States.
                    var states = defaultEstimateCountryId.HasValue
                        ? await _db.StateProvinces.AsNoTracking().ApplyCountryFilter(defaultEstimateCountryId.Value).ToListAsync()
                        : new();

                    if (states.Any())
                    {
                        var defaultEstimateStateId = setEstimateShippingDefaultAddress && customer.ShippingAddress != null
                            ? customer.ShippingAddress.StateProvinceId
                            : model.EstimateShipping.StateProvinceId;

                        foreach (var s in states)
                        {
                            model.EstimateShipping.AvailableStates.Add(new SelectListItem
                            {
                                Text = s.GetLocalized(x => x.Name),
                                Value = s.Id.ToString(),
                                Selected = s.Id == defaultEstimateStateId
                            });
                        }
                    }
                    else
                    {
                        model.EstimateShipping.AvailableStates.Add(new SelectListItem { Text = T("Address.OtherNonUS"), Value = "0" });
                    }

                    if (setEstimateShippingDefaultAddress && customer.ShippingAddress != null)
                    {
                        model.EstimateShipping.ZipPostalCode = customer.ShippingAddress.ZipPostalCode;
                    }
                }
            }

            #endregion

            #region Cart items

            foreach (var item in from)
            {
                // TODO: (ms) (core) Implement ShoppingCartItemMapper
                //model.AddItems(await PrepareShoppingCartItemModelAsync(item));
            }

            #endregion

            #region Order review data

            if (prepareAndDisplayOrderReviewData)
            {
                // TODO: (ms) (core) Access HttpContext.Session
                //HttpContext.Session.TryGetObject(CheckoutState.CheckoutStateSessionKey, out CheckoutState checkoutState);

                model.OrderReviewData.Display = true;

                // Billing info.
                // TODO: (mh)(core)Implement AddressModels PrepareModel()
                var billingAddress = customer.BillingAddress;
                if (billingAddress != null)
                {
                    // TODO: (ms) (core) Wait for AddressModels.PrepareModel() implementation
                    //model.OrderReviewData.BillingAddressPrepareModel(billingAddress, false, _addressSettings);
                }

                // Shipping info.
                if (from.IsShippingRequired())
                {
                    model.OrderReviewData.IsShippable = true;

                    // TODO: (mh) (core) Implement AddressModels PrepareModel()
                    var shippingAddress = customer.ShippingAddress;
                    if (shippingAddress != null)
                    {
                        // TODO: (ms)(core)Wait for AddressModels.PrepareModel() implementation
                        //model.OrderReviewData.ShippingAddress.PrepareModel(shippingAddress, false, _addressSettings);
                    }

                    // Selected shipping method.
                    var shippingOption = customer.GenericAttributes.SelectedShippingOption;
                    if (shippingOption != null)
                    {
                        model.OrderReviewData.ShippingMethod = shippingOption.Name;
                    }

                    // TODO: (ms) (core) Implement httpContext checkoutState
                    //if(checkoutState.CustomProperties.ContainsKey("HasOnlyOneActiveShippingMethod"))
                    ////if (_httpContext.Session.TryGetValue("HasOnlyOneActiveShippingMethod", out var _))
                    //{
                    //    model.OrderReviewData.DisplayShippingMethodChangeOption = !(bool)checkoutState.CustomProperties.Get("HasOnlyOneActiveShippingMethod");
                    //}
                }
            }

            //if (checkoutState.CustomProperties.ContainsKey("HasOnlyOneActivePaymentMethod"))
            //{
            //    model.OrderReviewData.DisplayPaymentMethodChangeOption = !(bool)checkoutState.CustomProperties.Get("HasOnlyOneActivePaymentMethod");
            //}

            var selectedPaymentMethodSystemName = customer.GenericAttributes.SelectedPaymentMethod;
            var paymentMethod = await _paymentService.LoadPaymentMethodBySystemNameAsync(selectedPaymentMethodSystemName);

            //// TODO: (ms) (core) Wait for PluginMediator.GetLocalizedFriendlyName implementation
            ////model.OrderReviewData.PaymentMethod = paymentMethod != null ? _pluginMediator.GetLocalizedFriendlyName(paymentMethod.Metadata) : "";
            //model.OrderReviewData.PaymentSummary = checkoutState.PaymentSummary;
            //model.OrderReviewData.IsPaymentSelectionSkipped = checkoutState.IsPaymentSelectionSkipped;

            #endregion

            var paymentTypes = new PaymentMethodType[] { PaymentMethodType.Button, PaymentMethodType.StandardAndButton };
            var boundPaymentMethods = await _paymentService.LoadActivePaymentMethodsAsync(
                _services.WorkContext.CurrentCustomer,
                from,
                _services.StoreContext.CurrentStore.Id,
                paymentTypes,
                false);

            var bpmModel = new ButtonPaymentMethodModel();

            foreach (var boundPaymentMethod in boundPaymentMethods)
            {
                if (from.IncludesMatchingItems(x => x.IsRecurring) && boundPaymentMethod.Value.RecurringPaymentType == RecurringPaymentType.NotSupported)
                    continue;

                var widgetInvoker = boundPaymentMethod.Value.GetPaymentInfoWidget();
                bpmModel.Items.Add(widgetInvoker);
            }
        }
    }
}

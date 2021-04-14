using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Localization.Routing;
using Smartstore.Core.Logging;
using Smartstore.Core.Messages;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Utilities.Html;
using Smartstore.Web.Components;
using Smartstore.Web.Filters;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Models.ShoppingCart;

namespace Smartstore.Web.Controllers
{
    public class ShoppingCartController : PublicControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IMessageFactory _messageFactory;
        private readonly ITaxService _taxService;
        private readonly IMediaService _mediaService;
        private readonly IActivityLogger _activityLogger;
        private readonly IPaymentService _paymentService;
        private readonly IShippingService _shippingService;
        private readonly ICurrencyService _currencyService;
        private readonly IDiscountService _discountService;
        private readonly IGiftCardService _giftCardService;
        private readonly IDownloadService _downloadService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILocalizationService _localizationService;
        private readonly IDeliveryTimeService _deliveryTimeService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ICheckoutAttributeMaterializer _checkoutAttributeMaterializer;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly MeasureSettings _measureSettings;
        private readonly CaptchaSettings _captchaSettings;
        private readonly OrderSettings _orderSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;

        public ShoppingCartController(
            SmartDbContext db,
            IMessageFactory messageFactory,
            ITaxService taxService,
            IMediaService mediaService,
            IActivityLogger activityLogger,
            IPaymentService paymentService,
            IShippingService shippingService,
            ICurrencyService currencyService,
            IDiscountService discountService,
            IGiftCardService giftCardService,
            IDownloadService downloadService,
            IShoppingCartService shoppingCartService,
            ILocalizationService localizationService,
            IDeliveryTimeService deliveryTimeService,
            IPriceCalculationService priceCalculationService,
            IOrderCalculationService orderCalculationService,
            IShoppingCartValidator shoppingCartValidator,
            IProductAttributeFormatter productAttributeFormatter,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            IProductAttributeMaterializer productAttributeMaterializer,
            ICheckoutAttributeMaterializer checkoutAttributeMaterializer,
            ProductUrlHelper productUrlHelper,
            ShoppingCartSettings shoppingCartSettings,
            CatalogSettings catalogSettings,
            MeasureSettings measureSettings,
            CaptchaSettings captchaSettings,
            OrderSettings orderSettings,
            MediaSettings mediaSettings,
            ShippingSettings shippingSettings,
            CustomerSettings customerSettings,
            RewardPointsSettings rewardPointsSettings)
        {
            _db = db;
            _messageFactory = messageFactory;
            _taxService = taxService;
            _mediaService = mediaService;
            _activityLogger = activityLogger;
            _paymentService = paymentService;
            _shippingService = shippingService;
            _currencyService = currencyService;
            _discountService = discountService;
            _giftCardService = giftCardService;
            _downloadService = downloadService;
            _shoppingCartService = shoppingCartService;
            _localizationService = localizationService;
            _deliveryTimeService = deliveryTimeService;
            _priceCalculationService = priceCalculationService;
            _orderCalculationService = orderCalculationService;
            _shoppingCartValidator = shoppingCartValidator;
            _productAttributeFormatter = productAttributeFormatter;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _productAttributeMaterializer = productAttributeMaterializer;
            _checkoutAttributeMaterializer = checkoutAttributeMaterializer;
            _productUrlHelper = productUrlHelper;
            _shoppingCartSettings = shoppingCartSettings;
            _catalogSettings = catalogSettings;
            _measureSettings = measureSettings;
            _captchaSettings = captchaSettings;
            _orderSettings = orderSettings;
            _mediaSettings = mediaSettings;
            _shippingSettings = shippingSettings;
            _customerSettings = customerSettings;
            _rewardPointsSettings = rewardPointsSettings;
        }

        #region Utilities

        //// TODO: (ms) (core) Move this method to ShoppingCartValidator service
        //[NonAction]
        //protected async Task<bool> ValidateAndSaveCartDataAsync(ProductVariantQuery query, List<string> warnings, bool useRewardPoints = false)
        //{
        //    Guard.NotNull(query, nameof(query));

        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

        //    await ParseAndSaveCheckoutAttributesAsync(cart, query);

        //    // Validate checkout attributes.
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var checkoutAttributes = customer.GenericAttributes.CheckoutAttributes;
        //    var isValid = await _shoppingCartValidator.ValidateCartItemsAsync(cart, warnings, true, checkoutAttributes);
        //    if (isValid)
        //    {
        //        // Reward points.
        //        if (_rewardPointsSettings.Enabled)
        //        {
        //            customer.GenericAttributes.UseRewardPointsDuringCheckout = useRewardPoints;
        //            await customer.GenericAttributes.SaveChangesAsync();
        //        }
        //    }

        //    return isValid;
        //}

        [NonAction]
        protected async Task PrepareButtonPaymentMethodModelAsync(ButtonPaymentMethodModel model, IList<OrganizedShoppingCartItem> cart)
        {
            Guard.NotNull(model, nameof(model));
            Guard.NotNull(cart, nameof(cart));

            model.Items.Clear();

            var paymentTypes = new PaymentMethodType[] { PaymentMethodType.Button, PaymentMethodType.StandardAndButton };

            var boundPaymentMethods = await _paymentService.LoadActivePaymentMethodsAsync(
                Services.WorkContext.CurrentCustomer,
                cart,
                Services.StoreContext.CurrentStore.Id,
                paymentTypes,
                false);

            foreach (var paymentMethod in boundPaymentMethods)
            {
                if (cart.IncludesMatchingItems(x => x.IsRecurring) && paymentMethod.Value.RecurringPaymentType == RecurringPaymentType.NotSupported)
                    continue;

                var widgetInvoker = paymentMethod.Value.GetPaymentInfoWidget();
                model.Items.Add(widgetInvoker);
            }
        }

        [NonAction]
        protected async Task ParseAndSaveCheckoutAttributesAsync(List<OrganizedShoppingCartItem> cart, ProductVariantQuery query)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(query, nameof(query));

            var selectedAttributes = new CheckoutAttributeSelection(string.Empty);
            var customer = cart.GetCustomer() ?? Services.WorkContext.CurrentCustomer;

            var checkoutAttributes = await _checkoutAttributeMaterializer.GetValidCheckoutAttributesAsync(cart);

            foreach (var attribute in checkoutAttributes)
            {
                var selectedItems = query.CheckoutAttributes.Where(x => x.AttributeId == attribute.Id);

                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.Boxes:
                        {
                            var selectedValue = selectedItems.FirstOrDefault()?.Value;
                            if (selectedValue.HasValue())
                            {
                                var selectedAttributeValueId = selectedValue.SplitSafe(",").FirstOrDefault()?.ToInt();
                                if (selectedAttributeValueId.GetValueOrDefault() > 0)
                                {
                                    selectedAttributes.AddAttributeValue(attribute.Id, selectedAttributeValueId.Value);
                                }
                            }
                        }
                        break;

                    case AttributeControlType.Checkboxes:
                        {
                            foreach (var item in selectedItems)
                            {
                                var selectedValue = item.Value.SplitSafe(",").FirstOrDefault()?.ToInt();
                                if (selectedValue.GetValueOrDefault() > 0)
                                {
                                    selectedAttributes.AddAttributeValue(attribute.Id, selectedValue);
                                }
                            }
                        }
                        break;

                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            var selectedValue = string.Join(",", selectedItems.Select(x => x.Value));
                            if (selectedValue.HasValue())
                            {
                                selectedAttributes.AddAttributeValue(attribute.Id, selectedValue);
                            }
                        }
                        break;

                    case AttributeControlType.Datepicker:
                        {
                            var selectedValue = selectedItems.FirstOrDefault()?.Date;
                            if (selectedValue.HasValue)
                            {
                                selectedAttributes.AddAttributeValue(attribute.Id, selectedValue.Value);
                            }
                        }
                        break;

                    case AttributeControlType.FileUpload:
                        {
                            var selectedValue = string.Join(",", selectedItems.Select(x => x.Value));
                            if (selectedValue.HasValue())
                            {
                                selectedAttributes.AddAttributeValue(attribute.Id, selectedValue);
                            }
                        }
                        break;
                }
            }

            customer.GenericAttributes.CheckoutAttributes = selectedAttributes;
            _db.TryUpdate(customer);
            await _db.SaveChangesAsync();
        }

        //// TODO: (ms) (core) Add methods dev documentations
        //[NonAction]
        //protected async Task<WishlistModel> PrepareWishlistModelAsync(IList<OrganizedShoppingCartItem> cart, bool isEditable = true)
        //{
        //    Guard.NotNull(cart, nameof(cart));

        //    var model = new WishlistModel
        //    {
        //        IsEditable = isEditable,
        //        EmailWishlistEnabled = _shoppingCartSettings.EmailWishlistEnabled,
        //        DisplayAddToCart = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart)
        //    };

        //    if (cart.Count == 0)
        //        return model;

        //    var customer = cart.FirstOrDefault().Item.Customer;
        //    model.CustomerGuid = customer.CustomerGuid;
        //    model.CustomerFullname = customer.GetFullName();
        //    model.ShowItemsFromWishlistToCartButton = _shoppingCartSettings.ShowItemsFromWishlistToCartButton;

        //    PrepareCartModelBase(model);

        //    // Cart warnings
        //    var warnings = new List<string>();
        //    var cartIsValid = await _shoppingCartValidator.ValidateCartItemsAsync(cart, warnings);
        //    if (!cartIsValid)
        //    {
        //        model.Warnings.AddRange(warnings);
        //    }

        //    foreach (var item in cart)
        //    {
        //        model.AddItems(await PrepareWishlistItemModelAsync(item));
        //    }

        //    model.Items.Each(async x =>
        //    {
        //        // Do not display QuantityUnitName in OffCanvasWishlist
        //        x.QuantityUnitName = null;

        //        var item = cart.Where(c => c.Item.Id == x.Id).FirstOrDefault();

        //        if (item != null)
        //        {
        //            x.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
        //                item.Item.AttributeSelection,
        //                item.Item.Product,
        //                null,
        //                htmlEncode: false,
        //                separator: ", ",
        //                includePrices: false,
        //                includeGiftCardAttributes: false,
        //                includeHyperlinks: false);
        //        }
        //    });

        //    return model;
        //}

        ///// <summary>
        ///// Prepares shopping cart model.
        ///// </summary>
        ///// <param name="model">Model instance.</param>
        ///// <param name="cart">Shopping cart items.</param>
        ///// <param name="isEditable">A value indicating whether the cart is editable.</param>
        ///// <param name="validateCheckoutAttributes">A value indicating whether checkout attributes get validated.</param>
        ///// <param name="prepareEstimateShippingIfEnabled">A value indicating whether to prepare "Estimate shipping" model.</param>
        ///// <param name="setEstimateShippingDefaultAddress">A value indicating whether to prefill "Estimate shipping" model with the default customer address.</param>
        ///// <param name="prepareAndDisplayOrderReviewData">A value indicating whether to prepare review data (such as billing/shipping address, payment or shipping data entered during checkout).</param>
        //[NonAction]
        //public async Task<ShoppingCartModel> PrepareShoppingCartModelAsync(
        //    IList<OrganizedShoppingCartItem> cart,
        //    bool isEditable = true,
        //    bool validateCheckoutAttributes = false,
        //    bool prepareEstimateShippingIfEnabled = true,
        //    bool setEstimateShippingDefaultAddress = true,
        //    bool prepareAndDisplayOrderReviewData = false)
        //{
        //    Guard.NotNull(cart, nameof(cart));

        //    if (cart.Count == 0)
        //    {
        //        return new();
        //    }

        //    var store = Services.StoreContext.CurrentStore;
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var currency = Services.WorkContext.WorkingCurrency;

        //    #region Simple properties

        //    var model = new ShoppingCartModel
        //    {
        //        MediaDimensions = _mediaSettings.CartThumbPictureSize,
        //        DeliveryTimesPresentation = _shoppingCartSettings.DeliveryTimesInShoppingCart,
        //        DisplayBasePrice = _shoppingCartSettings.ShowBasePrice,
        //        DisplayWeight = _shoppingCartSettings.ShowWeight,
        //        DisplayMoveToWishlistButton = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist),
        //        TermsOfServiceEnabled = _orderSettings.TermsOfServiceEnabled,
        //        DisplayCommentBox = _shoppingCartSettings.ShowCommentBox,
        //        DisplayEsdRevocationWaiverBox = _shoppingCartSettings.ShowEsdRevocationWaiverBox,
        //        IsEditable = isEditable
        //    };

        //    PrepareCartModelBase(model);

        //    var measure = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
        //    if (measure != null)
        //    {
        //        model.MeasureUnitName = measure.GetLocalized(x => x.Name);
        //    }

        //    model.CheckoutAttributeInfo = HtmlUtils.ConvertPlainTextToTable(
        //        HtmlUtils.ConvertHtmlToPlainText(
        //            await _checkoutAttributeFormatter.FormatAttributesAsync(customer.GenericAttributes.CheckoutAttributes, customer))
        //        );

        //    // Gift card and gift card boxes.
        //    model.DiscountBox.Display = _shoppingCartSettings.ShowDiscountBox;
        //    var discountCouponCode = customer.GenericAttributes.DiscountCouponCode;
        //    var discount = await _db.Discounts
        //        .AsNoTracking()
        //        .Where(x => x.CouponCode == discountCouponCode)
        //        .FirstOrDefaultAsync();

        //    if (discount != null
        //        && discount.RequiresCouponCode
        //        && await _discountService.IsDiscountValidAsync(discount, customer))
        //    {
        //        model.DiscountBox.CurrentCode = discount.CouponCode;
        //    }

        //    model.GiftCardBox.Display = _shoppingCartSettings.ShowGiftCardBox;

        //    // Reward points.
        //    if (_rewardPointsSettings.Enabled && !cart.IncludesMatchingItems(x => x.IsRecurring) && !customer.IsGuest())
        //    {
        //        var rewardPointsBalance = customer.GetRewardPointsBalance();
        //        var rewardPointsAmountBase = _orderCalculationService.ConvertRewardPointsToAmount(rewardPointsBalance);
        //        var rewardPointsAmount = _currencyService.ConvertFromPrimaryCurrency(rewardPointsAmountBase.Amount, currency);

        //        if (rewardPointsAmount > decimal.Zero)
        //        {
        //            model.RewardPoints.DisplayRewardPoints = true;
        //            model.RewardPoints.RewardPointsAmount = rewardPointsAmount.ToString(true);
        //            model.RewardPoints.RewardPointsBalance = rewardPointsBalance;
        //            model.RewardPoints.UseRewardPoints = customer.GenericAttributes.UseRewardPointsDuringCheckout;
        //        }
        //    }

        //    // Cart warnings.
        //    var warnings = new List<string>();
        //    var cartIsValid = await _shoppingCartValidator.ValidateCartItemsAsync(cart, warnings, validateCheckoutAttributes, customer.GenericAttributes.CheckoutAttributes);
        //    if (!cartIsValid)
        //    {
        //        model.Warnings.AddRange(warnings);
        //    }

        //    #endregion

        //    #region Checkout attributes

        //    var checkoutAttributes = await _checkoutAttributeMaterializer.GetValidCheckoutAttributesAsync(cart);

        //    foreach (var attribute in checkoutAttributes)
        //    {
        //        var caModel = new ShoppingCartModel.CheckoutAttributeModel
        //        {
        //            Id = attribute.Id,
        //            Name = attribute.GetLocalized(x => x.Name),
        //            TextPrompt = attribute.GetLocalized(x => x.TextPrompt),
        //            IsRequired = attribute.IsRequired,
        //            AttributeControlType = attribute.AttributeControlType
        //        };

        //        if (attribute.IsListTypeAttribute)
        //        {
        //            var caValues = await _db.CheckoutAttributeValues
        //                .AsNoTracking()
        //                .Where(x => x.CheckoutAttributeId == attribute.Id)
        //                .ToListAsync();

        //            // Prepare each attribute with image and price
        //            foreach (var caValue in caValues)
        //            {
        //                var pvaValueModel = new ShoppingCartModel.CheckoutAttributeValueModel
        //                {
        //                    Id = caValue.Id,
        //                    Name = caValue.GetLocalized(x => x.Name),
        //                    IsPreSelected = caValue.IsPreSelected,
        //                    Color = caValue.Color
        //                };

        //                if (caValue.MediaFileId.HasValue && caValue.MediaFile != null)
        //                {
        //                    pvaValueModel.ImageUrl = _mediaService.GetUrl(caValue.MediaFile, _mediaSettings.VariantValueThumbPictureSize, null, false);
        //                }

        //                caModel.Values.Add(pvaValueModel);

        //                // Display price if allowed.
        //                if (await Services.Permissions.AuthorizeAsync(Permissions.Catalog.DisplayPrice))
        //                {
        //                    var priceAdjustmentBase = await _taxService.GetCheckoutAttributePriceAsync(caValue);
        //                    var priceAdjustment = _currencyService.ConvertFromPrimaryCurrency(priceAdjustmentBase.Price.Amount, currency);

        //                    if (priceAdjustmentBase.Price > decimal.Zero)
        //                    {
        //                        pvaValueModel.PriceAdjustment = "+" + priceAdjustmentBase.Price.ToString();
        //                    }
        //                    else if (priceAdjustmentBase.Price < decimal.Zero)
        //                    {
        //                        pvaValueModel.PriceAdjustment = "-" + priceAdjustmentBase.Price.ToString();
        //                    }
        //                }
        //            }
        //        }

        //        // Set already selected attributes.
        //        var selectedCheckoutAttributes = customer.GenericAttributes.CheckoutAttributes;
        //        switch (attribute.AttributeControlType)
        //        {
        //            case AttributeControlType.DropdownList:
        //            case AttributeControlType.RadioList:
        //            case AttributeControlType.Boxes:
        //            case AttributeControlType.Checkboxes:
        //                if (selectedCheckoutAttributes.AttributesMap.Any())
        //                {
        //                    // Clear default selection.
        //                    foreach (var item in caModel.Values)
        //                    {
        //                        item.IsPreSelected = false;
        //                    }

        //                    // Select new values.
        //                    var selectedCaValues = await _checkoutAttributeMaterializer.MaterializeCheckoutAttributeValuesAsync(selectedCheckoutAttributes);
        //                    foreach (var caValue in selectedCaValues)
        //                    {
        //                        foreach (var item in caModel.Values)
        //                        {
        //                            if (caValue.Id == item.Id)
        //                            {
        //                                item.IsPreSelected = true;
        //                            }
        //                        }
        //                    }
        //                }
        //                break;

        //            case AttributeControlType.TextBox:
        //            case AttributeControlType.MultilineTextbox:
        //                if (selectedCheckoutAttributes.AttributesMap.Any())
        //                {
        //                    var enteredText = selectedCheckoutAttributes.AttributesMap
        //                        .Where(x => x.Key == attribute.Id)
        //                        .SelectMany(x => x.Value)
        //                        .FirstOrDefault()
        //                        .ToString();

        //                    if (enteredText.HasValue())
        //                    {
        //                        caModel.TextValue = enteredText;
        //                    }
        //                }
        //                break;

        //            case AttributeControlType.Datepicker:
        //                {
        //                    // Keep in mind my that the code below works only in the current culture.
        //                    var enteredDate = selectedCheckoutAttributes.AttributesMap
        //                        .Where(x => x.Key == attribute.Id)
        //                        .SelectMany(x => x.Value)
        //                        .FirstOrDefault()
        //                        .ToString();

        //                    if (enteredDate.HasValue()
        //                        && DateTime.TryParseExact(enteredDate, "D", CultureInfo.CurrentCulture, DateTimeStyles.None, out var selectedDate))
        //                    {
        //                        caModel.SelectedDay = selectedDate.Day;
        //                        caModel.SelectedMonth = selectedDate.Month;
        //                        caModel.SelectedYear = selectedDate.Year;
        //                    }
        //                }
        //                break;

        //            case AttributeControlType.FileUpload:
        //                if (selectedCheckoutAttributes.AttributesMap.Any())
        //                {
        //                    var FileValue = selectedCheckoutAttributes.AttributesMap
        //                        .Where(x => x.Key == attribute.Id)
        //                        .SelectMany(x => x.Value)
        //                        .FirstOrDefault()
        //                        .ToString();

        //                    if (FileValue.HasValue() && caModel.UploadedFileGuid.HasValue() && Guid.TryParse(caModel.UploadedFileGuid, out var guid))
        //                    {
        //                        var download = await _db.Downloads
        //                            .Include(x => x.MediaFile)
        //                            .FirstOrDefaultAsync(x => x.DownloadGuid == guid);

        //                        if (download != null && !download.UseDownloadUrl && download.MediaFile != null)
        //                        {
        //                            caModel.UploadedFileName = download.MediaFile.Name;
        //                        }
        //                    }
        //                }
        //                break;

        //            default:
        //                break;
        //        }

        //        model.CheckoutAttributes.Add(caModel);
        //    }

        //    #endregion

        //    #region Estimate shipping

        //    if (prepareEstimateShippingIfEnabled)
        //    {
        //        model.EstimateShipping.Enabled = _shippingSettings.EstimateShippingEnabled && cart.Any() && cart.IncludesMatchingItems(x => x.IsShippingEnabled);
        //        if (model.EstimateShipping.Enabled)
        //        {
        //            // Countries.
        //            var defaultEstimateCountryId = setEstimateShippingDefaultAddress && customer.ShippingAddress != null
        //                ? customer.ShippingAddress.CountryId
        //                : model.EstimateShipping.CountryId;

        //            var countriesForShipping = await _db.Countries
        //                .AsNoTracking()
        //                .ApplyStoreFilter(store.Id)
        //                .Where(x => x.AllowsShipping)
        //                .ToListAsync();

        //            foreach (var countries in countriesForShipping)
        //            {
        //                model.EstimateShipping.AvailableCountries.Add(new SelectListItem
        //                {
        //                    Text = countries.GetLocalized(x => x.Name),
        //                    Value = countries.Id.ToString(),
        //                    Selected = countries.Id == defaultEstimateCountryId
        //                });
        //            }

        //            // States.
        //            var states = defaultEstimateCountryId.HasValue
        //                ? await _db.StateProvinces.AsNoTracking().ApplyCountryFilter(defaultEstimateCountryId.Value).ToListAsync()
        //                : new();

        //            if (states.Any())
        //            {
        //                var defaultEstimateStateId = setEstimateShippingDefaultAddress && customer.ShippingAddress != null
        //                    ? customer.ShippingAddress.StateProvinceId
        //                    : model.EstimateShipping.StateProvinceId;

        //                foreach (var s in states)
        //                {
        //                    model.EstimateShipping.AvailableStates.Add(new SelectListItem
        //                    {
        //                        Text = s.GetLocalized(x => x.Name),
        //                        Value = s.Id.ToString(),
        //                        Selected = s.Id == defaultEstimateStateId
        //                    });
        //                }
        //            }
        //            else
        //            {
        //                model.EstimateShipping.AvailableStates.Add(new SelectListItem { Text = T("Address.OtherNonUS"), Value = "0" });
        //            }

        //            if (setEstimateShippingDefaultAddress && customer.ShippingAddress != null)
        //            {
        //                model.EstimateShipping.ZipPostalCode = customer.ShippingAddress.ZipPostalCode;
        //            }
        //        }
        //    }

        //    #endregion

        //    #region Cart items

        //    foreach (var item in cart)
        //    {
        //        model.AddItems(await PrepareShoppingCartItemModelAsync(item));
        //    }

        //    #endregion

        //    #region Order review data

        //    if (prepareAndDisplayOrderReviewData)
        //    {
        //        HttpContext.Session.TryGetObject(CheckoutState.CheckoutStateSessionKey, out CheckoutState checkoutState);

        //        model.OrderReviewData.Display = true;

        //        // Billing info.
        //        // TODO: (mh)(core)Implement AddressModels PrepareModel()
        //        //var billingAddress = customer.BillingAddress;
        //        //if (billingAddress != null)
        //        //{
        //        //    model.OrderReviewData.BillingAddress.PrepareModel(billingAddress, false, _addressSettings);
        //        //}

        //        // Shipping info.
        //        if (cart.IsShippingRequired())
        //        {
        //            model.OrderReviewData.IsShippable = true;

        //            // TODO: (mh) (core) Implement AddressModels PrepareModel()
        //            //var shippingAddress = customer.ShippingAddress;
        //            //if (shippingAddress != null)
        //            //{
        //            //    model.OrderReviewData.ShippingAddress.PrepareModel(shippingAddress, false, _addressSettings);
        //            //}

        //            // Selected shipping method.
        //            var shippingOption = customer.GenericAttributes.SelectedShippingOption;
        //            if (shippingOption != null)
        //            {
        //                model.OrderReviewData.ShippingMethod = shippingOption.Name;
        //            }

        //            if (checkoutState.CustomProperties.ContainsKey("HasOnlyOneActiveShippingMethod"))
        //            {
        //                model.OrderReviewData.DisplayShippingMethodChangeOption = !(bool)checkoutState.CustomProperties.Get("HasOnlyOneActiveShippingMethod");
        //            }
        //        }

        //        if (checkoutState.CustomProperties.ContainsKey("HasOnlyOneActivePaymentMethod"))
        //        {
        //            model.OrderReviewData.DisplayPaymentMethodChangeOption = !(bool)checkoutState.CustomProperties.Get("HasOnlyOneActivePaymentMethod");
        //        }

        //        var selectedPaymentMethodSystemName = customer.GenericAttributes.SelectedPaymentMethod;
        //        var paymentMethod = await _paymentService.LoadPaymentMethodBySystemNameAsync(selectedPaymentMethodSystemName);

        //        //// TODO: (ms) (core) PluginMediator.GetLocalizedFriendlyName is missing
        //        ////model.OrderReviewData.PaymentMethod = paymentMethod != null ? _pluginMediator.GetLocalizedFriendlyName(paymentMethod.Metadata) : "";
        //        //model.OrderReviewData.PaymentSummary = checkoutState.PaymentSummary;
        //        //model.OrderReviewData.IsPaymentSelectionSkipped = checkoutState.IsPaymentSelectionSkipped;
        //    }

        //    #endregion

        //    await PrepareButtonPaymentMethodModelAsync(model.ButtonPaymentMethods, cart);

        //    return model;
        //}

        //[NonAction]
        //protected async Task<WishlistModel.WishlistItemModel> PrepareWishlistItemModelAsync(OrganizedShoppingCartItem cartItem)
        //{
        //    Guard.NotNull(cartItem, nameof(cartItem));

        //    var item = cartItem.Item;

        //    var model = new WishlistModel.WishlistItemModel
        //    {
        //        DisableBuyButton = item.Product.DisableBuyButton
        //    };

        //    await PrepareCartItemModelBaseAsync(cartItem, model);

        //    if (cartItem.ChildItems != null)
        //    {
        //        foreach (var childItem in cartItem.ChildItems.Where(x => x.Item.Id != item.Id))
        //        {
        //            model.AddChildItems(await PrepareWishlistItemModelAsync(childItem));
        //        }
        //    }

        //    return model;
        //}

        [NonAction]
        protected async Task<ShoppingCartModel.ShoppingCartItemModel> PrepareShoppingCartItemModelAsync(OrganizedShoppingCartItem cartItem)
        {
            Guard.NotNull(cartItem, nameof(cartItem));

            var item = cartItem.Item;
            var product = item.Product;

            var model = new ShoppingCartModel.ShoppingCartItemModel
            {
                Weight = product.Weight,
                IsShipEnabled = product.IsShippingEnabled,
                IsDownload = product.IsDownload,
                HasUserAgreement = product.HasUserAgreement,
                IsEsd = product.IsEsd,
                DisableWishlistButton = product.DisableWishlistButton,
            };

            if (product.DisplayDeliveryTimeAccordingToStock(_catalogSettings))
            {
                var deliveryTime = await _deliveryTimeService.GetDeliveryTimeAsync(product.GetDeliveryTimeIdAccordingToStock(_catalogSettings));
                if (deliveryTime != null)
                {
                    model.DeliveryTimeName = deliveryTime.GetLocalized(x => x.Name);
                    model.DeliveryTimeHexValue = deliveryTime.ColorHexValue;

                    if (_shoppingCartSettings.DeliveryTimesInShoppingCart is DeliveryTimesPresentation.DateOnly
                        or DeliveryTimesPresentation.LabelAndDate)
                    {
                        model.DeliveryTimeDate = _deliveryTimeService.GetFormattedDeliveryDate(deliveryTime);
                    }
                }
            }

            var basePriceAdjustment = (await _priceCalculationService.GetFinalPriceAsync(product, null)
                - await _priceCalculationService.GetUnitPriceAsync(cartItem, true)) * -1;

            model.BasePrice = await _priceCalculationService.GetBasePriceInfoAsync(product, item.Customer, Services.WorkContext.WorkingCurrency, basePriceAdjustment);

            await PrepareCartItemModelBaseAsync(cartItem, model);

            if (cartItem.Item.BundleItem == null)
            {
                var selectedAttributeValues = await _productAttributeMaterializer.MaterializeProductVariantAttributeValuesAsync(item.AttributeSelection);
                if (selectedAttributeValues != null)
                {
                    var weight = decimal.Zero;
                    foreach (var attributeValue in selectedAttributeValues)
                    {
                        weight += attributeValue.WeightAdjustment;
                    }

                    model.Weight += weight;
                }
            }

            if (cartItem.ChildItems != null)
            {
                foreach (var childItem in cartItem.ChildItems.Where(x => x.Item.Id != item.Id))
                {
                    model.AddChildItems(await PrepareShoppingCartItemModelAsync(childItem));
                }
            }

            return model;
        }

        [NonAction]
        protected async Task PrepareCartItemModelBaseAsync(OrganizedShoppingCartItem cartItem, CartEntityModelBase model)
        {
            Guard.NotNull(cartItem, nameof(cartItem));

            var item = cartItem.Item;
            var product = cartItem.Item.Product;
            var customer = item.Customer;
            var currency = Services.WorkContext.WorkingCurrency;
            var shoppingCartType = item.ShoppingCartType;

            await _productAttributeMaterializer.MergeWithCombinationAsync(product, item.AttributeSelection);

            var productSeName = await product.GetActiveSlugAsync();

            // General model data
            model.Id = item.Id;
            model.Sku = product.Sku;
            model.ProductId = product.Id;
            model.ProductName = product.GetLocalized(x => x.Name);
            model.ProductSeName = productSeName;
            model.ProductUrl = await _productUrlHelper.GetProductUrlAsync(productSeName, cartItem);
            model.EnteredQuantity = item.Quantity;
            model.MinOrderAmount = product.OrderMinimumQuantity;
            model.MaxOrderAmount = product.OrderMaximumQuantity;
            model.QuantityStep = product.QuantityStep > 0 ? product.QuantityStep : 1;
            model.ShortDesc = product.GetLocalized(x => x.ShortDescription);
            model.ProductType = product.ProductType;
            model.VisibleIndividually = product.Visibility != ProductVisibility.Hidden;
            model.CreatedOnUtc = item.UpdatedOnUtc;

            if (item.BundleItem != null)
            {
                model.BundleItem.Id = item.BundleItem.Id;
                model.BundleItem.DisplayOrder = item.BundleItem.DisplayOrder;
                model.BundleItem.HideThumbnail = item.BundleItem.HideThumbnail;
                model.BundlePerItemPricing = item.BundleItem.BundleProduct.BundlePerItemPricing;
                model.BundlePerItemShoppingCart = item.BundleItem.BundleProduct.BundlePerItemShoppingCart;
                model.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
                    item.AttributeSelection,
                    product,
                    customer,
                    includePrices: false,
                    includeGiftCardAttributes: true,
                    includeHyperlinks: true);

                var bundleItemName = item.BundleItem.GetLocalized(x => x.Name);
                if (bundleItemName.Value.HasValue())
                {
                    model.ProductName = bundleItemName;
                }

                var bundleItemShortDescription = item.BundleItem.GetLocalized(x => x.ShortDescription);
                if (bundleItemShortDescription.Value.HasValue())
                {
                    model.ShortDesc = bundleItemShortDescription;
                }

                if (model.BundlePerItemPricing && model.BundlePerItemShoppingCart)
                {
                    var bundleItemSubTotalWithDiscountBase = await _taxService.GetProductPriceAsync(product, await _priceCalculationService.GetSubTotalAsync(cartItem, true));
                    var bundleItemSubTotalWithDiscount = _currencyService.ConvertFromPrimaryCurrency(bundleItemSubTotalWithDiscountBase.Price.Amount, currency);
                    model.BundleItem.PriceWithDiscount = bundleItemSubTotalWithDiscount.ToString();
                }
            }
            else
            {
                model.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(item.AttributeSelection, product, customer);
            }

            var allowedQuantities = product.ParseAllowedQuantities();
            foreach (var quantity in allowedQuantities)
            {
                model.AllowedQuantities.Add(new SelectListItem
                {
                    Text = quantity.ToString(),
                    Value = quantity.ToString(),
                    Selected = item.Quantity == quantity
                });
            }

            var quantityUnit = await _db.QuantityUnits.GetQuantityUnitByIdAsync(product.QuantityUnitId ?? 0, _catalogSettings.ShowDefaultQuantityUnit);
            if (quantityUnit != null)
            {
                model.QuantityUnitName = quantityUnit.GetLocalized(x => x.Name);
            }

            if (product.IsRecurring)
            {
                model.RecurringInfo = T("ShoppingCart.RecurringPeriod", product.RecurringCycleLength, product.RecurringCyclePeriod.GetLocalizedEnum());
            }

            if (product.CallForPrice)
            {
                model.UnitPrice = T("Products.CallForPrice");
            }
            else
            {
                var unitPriceWithDiscountBase = await _taxService.GetProductPriceAsync(product, await _priceCalculationService.GetUnitPriceAsync(cartItem, true));
                var unitPriceWithDiscount = _currencyService.ConvertFromPrimaryCurrency(unitPriceWithDiscountBase.Price.Amount, currency);
                model.UnitPrice = unitPriceWithDiscount.ToString();
            }

            // Subtotal and discount.
            if (product.CallForPrice)
            {
                model.SubTotal = T("Products.CallForPrice");
            }
            else
            {
                var cartItemSubTotalWithDiscount = await _priceCalculationService.GetSubTotalAsync(cartItem, true);
                var cartItemSubTotalWithDiscountBase = await _taxService.GetProductPriceAsync(product, cartItemSubTotalWithDiscount);
                cartItemSubTotalWithDiscount = _currencyService.ConvertFromPrimaryCurrency(cartItemSubTotalWithDiscountBase.Price.Amount, currency);

                model.SubTotal = cartItemSubTotalWithDiscount.ToString();

                // Display an applied discount amount.
                var cartItemSubTotalWithoutDiscount = await _priceCalculationService.GetSubTotalAsync(cartItem, false);
                var cartItemSubTotalWithoutDiscountBase = await _taxService.GetProductPriceAsync(product, cartItemSubTotalWithoutDiscount);
                var cartItemSubTotalDiscountBase = cartItemSubTotalWithoutDiscountBase.Price - cartItemSubTotalWithDiscountBase.Price;

                if (cartItemSubTotalDiscountBase > decimal.Zero)
                {
                    var itemDiscount = _currencyService.ConvertFromPrimaryCurrency(cartItemSubTotalDiscountBase.Amount, currency);
                    model.Discount = itemDiscount.ToString();
                }
            }

            if (item.BundleItem != null)
            {
                if (_shoppingCartSettings.ShowProductBundleImagesOnShoppingCart)
                {
                    model.Image = await PrepareCartItemImageModelAsync(product, item.AttributeSelection, _mediaSettings.CartThumbBundleItemPictureSize, model.ProductName);
                }
            }
            else
            {
                if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
                {
                    model.Image = await PrepareCartItemImageModelAsync(product, item.AttributeSelection, _mediaSettings.CartThumbPictureSize, model.ProductName);
                }
            }

            var itemWarnings = new List<string>();
            var isItemValid = await _shoppingCartValidator.ValidateCartItemsAsync(new List<OrganizedShoppingCartItem> { cartItem }, itemWarnings);
            if (!isItemValid)
            {
                itemWarnings.Each(x => model.Warnings.Add(x));
            }
        }

        [NonAction]
        protected async Task<ImageModel> PrepareCartItemImageModelAsync(
            Product product,
            ProductVariantAttributeSelection attributeSelection,
            int pictureSize,
            string productName)
        {
            Guard.NotNull(product, nameof(product));

            var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, attributeSelection);

            MediaFileInfo file = null;
            if (combination != null)
            {
                var fileIds = combination.GetAssignedMediaIds();
                if (fileIds.Any())
                {
                    file = await _mediaService.GetFileByIdAsync(fileIds[0], MediaLoadFlags.AsNoTracking);
                }
            }

            // If no attribute combination image was found, then load product pictures.
            if (file == null)
            {
                var mediaFile = await _db.ProductMediaFiles
                    .AsNoTracking()
                    .Include(x => x.MediaFile)
                    .ApplyProductFilter(product.Id)
                    .FirstOrDefaultAsync();

                if (mediaFile?.MediaFile != null)
                {
                    file = _mediaService.ConvertMediaFile(mediaFile.MediaFile);
                }
            }

            // Let's check whether this product has some parent "grouped" product.
            if (file == null && product.Visibility == ProductVisibility.Hidden && product.ParentGroupedProductId > 0)
            {
                var mediaFile = await _db.ProductMediaFiles
                    .AsNoTracking()
                    .Include(x => x.MediaFile)
                    .ApplyProductFilter(product.ParentGroupedProductId)
                    .FirstOrDefaultAsync();

                if (mediaFile?.MediaFile != null)
                {
                    file = _mediaService.ConvertMediaFile(mediaFile.MediaFile);
                }
            }

            return new ImageModel
            {
                File = file,
                ThumbSize = pictureSize,
                Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? T("Media.Product.ImageLinkTitleFormat", productName),
                Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? T("Media.Product.ImageAlternateTextFormat", productName),
                NoFallback = _catalogSettings.HideProductDefaultPictures,
            };
        }

        [NonAction]
        protected async Task<MiniShoppingCartModel> PrepareMiniShoppingCartModelAsync()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;

            var model = new MiniShoppingCartModel
            {
                ShowProductImages = _shoppingCartSettings.ShowProductImagesInMiniShoppingCart,
                ThumbSize = _mediaSettings.MiniCartThumbPictureSize,
                CurrentCustomerIsGuest = customer.IsGuest(),
                AnonymousCheckoutAllowed = _orderSettings.AnonymousCheckoutAllowed,
                DisplayMoveToWishlistButton = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist),
                ShowBasePrice = _shoppingCartSettings.ShowBasePrice
            };

            var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.ShoppingCart, storeId);
            model.TotalProducts = cart.GetTotalQuantity();

            if (cart.Count == 0)
            {
                return model;
            }

            // TODO: (ms) (core) Broken. Fix it...
            //model.SubTotal = (await _orderCalculationService.GetShoppingCartSubTotalAsync(cart)).SubTotalWithoutDiscount.ToString();
            model.SubTotal = "999 €";

            //a customer should visit the shopping cart page before going to checkout if:
            //1. we have at least one checkout attribute that is reqired
            //2. min order sub-total is OK

            var checkoutAttributes = await _checkoutAttributeMaterializer.GetValidCheckoutAttributesAsync(cart);

            model.DisplayCheckoutButton = !checkoutAttributes.Any(x => x.IsRequired);

            // Products sort descending (recently added products)
            foreach (var cartItem in cart)
            {
                var item = cartItem.Item;
                var product = cartItem.Item.Product;
                var productSeName = await product.GetActiveSlugAsync();

                var cartItemModel = new MiniShoppingCartModel.ShoppingCartItemModel
                {
                    Id = item.Id,
                    ProductId = product.Id,
                    ProductName = product.GetLocalized(x => x.Name),
                    ShortDesc = product.GetLocalized(x => x.ShortDescription),
                    ProductSeName = productSeName,
                    EnteredQuantity = item.Quantity,
                    MaxOrderAmount = product.OrderMaximumQuantity,
                    MinOrderAmount = product.OrderMinimumQuantity,
                    QuantityStep = product.QuantityStep > 0 ? product.QuantityStep : 1,
                    CreatedOnUtc = item.UpdatedOnUtc,
                    ProductUrl = await _productUrlHelper.GetProductUrlAsync(productSeName, cartItem),
                    QuantityUnitName = null,
                    AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
                        item.AttributeSelection,
                        product,
                        null,
                        ", ",
                        false,
                        false,
                        false,
                        false,
                        false)
                };

                if (cartItem.ChildItems != null && _shoppingCartSettings.ShowProductBundleImagesOnShoppingCart)
                {
                    var bundleItems = cartItem.ChildItems.Where(x =>
                        x.Item.Id != item.Id
                        && x.Item.BundleItem != null
                        && !x.Item.BundleItem.HideThumbnail);

                    foreach (var bundleItem in bundleItems)
                    {
                        var bundleItemModel = new MiniShoppingCartModel.ShoppingCartItemBundleItem
                        {
                            ProductName = bundleItem.Item.Product.GetLocalized(x => x.Name),
                            ProductSeName = await bundleItem.Item.Product.GetActiveSlugAsync(),
                        };

                        bundleItemModel.ProductUrl = await _productUrlHelper.GetProductUrlAsync(
                            bundleItem.Item.ProductId,
                            bundleItemModel.ProductSeName,
                            bundleItem.Item.AttributeSelection);

                        var file = await _db.ProductMediaFiles
                            .AsNoTracking()
                            .Include(x => x.MediaFile)
                            .ApplyProductFilter(bundleItem.Item.ProductId)
                            .FirstOrDefaultAsync();

                        if (file?.MediaFile != null)
                        {
                            bundleItemModel.PictureUrl = _mediaService.GetUrl(file.MediaFile, MediaSettings.ThumbnailSizeXxs);
                        }

                        cartItemModel.BundleItems.Add(bundleItemModel);
                    }
                }

                // Unit prices.
                if (product.CallForPrice)
                {
                    cartItemModel.UnitPrice = T("Products.CallForPrice");
                }
                else
                {
                    var attributeCombination = await _productAttributeMaterializer.FindAttributeCombinationAsync(item.ProductId, item.AttributeSelection);
                    product.MergeWithCombination(attributeCombination);

                    var unitPriceWithDiscountBase = await _taxService.GetProductPriceAsync(product, await _priceCalculationService.GetUnitPriceAsync(cartItem, true));
                    var unitPriceWithDiscount = _currencyService.ConvertFromPrimaryCurrency(unitPriceWithDiscountBase.Price.Amount, Services.WorkContext.WorkingCurrency);

                    cartItemModel.UnitPrice = unitPriceWithDiscount.ToString();

                    if (unitPriceWithDiscount != decimal.Zero && model.ShowBasePrice)
                    {
                        cartItemModel.BasePriceInfo = await _priceCalculationService.GetBasePriceInfoAsync(item.Product);
                    }
                }

                // Image.
                if (_shoppingCartSettings.ShowProductImagesInMiniShoppingCart)
                {
                    cartItemModel.Image = await PrepareCartItemImageModelAsync(product, item.AttributeSelection, _mediaSettings.MiniCartThumbPictureSize, cartItemModel.ProductName);
                }

                model.Items.Add(cartItemModel);
            }

            return model;
        }

        //#endregion


        //public IActionResult CartSummary()
        //{
        //    // Stop annoying MiniProfiler report.
        //    return new EmptyResult();
        //}

        [RequireSsl]
        [LocalizedRoute("/cart", Name = "ShoppingCart")]
        public async Task<IActionResult> Cart(ProductVariantQuery query)
        {
            Guard.NotNull(query, nameof(query));

            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
                return RedirectToRoute("Homepage");

            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

            // Allow to fill checkout attributes with values from query string.
            if (query.CheckoutAttributes.Any())
            {
                await ParseAndSaveCheckoutAttributesAsync(cart, query);
            }

            // TODO: (ms) (core) replace this with mapping
            var model = new ShoppingCartModel();// await PrepareShoppingCartModelAsync(cart);

            HttpContext.Session.TrySetObject(CheckoutState.CheckoutStateSessionKey, new CheckoutState());

            return View(model);
        }

        //// INFO: (ms) (core) You can find this information in 1_StoreRoutes in classic code. It should be checked for every action you implement.
        //// TODO: (ms) (core) Remove this comment.
        //[RequireSsl]
        //[LocalizedRoute("/wishlist/{customerGuid:guid?}", Name = "Wishlist")]
        //public async Task<IActionResult> Wishlist(Guid? customerGuid)
        //{
        //    if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
        //    {
        //        return RedirectToRoute("Homepage");
        //    }

        //    // Check customer controllers

        //    var customer = customerGuid.HasValue
        //        ? await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerGuid == customerGuid.Value)
        //        : Services.WorkContext.CurrentCustomer;

        //    if (customer == null)
        //    {
        //        return RedirectToRoute("Homepage");
        //    }

        //    var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);

        //    var model = await PrepareWishlistModelAsync(cart, !customerGuid.HasValue);

        //    return View(model);
        //}

        //#region Offcanvas

        //public async Task<IActionResult> OffCanvasCart()
        //{
        //    var model = new OffCanvasCartModel();

        //    if (await Services.Permissions.AuthorizeAsync(Permissions.System.AccessShop))
        //    {
        //        model.ShoppingCartEnabled = _shoppingCartSettings.MiniShoppingCartEnabled
        //            && await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart);
        //        model.WishlistEnabled = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist);
        //        model.CompareProductsEnabled = _catalogSettings.CompareProductsEnabled;
        //    }

        //    return PartialView(model);
        //}

        public async Task<IActionResult> OffCanvasShoppingCart()
        {
            if (!_shoppingCartSettings.MiniShoppingCartEnabled)
            {
                return Content(string.Empty);
            }
                

            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
            {
                return Content(string.Empty);
            }
            
            var model = await PrepareMiniShoppingCartModelAsync();

            HttpContext.Session.TrySetObject(CheckoutState.CheckoutStateSessionKey, new CheckoutState());

            return PartialView(model);
        }

        //public async Task<IActionResult> OffCanvasWishlist()
        //{
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var storeId = Services.StoreContext.CurrentStore.Id;

        //    var cartItems = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, storeId);

        //    var model = await PrepareWishlistModelAsync(cartItems, true);

        //    model.ThumbSize = _mediaSettings.MiniCartThumbPictureSize;

        //    return PartialView(model);
        //}

        //#endregion

        //#region Shopping Cart

        ///// <summary>
        ///// Validates and saves cart data.
        ///// </summary>
        ///// <param name="query">The <see cref="ProductVariantQuery"/>.</param>
        ///// <param name="useRewardPoints">A value indicating whether to use reward points.</param>        
        //[HttpPost]
        //public async Task<IActionResult> SaveCartData(ProductVariantQuery query, bool useRewardPoints = false)
        //{
        //    var warnings = new List<string>();
        //    var success = await ValidateAndSaveCartDataAsync(query, warnings, useRewardPoints);

        //    return Json(new
        //    {
        //        success,
        //        message = string.Join(Environment.NewLine, warnings)
        //    });
        //}

        ///// <summary>
        ///// Updates cart item quantity in shopping cart.
        ///// </summary>
        ///// <param name="cartItemId">Identifier of <see cref="ShoppingCartItem"/>.</param>
        ///// <param name="newQuantity">The new quantity to set.</param>
        ///// <param name="isCartPage">A value indicating whether the customer is on the cart page or another.</param>
        ///// <param name="isWishlist">A value indicating whether the <see cref="ShoppingCartType"/> is Wishlist or ShoppingCart.</param>        
        //[HttpPost]
        //public async Task<IActionResult> UpdateCartItem(int cartItemId, int newQuantity, bool isCartPage = false, bool isWishlist = false)
        //{
        //    if (!await Services.Permissions.AuthorizeAsync(isWishlist ? Permissions.Cart.AccessWishlist : Permissions.Cart.AccessShoppingCart))
        //        return RedirectToRoute("Homepage");

        //    var warnings = new List<string>();
        //    warnings.AddRange(
        //        await _shoppingCartService.UpdateCartItemAsync(
        //            Services.WorkContext.CurrentCustomer,
        //            cartItemId,
        //            newQuantity,
        //            false));

        //    var cartHtml = string.Empty;
        //    var totalsHtml = string.Empty;

        //    var cart = await _shoppingCartService.GetCartItemsAsync(
        //        null,
        //        isWishlist ? ShoppingCartType.Wishlist : ShoppingCartType.ShoppingCart,
        //        Services.StoreContext.CurrentStore.Id);

        //    if (isCartPage)
        //    {
        //        if (isWishlist)
        //        {
        //            var model = await PrepareWishlistModelAsync(cart);
        //            cartHtml = await this.InvokeViewAsync("WishlistItems", model);
        //        }
        //        else
        //        {
        //            var model = PrepareShoppingCartModelAsync(cart);
        //            cartHtml = await this.InvokeViewAsync("CartItems", model);
        //            totalsHtml = await this.InvokeViewComponentAsync(ViewData, "OrderTotals", new { isEditable = true });
        //        }
        //    }

        //    var subTotal = await _orderCalculationService.GetShoppingCartSubTotalAsync(cart);

        //    return Json(new
        //    {
        //        success = warnings.Count <= 0,
        //        SubTotal = subTotal.SubTotalWithoutDiscount.ToString(),
        //        message = warnings,
        //        cartHtml,
        //        totalsHtml,
        //        displayCheckoutButtons = true
        //    });
        //}

        ///// <summary>
        ///// Removes cart item with identifier <paramref name="cartItemId"/> from either the shopping cart or the wishlist.
        ///// </summary>
        ///// <param name="cartItemId">Identifier of <see cref="ShoppingCartItem"/> to remove.</param>
        ///// <param name="isWishlistItem">A value indicating whether to remove the cart item from wishlist or shopping cart.</param>        
        //[HttpPost]
        //public async Task<IActionResult> DeleteCartItem(int cartItemId, bool isWishlistItem = false)
        //{
        //    if (!await Services.Permissions.AuthorizeAsync(isWishlistItem ? Permissions.Cart.AccessWishlist : Permissions.Cart.AccessShoppingCart))
        //    {
        //        return Json(new { success = false, displayCheckoutButtons = true });
        //    }

        //    // Get shopping cart item.
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var cartType = isWishlistItem ? ShoppingCartType.Wishlist : ShoppingCartType.ShoppingCart;
        //    var item = customer.ShoppingCartItems.FirstOrDefault(x => x.Id == cartItemId && x.ShoppingCartType == cartType);

        //    if (item == null)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            displayCheckoutButtons = true,
        //            message = T("ShoppingCart.DeleteCartItem.Failed").Value
        //        });
        //    }

        //    // Remove the cart item.
        //    await _shoppingCartService.DeleteCartItemsAsync(new[] { item }, removeInvalidCheckoutAttributes: true);

        //    var storeId = Services.StoreContext.CurrentStore.Id;
        //    // Create updated cart model.
        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: storeId);
        //    var wishlist = await _shoppingCartService.GetCartItemsAsync(cartType: ShoppingCartType.Wishlist, storeId: storeId);
        //    var cartHtml = string.Empty;
        //    var totalsHtml = string.Empty;
        //    var cartItemCount = 0;

        //    if (cartType == ShoppingCartType.Wishlist)
        //    {
        //        var model = PrepareWishlistModelAsync(wishlist);
        //        cartHtml = await this.InvokeViewAsync("WishlistItems", model);
        //        cartItemCount = wishlist.Count;
        //    }
        //    else
        //    {
        //        var model = PrepareShoppingCartModelAsync(cart);
        //        cartHtml = await this.InvokeViewAsync("CartItems", model);
        //        totalsHtml = await this.InvokeViewComponentAsync(ViewData, "OrderTotals", new { isEditable = true });
        //        cartItemCount = cart.Count;
        //    }

        //    // Updated cart.
        //    return Json(new
        //    {
        //        success = true,
        //        displayCheckoutButtons = true,
        //        message = T("ShoppingCart.DeleteCartItem.Success").Value,
        //        cartHtml,
        //        totalsHtml,
        //        cartItemCount
        //    });
        //}

        //// TODO: (ms) (core) Add dev docu to all ajax action methods
        //[HttpPost]
        //public async Task<IActionResult> AddProductSimple(int productId, int shoppingCartTypeId = 1, bool forceRedirection = false)
        //{
        //    // Adds products without variants to the cart or redirects user to product details page.
        //    // This method is used on catalog pages (category/manufacturer etc...).

        //    var product = await _db.Products.FindByIdAsync(productId, false);
        //    if (product == null)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            message = T("Products.NotFound", productId)
        //        });
        //    }

        //    // Filter out cases where a product cannot be added to the cart
        //    if (product.ProductType == ProductType.GroupedProduct || product.CustomerEntersPrice || product.IsGiftCard)
        //    {
        //        return Json(new
        //        {
        //            redirect = Url.RouteUrl("Product", new { SeName = await product.GetActiveSlugAsync() }),
        //        });
        //    }

        //    var allowedQuantities = product.ParseAllowedQuantities();
        //    if (allowedQuantities.Length > 0)
        //    {
        //        // The user must select a quantity from the dropdown list, therefore the product cannot be added to the cart
        //        return Json(new
        //        {
        //            redirect = Url.RouteUrl("Product", new { SeName = await product.GetActiveSlugAsync() }),
        //        });
        //    }

        //    // Get product warnings without attribute validations.

        //    var storeId = Services.StoreContext.CurrentStore.Id;
        //    var cartType = (ShoppingCartType)shoppingCartTypeId;

        //    // Get existing shopping cart items. Then, tries to find a cart item with the corresponding product.
        //    var cart = await _shoppingCartService.GetCartItemsAsync(null, cartType, storeId);
        //    var cartItem = cart.FindItemInCart(cartType, product);

        //    var quantityToAdd = product.OrderMinimumQuantity > 0 ? product.OrderMinimumQuantity : 1;

        //    // If we already have the same product in the cart, then use the total quantity to validate
        //    quantityToAdd = cartItem != null ? cartItem.Item.Quantity + quantityToAdd : quantityToAdd;

        //    var productWarnings = new List<string>();
        //    if (!await _shoppingCartValidator.ValidateProductAsync(cartItem.Item, productWarnings, storeId, quantityToAdd))
        //    {
        //        // Product is not valid and therefore cannot be added to the cart. Display standard product warnings.
        //        return Json(new
        //        {
        //            success = false,
        //            message = productWarnings.ToArray()
        //        });
        //    }

        //    // Product looks good so far, let's try adding the product to the cart (with product attribute validation etc.)
        //    var addToCartContext = new AddToCartContext
        //    {
        //        Product = product,
        //        CartType = cartType,
        //        Quantity = quantityToAdd,
        //        AutomaticallyAddRequiredProducts = true
        //    };

        //    if (!await _shoppingCartService.AddToCartAsync(addToCartContext))
        //    {
        //        // Item could not be added to the cart. Most likely, the customer has to select product variant attributes.
        //        return Json(new
        //        {
        //            redirect = Url.RouteUrl("Product", new { SeName = await product.GetActiveSlugAsync() }),
        //        });
        //    }

        //    // Product has been added to the cart. Add to activity log.
        //    _activityLogger.LogActivity(
        //        "PublicStore.AddToShoppingCart",
        //        T("ActivityLog.PublicStore.AddToShoppingCart"),
        //        product.Name);

        //    if (_shoppingCartSettings.DisplayCartAfterAddingProduct || forceRedirection)
        //    {
        //        // Redirect to the shopping cart page
        //        return Json(new
        //        {
        //            redirect = Url.RouteUrl("ShoppingCart"),
        //        });
        //    }

        //    return Json(new
        //    {
        //        success = true,
        //        message = T("Products.ProductHasBeenAddedToTheCart", Url.RouteUrl("ShoppingCart")).Value
        //    });
        //}

        //[HttpPost]
        //public async Task<IActionResult> AddProduct(int productId, int shoppingCartTypeId, ProductVariantQuery query)
        //{
        //    // Adds a product to cart. This method is used on product details page.
        //    var form = HttpContext.Request.Form;
        //    var product = await _db.Products.FindByIdAsync(productId, false);
        //    if (product == null)
        //    {
        //        return Json(new
        //        {
        //            redirect = Url.RouteUrl("Homepage"),
        //        });
        //    }

        //    Money customerEnteredPriceConverted = new();
        //    if (product.CustomerEntersPrice)
        //    {
        //        foreach (var formKey in form.Keys)
        //        {
        //            if (formKey.EqualsNoCase(string.Format("addtocart_{0}.CustomerEnteredPrice", productId)))
        //            {
        //                if (decimal.TryParse(form[formKey], out var customerEnteredPrice))
        //                {
        //                    customerEnteredPriceConverted = _currencyService.ConvertToPrimaryCurrency(new Money(customerEnteredPrice, Services.WorkContext.WorkingCurrency));
        //                }

        //                break;
        //            }
        //        }
        //    }

        //    var quantity = product.OrderMinimumQuantity;
        //    var key1 = "addtocart_{0}.EnteredQuantity".FormatWith(productId);
        //    var key2 = "addtocart_{0}.AddToCart.EnteredQuantity".FormatWith(productId);

        //    if (form.Keys.Contains(key1))
        //    {
        //        _ = int.TryParse(form[key1], out quantity);
        //    }
        //    else if (form.Keys.Contains(key2))
        //    {
        //        _ = int.TryParse(form[key2], out quantity);
        //    }

        //    // Save item
        //    var cartType = (ShoppingCartType)shoppingCartTypeId;

        //    var addToCartContext = new AddToCartContext
        //    {
        //        Product = product,
        //        VariantQuery = query,
        //        CartType = cartType,
        //        CustomerEnteredPrice = customerEnteredPriceConverted,
        //        Quantity = quantity,
        //        AutomaticallyAddRequiredProducts = true
        //    };

        //    if (!await _shoppingCartService.AddToCartAsync(addToCartContext))
        //    {
        //        // Product could not be added to the cart/wishlist
        //        // Display warnings.
        //        return Json(new
        //        {
        //            success = false,
        //            message = addToCartContext.Warnings.ToArray()
        //        });
        //    }

        //    // Product was successfully added to the cart/wishlist.
        //    // Log activity and redirect if enabled.

        //    bool redirect;
        //    string routeUrl, activity, resourceName;

        //    switch (cartType)
        //    {
        //        case ShoppingCartType.Wishlist:
        //            {
        //                redirect = _shoppingCartSettings.DisplayWishlistAfterAddingProduct;
        //                routeUrl = "Wishlist";
        //                activity = "PublicStore.AddToWishlist";
        //                resourceName = "ActivityLog.PublicStore.AddToWishlist";
        //                break;
        //            }
        //        case ShoppingCartType.ShoppingCart:
        //        default:
        //            {
        //                redirect = _shoppingCartSettings.DisplayCartAfterAddingProduct;
        //                routeUrl = "ShoppingCart";
        //                activity = "PublicStore.AddToShoppingCart";
        //                resourceName = "ActivityLog.PublicStore.AddToShoppingCart";
        //                break;
        //            }
        //    }

        //    _activityLogger.LogActivity(activity, T(resourceName), product.Name);

        //    return redirect
        //        ? Json(new
        //        {
        //            redirect = Url.RouteUrl(routeUrl),
        //        })
        //        : Json(new
        //        {
        //            success = true
        //        });
        //}

        //// Ajax.
        //[HttpPost]
        //[ActionName("MoveItemBetweenCartAndWishlist")]
        //public async Task<IActionResult> MoveItemBetweenCartAndWishlistAjax(int cartItemId, ShoppingCartType cartType, bool isCartPage = false)
        //{
        //    if (await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart)
        //        || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            message = T("Common.NoProcessingSecurityIssue").Value
        //        });
        //    }

        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var storeId = Services.StoreContext.CurrentStore.Id;
        //    var cart = await _shoppingCartService.GetCartItemsAsync(customer, cartType, storeId);
        //    var cartItem = cart.Where(x => x.Item.Id == cartItemId).FirstOrDefault();

        //    if (cartItem != null)
        //    {
        //        var addToCartContext = new AddToCartContext
        //        {
        //            Item = cartItem.Item,
        //            Customer = customer,
        //            CartType = cartType == ShoppingCartType.Wishlist ? ShoppingCartType.ShoppingCart : ShoppingCartType.Wishlist,
        //            StoreId = storeId,
        //            AutomaticallyAddRequiredProducts = true,
        //            Product = cartItem.Item.Product,
        //            RawAttributes = cartItem.Item.RawAttributes,
        //            CustomerEnteredPrice = new(cartItem.Item.CustomerEnteredPrice, Services.WorkContext.WorkingCurrency),
        //            Quantity = cartItem.Item.Quantity,
        //            ChildItems = cartItem.ChildItems.Select(x => x.Item).ToList(),
        //            BundleItem = cartItem.Item.BundleItem
        //        };

        //        var warnings = await _shoppingCartService.CopyAsync(addToCartContext);

        //        if (_shoppingCartSettings.MoveItemsFromWishlistToCart && addToCartContext.Warnings.Count == 0)
        //        {
        //            // No warnings (already in cart). Let's remove the item from origin.
        //            await _shoppingCartService.DeleteCartItemsAsync(new[] { cartItem.Item });
        //        }

        //        if (addToCartContext.Warnings.Count == 0)
        //        {
        //            var cartHtml = string.Empty;
        //            var totalsHtml = string.Empty;
        //            var message = string.Empty;
        //            var cartItemCount = 0;

        //            if (_shoppingCartSettings.DisplayCartAfterAddingProduct && cartType == ShoppingCartType.Wishlist)
        //            {
        //                // Redirect to the shopping cart page.
        //                return Json(new
        //                {
        //                    redirect = Url.RouteUrl("ShoppingCart")
        //                });
        //            }

        //            if (isCartPage)
        //            {
        //                if (cartType == ShoppingCartType.Wishlist)
        //                {
        //                    var wishlist = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, storeId);

        //                    var model = await PrepareWishlistModelAsync(wishlist);

        //                    cartHtml = await this.InvokeViewAsync("WishlistItems", model);
        //                    message = T("Products.ProductHasBeenAddedToTheCart");
        //                    cartItemCount = wishlist.Count;
        //                }
        //                else
        //                {
        //                    cart = await _shoppingCartService.GetCartItemsAsync(customer, cartType, storeId);

        //                    var model = await PrepareShoppingCartModelAsync(cart);

        //                    cartHtml = await this.InvokeViewAsync("CartItems", model);
        //                    totalsHtml = await this.InvokeViewComponentAsync(ViewData, "OrderTotals", new { isEditable = true });
        //                    message = T("Products.ProductHasBeenAddedToTheWishlist");
        //                    cartItemCount = cart.Count;
        //                }
        //            }

        //            return Json(new
        //            {
        //                success = true,
        //                wasMoved = _shoppingCartSettings.MoveItemsFromWishlistToCart,
        //                message,
        //                cartHtml,
        //                totalsHtml,
        //                cartItemCount,
        //                displayCheckoutButtons = true
        //            });
        //        }
        //    }

        //    return Json(new
        //    {
        //        success = false,
        //        message = T("Products.ProductNotAddedToTheCart").Value
        //    });
        //}

        //[HttpPost, ActionName("Wishlist")]
        //[FormValueRequired("addtocartbutton")]
        //public async Task<IActionResult> AddItemsToCartFromWishlist(Guid? customerGuid)
        //{
        //    if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart)
        //        || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
        //    {
        //        return RedirectToRoute("Homepage");
        //    }

        //    var pageCustomer = !customerGuid.HasValue
        //        ? Services.WorkContext.CurrentCustomer
        //        : await _db.Customers
        //            .AsNoTracking()
        //            .Where(x => x.CustomerGuid == customerGuid)
        //            .FirstOrDefaultAsync();

        //    var storeId = Services.StoreContext.CurrentStore.Id;
        //    var pageCart = await _shoppingCartService.GetCartItemsAsync(pageCustomer, ShoppingCartType.Wishlist, storeId);

        //    var allWarnings = new List<string>();
        //    var numberOfAddedItems = 0;
        //    var form = HttpContext.Request.Form;

        //    var allIdsToAdd = form["addtocart"].FirstOrDefault() != null
        //        ? form["addtocart"].Select(x => int.Parse(x)).ToList()
        //        : new List<int>();

        //    foreach (var cartItem in pageCart)
        //    {
        //        if (allIdsToAdd.Contains(cartItem.Item.Id))
        //        {
        //            var addToCartContext = new AddToCartContext()
        //            {
        //                Item = cartItem.Item,
        //                Customer = Services.WorkContext.CurrentCustomer,
        //                CartType = ShoppingCartType.ShoppingCart,
        //                StoreId = storeId,
        //                RawAttributes = cartItem.Item.RawAttributes,
        //                ChildItems = cartItem.ChildItems.Select(x => x.Item).ToList(),
        //                CustomerEnteredPrice = new Money(cartItem.Item.CustomerEnteredPrice, _currencyService.PrimaryCurrency),
        //                Product = cartItem.Item.Product,
        //                Quantity = cartItem.Item.Quantity
        //            };

        //            if (await _shoppingCartService.CopyAsync(addToCartContext))
        //            {
        //                numberOfAddedItems++;
        //            }

        //            if (_shoppingCartSettings.MoveItemsFromWishlistToCart && !customerGuid.HasValue && addToCartContext.Warnings.Count == 0)
        //            {
        //                await _shoppingCartService.DeleteCartItemsAsync(new[] { cartItem.Item });
        //            }

        //            allWarnings.AddRange(addToCartContext.Warnings);
        //        }
        //    }

        //    if (numberOfAddedItems > 0)
        //    {
        //        return RedirectToRoute("ShoppingCart");
        //    }

        //    var cart = await _shoppingCartService.GetCartItemsAsync(pageCustomer, ShoppingCartType.Wishlist, storeId);
        //    var model = PrepareWishlistModelAsync(cart, !customerGuid.HasValue);

        //    NotifyInfo(T("Products.SelectProducts"), true);

        //    return View(model);
        //}


        //[RequireSsl]
        //[GdprConsent]
        //public async Task<IActionResult> EmailWishlist()
        //{
        //    if (!_shoppingCartSettings.EmailWishlistEnabled || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
        //        return RedirectToRoute("Homepage");

        //    var customer = Services.WorkContext.CurrentCustomer;

        //    var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);
        //    if (cart.Count == 0)
        //        return RedirectToRoute("Homepage");

        //    var model = new WishlistEmailAFriendModel
        //    {
        //        YourEmailAddress = customer.Email,
        //        DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnEmailWishlistToFriendPage
        //    };

        //    return View(model);
        //}

        //[HttpPost, ActionName("EmailWishlist")]
        //[FormValueRequired("send-email")]
        //[ValidateCaptcha]
        //[GdprConsent]
        //public async Task<IActionResult> EmailWishlistSend(WishlistEmailAFriendModel model, string captchaError)
        //{
        //    if (!_shoppingCartSettings.EmailWishlistEnabled || !await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
        //        return RedirectToRoute("Homepage");

        //    var customer = Services.WorkContext.CurrentCustomer;

        //    var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);
        //    if (cart.Count == 0)
        //    {
        //        return RedirectToRoute("Homepage");
        //    }

        //    if (_captchaSettings.ShowOnEmailWishlistToFriendPage && captchaError.HasValue())
        //    {
        //        ModelState.AddModelError("", captchaError);
        //    }

        //    // Check whether the current customer is guest and ia allowed to email wishlist.
        //    if (customer.IsGuest() && !_shoppingCartSettings.AllowAnonymousUsersToEmailWishlist)
        //    {
        //        ModelState.AddModelError("", T("Wishlist.EmailAFriend.OnlyRegisteredUsers"));
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        await _messageFactory.SendShareWishlistMessageAsync(
        //            customer,
        //            model.YourEmailAddress,
        //            model.FriendEmail,
        //            HtmlUtils.ConvertPlainTextToHtml(model.PersonalMessage.HtmlEncode()));

        //        model.SuccessfullySent = true;
        //        model.Result = T("Wishlist.EmailAFriend.SuccessfullySent");

        //        return View(model);
        //    }

        //    // If we got this far, something failed, redisplay form.
        //    ModelState.AddModelError("", T("Common.Error.Sendmail"));
        //    model.DisplayCaptcha = _captchaSettings.CanDisplayCaptcha && _captchaSettings.ShowOnEmailWishlistToFriendPage;

        //    return View(model);
        //}

        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("estimateshipping")]
        //public async Task<IActionResult> GetEstimateShipping(EstimateShippingModel shippingModel, ProductVariantQuery query)
        //{
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var storeId = Services.StoreContext.CurrentStore.Id;
        //    var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.ShoppingCart, storeId);

        //    await ParseAndSaveCheckoutAttributesAsync(cart, query);

        //    var model = await PrepareShoppingCartModelAsync(cart, setEstimateShippingDefaultAddress: false);

        //    model.EstimateShipping.CountryId = shippingModel.CountryId;
        //    model.EstimateShipping.StateProvinceId = shippingModel.StateProvinceId;
        //    model.EstimateShipping.ZipPostalCode = shippingModel.ZipPostalCode;

        //    if (cart.IsShippingRequired())
        //    {
        //        var shippingInfoUrl = Url.TopicAsync("ShippingInfo").ToString();
        //        if (shippingInfoUrl.HasValue())
        //        {
        //            model.EstimateShipping.ShippingInfoUrl = shippingInfoUrl;
        //        }

        //        var address = new Address
        //        {
        //            CountryId = shippingModel.CountryId,
        //            Country = await _db.Countries.FindByIdAsync(shippingModel.CountryId.GetValueOrDefault(), false),
        //            StateProvinceId = shippingModel.StateProvinceId,
        //            StateProvince = await _db.StateProvinces.FindByIdAsync(shippingModel.StateProvinceId.GetValueOrDefault(), false),
        //            ZipPostalCode = shippingModel.ZipPostalCode,
        //        };

        //        var getShippingOptionResponse = _shippingService.GetShippingOptions(cart, address, storeId: storeId);
        //        if (!getShippingOptionResponse.Success)
        //        {
        //            foreach (var error in getShippingOptionResponse.Errors)
        //            {
        //                model.EstimateShipping.Warnings.Add(error);
        //            }
        //        }
        //        else
        //        {
        //            if (getShippingOptionResponse.ShippingOptions.Count > 0)
        //            {
        //                var shippingMethods = await _shippingService.GetAllShippingMethodsAsync(storeId: storeId);

        //                foreach (var shippingOption in getShippingOptionResponse.ShippingOptions)
        //                {
        //                    var soModel = new EstimateShippingModel.ShippingOptionModel
        //                    {
        //                        ShippingMethodId = shippingOption.ShippingMethodId,
        //                        Name = shippingOption.Name,
        //                        Description = shippingOption.Description
        //                    };

        //                    var currency = Services.WorkContext.WorkingCurrency;

        //                    var shippingTotal = await _orderCalculationService.AdjustShippingRateAsync(
        //                        cart,
        //                        new(shippingOption.Rate, currency),
        //                        shippingOption,
        //                        shippingMethods);

        //                    var rate = await _taxService.GetShippingPriceAsync(shippingTotal.Amount);
        //                    soModel.Price = rate.Price.ToString(true);

        //                    model.EstimateShipping.ShippingOptions.Add(soModel);
        //                }
        //            }
        //            else
        //            {
        //                model.EstimateShipping.Warnings.Add(T("Checkout.ShippingIsNotAllowed"));
        //            }
        //        }
        //    }

        //    return View(model);
        //}

        //#endregion

        //#region Upload

        //[HttpPost]
        //[MaxMediaFileSize]
        //public async Task<IActionResult> UploadFileProductAttribute(int productId, int productAttributeId, IFormFile formFile)
        //{
        //    var product = await _db.Products.FindByIdAsync(productId, false);
        //    if (product == null || formFile == null || !product.Published || product.Deleted || product.IsSystemProduct)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            downloadGuid = Guid.Empty,
        //        });
        //    }

        //    // Ensure that this attribute belongs to this product and has the "file upload" type
        //    var pva = await _db.ProductVariantAttributes
        //        .AsNoTracking()
        //        .ApplyProductFilter(new[] { productId })
        //        .Include(x => x.ProductAttribute)
        //        .Where(x => x.ProductAttributeId == productAttributeId)
        //        .FirstOrDefaultAsync();

        //    if (pva == null || pva.AttributeControlType != AttributeControlType.FileUpload)
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            downloadGuid = Guid.Empty,
        //        });
        //    }

        //    var download = new Download
        //    {
        //        DownloadGuid = Guid.NewGuid(),
        //        UseDownloadUrl = false,
        //        DownloadUrl = "",
        //        UpdatedOnUtc = DateTime.UtcNow,
        //        EntityId = productId,
        //        EntityName = "ProductAttribute",
        //        IsTransient = true
        //    };

        //    var mediaFile = await _downloadService.InsertDownloadAsync(download, formFile.OpenReadStream(), formFile.FileName);

        //    return Json(new
        //    {
        //        id = download.MediaFileId,
        //        name = mediaFile.Name,
        //        type = mediaFile.MediaType,
        //        thumbUrl = _mediaService.GetUrl(download.MediaFile, _mediaSettings.ProductThumbPictureSize, string.Empty),
        //        success = true,
        //        message = T("ShoppingCart.FileUploaded").Value,
        //        downloadGuid = download.DownloadGuid,
        //    });
        //}

        //[HttpPost]
        //[MaxMediaFileSize]
        //// TODO: (ms) (core) TEST that IFormFile is beeing
        //public async Task<IActionResult> UploadFileCheckoutAttribute(IFormFile formFile)
        //{
        //    if (formFile == null || !formFile.FileName.HasValue())
        //    {
        //        return Json(new
        //        {
        //            success = false,
        //            downloadGuid = Guid.Empty
        //        });
        //    }

        //    var download = new Download
        //    {
        //        DownloadGuid = Guid.NewGuid(),
        //        UseDownloadUrl = false,
        //        DownloadUrl = "",
        //        UpdatedOnUtc = DateTime.UtcNow,
        //        EntityId = 0,
        //        EntityName = "CheckoutAttribute",
        //        IsTransient = true
        //    };

        //    var mediaFile = await _downloadService.InsertDownloadAsync(download, formFile.OpenReadStream(), formFile.FileName);

        //    return Json(new
        //    {
        //        id = download.MediaFileId,
        //        name = mediaFile.Name,
        //        type = mediaFile.MediaType,
        //        thumbUrl = await _mediaService.GetUrlAsync(mediaFile.File.Id, _mediaSettings.ProductThumbPictureSize, host: string.Empty),
        //        success = true,
        //        message = T("ShoppingCart.FileUploaded").Value,
        //        downloadGuid = download.DownloadGuid,
        //    });
        //}

        //#endregion

        ///// <summary>
        ///// Validates and saves cart data. When valid, customer is directed to the checkout process, otherwise the customer is 
        ///// redirected back to the shopping cart.
        ///// </summary>
        ///// <param name="query">The <see cref="ProductVariantQuery"/>.</param>
        ///// <param name="useRewardPoints">A value indicating whether to use reward points.</param>
        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("startcheckout")]
        //public async Task<IActionResult> StartCheckout(ProductVariantQuery query, bool useRewardPoints = false)
        //{
        //    ShoppingCartModel model;
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var warnings = new List<string>();
        //    if (!await ValidateAndSaveCartDataAsync(query, warnings, useRewardPoints))
        //    {
        //        // Something is wrong with the checkout data. Redisplay shopping cart.
        //        var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);
        //        model = await PrepareShoppingCartModelAsync(cart, validateCheckoutAttributes: true);
        //        return View(model);
        //    }

        //    //savechanges

        //    // Everything is OK.
        //    if (customer.IsGuest())
        //    {
        //        if (_customerSettings.UserRegistrationType == UserRegistrationType.Disabled)
        //        {
        //            return RedirectToAction("BillingAddress", "Checkout");
        //        }
        //        else if (_orderSettings.AnonymousCheckoutAllowed)
        //        {
        //            return RedirectToRoute("Login", new { checkoutAsGuest = true, returnUrl = Url.RouteUrl("ShoppingCart") });
        //        }
        //        else
        //        {
        //            return new UnauthorizedResult();
        //        }
        //    }
        //    else
        //    {
        //        return RedirectToRoute("Checkout");
        //    }
        //}

        ///// <summary>
        ///// Redirects customer back to last visited shopping page.
        ///// </summary>
        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("continueshopping")]
        //public ActionResult ContinueShopping()
        //{
        //    var returnUrl = Services.WorkContext.CurrentCustomer.GenericAttributes.LastContinueShoppingPage;
        //    return RedirectToReferrer(returnUrl);
        //}

        //#region Discount/GiftCard coupon codes & Reward points

        ///// <summary>
        ///// Tries to apply <paramref name="discountCouponcode"/> as <see cref="Discount"/> and applies 
        ///// selected checkout attributes.
        ///// </summary>
        ///// <param name="query">The <see cref="ProductVariantQuery"/></param>
        ///// <param name="discountCouponcode">The <see cref="Discount.CouponCode"/> to apply.</param>
        ///// <returns></returns>
        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("applydiscountcouponcode")]
        //public async Task<IActionResult> ApplyDiscountCoupon(ProductVariantQuery query, string discountCouponcode)
        //{
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);
        //    var model = await PrepareShoppingCartModelAsync(cart);
        //    model.DiscountBox.IsWarning = true;

        //    await ParseAndSaveCheckoutAttributesAsync(cart, query);

        //    if (discountCouponcode.HasValue())
        //    {
        //        var discount = await _db.Discounts
        //            .AsNoTracking()
        //            .FirstOrDefaultAsync(x => x.CouponCode == discountCouponcode);

        //        var isDiscountValid = discount != null
        //            && discount.RequiresCouponCode
        //            && await _discountService.IsDiscountValidAsync(discount, customer, discountCouponcode);

        //        if (isDiscountValid)
        //        {
        //            var discountApplied = true;
        //            var oldCartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);

        //            customer.GenericAttributes.DiscountCouponCode = discountCouponcode;

        //            if (oldCartTotal.Total.HasValue)
        //            {
        //                var newCartTotal = await _orderCalculationService.GetShoppingCartTotalAsync(cart);
        //                discountApplied = oldCartTotal.Total != newCartTotal.Total;
        //            }

        //            if (discountApplied)
        //            {
        //                model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.Applied");
        //                model.DiscountBox.IsWarning = false;
        //            }
        //            else
        //            {
        //                model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.NoMoreDiscount");

        //                customer.GenericAttributes.DiscountCouponCode = null;
        //            }
        //        }
        //        else
        //        {
        //            model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.WrongDiscount");
        //        }
        //    }
        //    else
        //    {
        //        model.DiscountBox.Message = T("ShoppingCart.DiscountCouponCode.WrongDiscount");
        //    }

        //    return View(model);
        //}

        ///// <summary>
        ///// Removes the applied discount coupon code from current customer.
        ///// </summary>
        //[HttpPost]
        //public async Task<IActionResult> RemoveDiscountCoupon()
        //{
        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

        //    var customer = Services.WorkContext.CurrentCustomer;
        //    customer.GenericAttributes.DiscountCouponCode = null;

        //    var model = await PrepareShoppingCartModelAsync(cart);

        //    var discountHtml = await this.InvokeViewAsync("_DiscountBox", model.DiscountBox);
        //    var totalsHtml = await this.InvokeViewComponentAsync(ViewData, typeof(OrderTotalsViewComponent), new { isEditable = true });

        //    // Updated cart.
        //    return Json(new
        //    {
        //        success = true,
        //        totalsHtml,
        //        discountHtml,
        //        displayCheckoutButtons = true
        //    });
        //}

        ///// <summary>
        ///// Applies gift card by coupon code to cart.
        ///// </summary>
        ///// <param name="query">The <see cref="ProductVariantQuery"/>.</param>
        ///// <param name="giftCardCouponCode">The <see cref="GiftCard.GiftCardCouponCode"/> to apply.</param>
        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("applygiftcardcouponcode")]
        //public async Task<IActionResult> ApplyGiftCard(ProductVariantQuery query, string giftCardCouponCode)
        //{
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var storeId = Services.StoreContext.CurrentStore.Id;

        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: storeId);

        //    await ParseAndSaveCheckoutAttributesAsync(cart, query);

        //    var model = await PrepareShoppingCartModelAsync(cart);
        //    model.GiftCardBox.IsWarning = true;

        //    if (!cart.ContainsRecurringItem())
        //    {
        //        if (giftCardCouponCode.HasValue())
        //        {
        //            var giftCard = await _db.GiftCards
        //                .AsNoTracking()
        //                .ApplyCouponFilter(new[] { giftCardCouponCode })
        //                .FirstOrDefaultAsync();

        //            var isGiftCardValid = giftCard != null && _giftCardService.ValidateGiftCard(giftCard, storeId);
        //            if (isGiftCardValid)
        //            {
        //                var couponCodes = new List<GiftCardCouponCode>(customer.GenericAttributes.GiftCardCouponCodes);
        //                if (couponCodes.Select(x => x.Value).Contains(giftCardCouponCode))
        //                {
        //                    var giftCardCoupon = new GiftCardCouponCode(giftCardCouponCode);
        //                    couponCodes.Add(giftCardCoupon);
        //                    customer.GenericAttributes.GiftCardCouponCodes = couponCodes;
        //                }

        //                model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.Applied");
        //                model.GiftCardBox.IsWarning = false;
        //            }
        //            else
        //            {
        //                model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.WrongGiftCard");
        //            }
        //        }
        //        else
        //        {
        //            model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.WrongGiftCard");
        //        }
        //    }
        //    else
        //    {
        //        model.GiftCardBox.Message = T("ShoppingCart.GiftCardCouponCode.DontWorkWithAutoshipProducts");
        //    }

        //    return View(model);
        //}

        ///// <summary>
        ///// Removes applied gift card by <paramref name="giftCardId"/> from customer.
        ///// </summary>
        ///// <param name="giftCardId"><see cref="GiftCard"/> identifier to remove.</param>        
        //[HttpPost]
        //public async Task<IActionResult> RemoveGiftCardCode(int giftCardId)
        //{
        //    var customer = Services.WorkContext.CurrentCustomer;
        //    var storeId = Services.StoreContext.CurrentStore.Id;

        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: storeId);
        //    var model = await PrepareShoppingCartModelAsync(cart);

        //    var giftCard = await _db.GiftCards.FindByIdAsync(giftCardId, false);
        //    if (giftCard != null)
        //    {
        //        var giftCards = new List<GiftCardCouponCode>(customer.GenericAttributes.GiftCardCouponCodes);
        //        var found = giftCards.Where(x => x.Value == giftCard.GiftCardCouponCode).FirstOrDefault();
        //        if (giftCards.Remove(found))
        //        {
        //            customer.GenericAttributes.GiftCardCouponCodes = giftCards;
        //        }
        //    }

        //    var totalsHtml = await this.InvokeViewComponentAsync(ViewData, "OrderTotals", new { isEditable = true });

        //    // Updated cart.
        //    return Json(new
        //    {
        //        success = true,
        //        totalsHtml,
        //        displayCheckoutButtons = true
        //    });
        //}

        //[HttpPost, ActionName("Cart")]
        //[FormValueRequired("applyrewardpoints")]
        //public async Task<IActionResult> ApplyRewardPoints(ProductVariantQuery query, bool useRewardPoints = false)
        //{
        //    var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

        //    await ParseAndSaveCheckoutAttributesAsync(cart, query);

        //    var model = await PrepareShoppingCartModelAsync(cart);
        //    model.RewardPoints.UseRewardPoints = useRewardPoints;

        //    var customer = Services.WorkContext.CurrentCustomer;
        //    customer.GenericAttributes.UseRewardPointsDuringCheckout = useRewardPoints;

        //    return View(model);
        //}

        #endregion
    }
}

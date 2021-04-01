using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
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
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;
using Smartstore.Utilities.Html;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Models.ShoppingCart;

namespace Smartstore.Web.Controllers
{
    public class ShoppingCartController : PublicControllerBase
    {
        // TODO: (ms) (core) SmartDbContext should always be first => then services & helpers => then settings & last the lazy stuff that's not needed often.
        private readonly SmartDbContext _db;
        private readonly ITaxService _taxService;
        private readonly IMediaService _mediaService;
        private readonly IPaymentService _paymentService;
        private readonly ICurrencyService _currencyService;
        private readonly IDiscountService _discountService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly ILocalizationService _localizationService;
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
        private readonly OrderSettings _orderSettings;
        private readonly MediaSettings _mediaSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;

        public ShoppingCartController(
            SmartDbContext db,
            ITaxService taxService,
            IMediaService mediaService,
            IPaymentService paymentService,
            ICurrencyService currencyService,
            IDiscountService discountService,
            IShoppingCartService shoppingCartService,
            ILocalizationService localizationService,
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
            OrderSettings orderSettings,
            MediaSettings mediaSettings,
            ShippingSettings shippingSettings,
            RewardPointsSettings rewardPointsSettings)
        {
            _db = db;
            _taxService = taxService;
            _mediaService = mediaService;
            _paymentService = paymentService;
            _currencyService = currencyService;
            _discountService = discountService;
            _shoppingCartService = shoppingCartService;
            _localizationService = localizationService;
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
            _orderSettings = orderSettings;
            _mediaSettings = mediaSettings;
            _shippingSettings = shippingSettings;
            _rewardPointsSettings = rewardPointsSettings;
        }

        public IActionResult CartSummary()
        {
            // Stop annoying MiniProfiler report.
            return new EmptyResult();
        }

        [NonAction]
        private async Task PrepareButtonPaymentMethodModelAsync(ButtonPaymentMethodModel model, IList<OrganizedShoppingCartItem> cart)
        {
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
                if (cart.IncludesMatchingItems(x=>x.IsRecurring) && paymentMethod.Value.RecurringPaymentType == RecurringPaymentType.NotSupported)
                    continue;

                // TODO: (ms) PaymentMethod: implement GetPaymentInfoRoute equivalent
                //paymentMethod.Value.GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues);

                //if (actionName.HasValue() && controllerName.HasValue())
                //{
                //    model.Items.Add(new ButtonPaymentMethodModel.ButtonPaymentMethodItem
                //    {
                //        ActionName = actionName,
                //        ControllerName = controllerName,
                //        RouteValues = routeValues
                //    });
                //}
            }
        }

        [NonAction]
        protected async Task ParseAndSaveCheckoutAttributesAsync(List<OrganizedShoppingCartItem> cart, ProductVariantQuery query)
        {
            var selectedAttributes = new CheckoutAttributeSelection(string.Empty);

            var customer = cart.GetCustomer();

            if (cart.IsNullOrEmpty())
            {
                if (customer != null)
                {
                    customer.GenericAttributes.CheckoutAttributes = selectedAttributes;
                    _db.TryUpdate(customer);
                }

                return;
            }

            var checkoutAttributes = _db.CheckoutAttributes
                .AsNoTracking()
                .AsCaching()
                .ApplyStandardFilter(false, Services.StoreContext.CurrentStore.Id)
                .ToList();

            if (!cart.IncludesMatchingItems(x => x.IsShippingEnabled))
            {
                // Remove attributes which require shippable products.
                checkoutAttributes = checkoutAttributes.RemoveShippableAttributes().ToList();
            }

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

                            //var selectedValues = selectedItems
                            //    .SelectMany(x => x.Value.SplitSafe(","))
                            //    .Select(x => (x?.ToInt()).GetValueOrDefault())
                            //    .Where(x => x > 0)
                            //    .Select(x => (object)x);

                            //selectedAttributes.AddAttribute(attribute.Id, selectedValues);
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


        // TODO: (ms) Add methods dev documentations
        [NonAction]
        protected async Task PrepareWishlistModel(WishlistModel model, IList<OrganizedShoppingCartItem> cart, bool isEditable = true)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(model, nameof(model));

            model.EmailWishlistEnabled = _shoppingCartSettings.EmailWishlistEnabled;
            model.IsEditable = isEditable;
            model.DisplayAddToCart = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart);

            if (cart.Count == 0)
                return;

            #region Simple properties

            var customer = cart.FirstOrDefault().Item.Customer;
            model.CustomerGuid = customer.CustomerGuid;
            model.CustomerFullname = customer.GetFullName();
            model.ShowProductImages = _shoppingCartSettings.ShowProductImagesOnShoppingCart;
            model.ShowProductBundleImages = _shoppingCartSettings.ShowProductBundleImagesOnShoppingCart;
            model.ShowItemsFromWishlistToCartButton = _shoppingCartSettings.ShowItemsFromWishlistToCartButton;
            model.ShowSku = _catalogSettings.ShowProductSku;
            model.DisplayShortDesc = _shoppingCartSettings.ShowShortDesc;
            model.BundleThumbSize = _mediaSettings.CartThumbBundleItemPictureSize;


            var warnings = new List<string>();
            if (!await _shoppingCartValidator.ValidateCartAsync(cart, warnings))
            {
                model.Warnings.AddRange(warnings);
            }

            #endregion

            #region Cart items

            foreach (var sci in cart)
            {
                // TODO: (ms) Implement rest of Preparation methods for wishlist/cart
                //var wishlistCartItemModel = PrepareWishlistCartItemModel(sci);

                //model.Items.Add(wishlistCartItemModel);
            }

            #endregion
        }


        /// <summary>
        /// Prepares shopping cart model.
        /// </summary>
        /// <param name="model">Model instance.</param>
        /// <param name="cart">Shopping cart items.</param>
        /// <param name="isEditable">A value indicating whether the cart is editable.</param>
        /// <param name="validateCheckoutAttributes">A value indicating whether checkout attributes get validated.</param>
        /// <param name="prepareEstimateShippingIfEnabled">A value indicating whether to prepare "Estimate shipping" model.</param>
        /// <param name="setEstimateShippingDefaultAddress">A value indicating whether to prefill "Estimate shipping" model with the default customer address.</param>
        /// <param name="prepareAndDisplayOrderReviewData">A value indicating whether to prepare review data (such as billing/shipping address, payment or shipping data entered during checkout).</param>
        [NonAction]
        protected async Task PrepareShoppingCartModelAsync(
            ShoppingCartModel model,
            IList<OrganizedShoppingCartItem> cart,
            bool isEditable = true,
            bool validateCheckoutAttributes = false,
            bool prepareEstimateShippingIfEnabled = true,
            bool setEstimateShippingDefaultAddress = true,
            bool prepareAndDisplayOrderReviewData = false)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(model, nameof(model));

            if (cart.Count == 0)
            {
                return;
            }

            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var currency = Services.WorkContext.WorkingCurrency;

            #region Simple properties

            model.MediaDimensions = _mediaSettings.CartThumbPictureSize;
            model.BundleThumbSize = _mediaSettings.CartThumbBundleItemPictureSize;
            model.DeliveryTimesPresentation = _shoppingCartSettings.DeliveryTimesInShoppingCart;
            model.DisplayShortDesc = _shoppingCartSettings.ShowShortDesc;
            model.DisplayBasePrice = _shoppingCartSettings.ShowBasePrice;
            model.DisplayWeight = _shoppingCartSettings.ShowWeight;
            model.DisplayMoveToWishlistButton = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist);
            model.IsEditable = isEditable;
            model.ShowProductImages = _shoppingCartSettings.ShowProductImagesOnShoppingCart;
            model.ShowProductBundleImages = _shoppingCartSettings.ShowProductBundleImagesOnShoppingCart;
            model.ShowSku = _catalogSettings.ShowProductSku;

            var measure = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId);
            if (measure != null)
            {
                model.MeasureUnitName = measure.GetLocalized(x => x.Name);
            }

            model.CheckoutAttributeInfo = HtmlUtils.ConvertPlainTextToTable(HtmlUtils.ConvertHtmlToPlainText(
                await _checkoutAttributeFormatter.FormatAttributesAsync(customer.GenericAttributes.CheckoutAttributes, customer)
            ));

            model.TermsOfServiceEnabled = _orderSettings.TermsOfServiceEnabled;

            // Gift card and gift card boxes.
            model.DiscountBox.Display = _shoppingCartSettings.ShowDiscountBox;
            var discountCouponCode = customer.GenericAttributes.DiscountCouponCode;
            var discount = await _db.Discounts.Where(x => x.CouponCode == discountCouponCode).FirstOrDefaultAsync();

            if (discount != null
                && discount.RequiresCouponCode
                && await _discountService.IsDiscountValidAsync(discount, customer))
            {
                model.DiscountBox.CurrentCode = discount.CouponCode;
            }

            model.GiftCardBox.Display = _shoppingCartSettings.ShowGiftCardBox;
            model.DisplayCommentBox = _shoppingCartSettings.ShowCommentBox;
            model.DisplayEsdRevocationWaiverBox = _shoppingCartSettings.ShowEsdRevocationWaiverBox;

            // Reward points.
            if (_rewardPointsSettings.Enabled && !cart.IncludesMatchingItems(x => x.IsRecurring) && !customer.IsGuest())
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
            var valid = await _shoppingCartValidator.ValidateCartAsync(cart, warnings, validateCheckoutAttributes, customer.GenericAttributes.CheckoutAttributes);
            if (!valid)
            {
                model.Warnings.AddRange(warnings);
            }

            #endregion

            #region Checkout attributes

            var checkoutAttributes = _db.CheckoutAttributes
                .AsNoTracking()
                .AsCaching()
                .ApplyStandardFilter(false, Services.StoreContext.CurrentStore.Id)
                .ToList();

            if (!cart.IncludesMatchingItems(x => x.IsShippingEnabled))
            {
                // Remove attributes which require shippable products.
                checkoutAttributes = checkoutAttributes.RemoveShippableAttributes().ToList();
            }

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
                        if (await Services.Permissions.AuthorizeAsync(Permissions.Catalog.DisplayPrice))
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
                model.EstimateShipping.Enabled = _shippingSettings.EstimateShippingEnabled && cart.Any() && cart.IncludesMatchingItems(x => x.IsShippingEnabled);
                if (model.EstimateShipping.Enabled)
                {
                    // Countries.
                    var defaultEstimateCountryId = setEstimateShippingDefaultAddress && customer.ShippingAddress != null
                        ? customer.ShippingAddress.CountryId
                        : model.EstimateShipping.CountryId;

                    var countriesForShipping = await _db.Countries
                        .AsNoTracking()
                        .AsCaching()
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
                        model.EstimateShipping.AvailableStates.Add(
                            new SelectListItem { Text = await _localizationService.GetResourceAsync("Address.OtherNonUS"), Value = "0" }
                            );
                    }

                    if (setEstimateShippingDefaultAddress && customer.ShippingAddress != null)
                    {
                        model.EstimateShipping.ZipPostalCode = customer.ShippingAddress.ZipPostalCode;
                    }
                }
            }

            #endregion

            #region Cart items

            foreach (var sci in cart)
            {
                // TODO: (ms) Implement missing methods like PrepareShoppingCartItemModel

                //var shoppingCartItemModel = PrepareShoppingCartItemModel(sci);
                //model.Items.Add(shoppingCartItemModel);
            }

            #endregion

            #region Order review data

            if (prepareAndDisplayOrderReviewData)
            {
                // TODO: (ms) GetCheckoutState is missing SafeGetValue & SafeSet
                //var checkoutState = HttpContext.GetCheckoutState();

                //model.OrderReviewData.Display = true;

                //// Billing info.
                //var billingAddress = customer.BillingAddress;
                //if (billingAddress != null)
                //{
                //    model.OrderReviewData.BillingAddress.PrepareModel(billingAddress, false, _addressSettings);
                //}

                //// Shipping info.
                //if (cart.IsShippingRequired())
                //{
                //    model.OrderReviewData.IsShippable = true;

                //    var shippingAddress = customer.ShippingAddress;
                //    if (shippingAddress != null)
                //    {
                //        model.OrderReviewData.ShippingAddress.PrepareModel(shippingAddress, false, _addressSettings);
                //    }

                //    // Selected shipping method.
                //    var shippingOption = customer.GenericAttributes.SelectedShippingOption;
                //    if (shippingOption != null)
                //    {
                //        model.OrderReviewData.ShippingMethod = shippingOption.Name;
                //    }

                //    if (checkoutState.CustomProperties.ContainsKey("HasOnlyOneActiveShippingMethod"))
                //    {
                //        model.OrderReviewData.DisplayShippingMethodChangeOption = !(bool)checkoutState.CustomProperties.Get("HasOnlyOneActiveShippingMethod");
                //    }
                //}

                //if (checkoutState.CustomProperties.ContainsKey("HasOnlyOneActivePaymentMethod"))
                //{
                //    model.OrderReviewData.DisplayPaymentMethodChangeOption = !(bool)checkoutState.CustomProperties.Get("HasOnlyOneActivePaymentMethod");
                //}

                //var selectedPaymentMethodSystemName = customer.GenericAttributes.SelectedPaymentMethod;
                //var paymentMethod = await _paymentService.LoadPaymentMethodBySystemNameAsync(selectedPaymentMethodSystemName);

                //// TODO: (ms) PluginMediator.GetLocalizedFriendlyName is missing
                ////model.OrderReviewData.PaymentMethod = paymentMethod != null ? _pluginMediator.GetLocalizedFriendlyName(paymentMethod.Metadata) : "";
                //model.OrderReviewData.PaymentSummary = checkoutState.PaymentSummary;
                //model.OrderReviewData.IsPaymentSelectionSkipped = checkoutState.IsPaymentSelectionSkipped;
            }

            #endregion

            PrepareButtonPaymentMethodModelAsync(model.ButtonPaymentMethods, cart);
        }


        public async Task<IActionResult> Cart(ProductVariantQuery query)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
                return RedirectToRoute("HomePage");

            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

            // Allow to fill checkout attributes with values from query string.
            if (query.CheckoutAttributes.Any())
            {
                await ParseAndSaveCheckoutAttributesAsync(cart, query);
            }

            var model = new ShoppingCartModel();
            await PrepareShoppingCartModelAsync(model, cart);

            //HttpContext.Session.SafeSet(CheckoutState.CheckoutStateSessionKey, new CheckoutState());

            return View(model);
        }

        public async Task<IActionResult> Wishlist(Guid? customerGuid)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
                return RedirectToRoute("HomePage");

            var customer = customerGuid.HasValue
                ? _db.Customers.AsNoTracking().FirstOrDefault(x => x.CustomerGuid == customerGuid.Value)
                : Services.WorkContext.CurrentCustomer;

            if (customer == null)
            {
                return RedirectToRoute("HomePage");
            }

            var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);

            var model = new WishlistModel();
            // TODO: (ms) Implement PrepareWishlistModel -> combined with PrepareShopingCartModel
            // PrepareWishlistModel(model, cart, !customerGuid.HasValue);
            return View(model);
        }


        // TODO: (ms) (core) Remove it. This is a view component already.
        //public ActionResult OffCanvasCart()
        //{
        //    var model = new OffCanvasCartModel();

        //    if (Services.Permissions.Authorize(Permissions.System.AccessShop))
        //    {
        //        model.ShoppingCartEnabled = _shoppingCartSettings.MiniShoppingCartEnabled && Services.Permissions.Authorize(Permissions.Cart.AccessShoppingCart);
        //        model.WishlistEnabled = Services.Permissions.Authorize(Permissions.Cart.AccessWishlist);
        //        model.CompareProductsEnabled = _catalogSettings.CompareProductsEnabled;
        //    }

        //    return PartialView(model);
        //}

        public async Task<IActionResult> OffCanvasShoppingCart()
        {
            if (!_shoppingCartSettings.MiniShoppingCartEnabled)
                return Content(string.Empty);

            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
                return Content(string.Empty);

            var model = await PrepareMiniShoppingCartModelAsync();

            HttpContext.Session.TrySetObject(CheckoutState.CheckoutStateSessionKey, new CheckoutState());

            return PartialView(model);
        }

        public async Task<IActionResult> OffCanvasWishlist()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var storeId = Services.StoreContext.CurrentStore.Id;

            var cartItems = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, storeId);

            var model = await PrepareWishlistModelAsync(cartItems, true);

            // reformat AttributeInfo: this is bad! Put this in PrepareMiniWishlistModel later.
            model.Items.Each(async x =>
            {
                // don't display QuantityUnitName in OffCanvasWishlist
                x.QuantityUnitName = null;

                var sci = cartItems.Where(c => c.Item.Id == x.Id).FirstOrDefault();

                if (sci != null)
                {
                    x.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
                        sci.Item.AttributeSelection,
                        sci.Item.Product,
                        null,
                        htmlEncode: false,
                        separator: ", ",
                        includePrices: false,
                        includeGiftCardAttributes: false,
                        includeHyperlinks: false);
                }
            });

            model.ThumbSize = _mediaSettings.MiniCartThumbPictureSize;

            return PartialView(model);
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

            // TODO: (ms) (core) Finish the job.

            return model;
        }

        [NonAction]
        protected async Task<WishlistModel> PrepareWishlistModelAsync(IList<OrganizedShoppingCartItem> cart, bool isEditable = true)
        {
            Guard.NotNull(cart, nameof(cart));

            var model = new WishlistModel
            {
                IsEditable = isEditable,
                EmailWishlistEnabled = _shoppingCartSettings.EmailWishlistEnabled,
                DisplayAddToCart = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart)
            };

            if (cart.Count == 0)
                return model;

            var customer = cart.FirstOrDefault()?.Item.Customer ?? Services.WorkContext.CurrentCustomer;
            model.CustomerGuid = customer.CustomerGuid;
            model.CustomerFullname = customer.GetFullName();
            model.ShowProductImages = _shoppingCartSettings.ShowProductImagesOnShoppingCart;
            model.ShowProductBundleImages = _shoppingCartSettings.ShowProductBundleImagesOnShoppingCart;
            model.ShowItemsFromWishlistToCartButton = _shoppingCartSettings.ShowItemsFromWishlistToCartButton;
            model.ShowSku = _catalogSettings.ShowProductSku;
            model.DisplayShortDesc = _shoppingCartSettings.ShowShortDesc;
            model.BundleThumbSize = _mediaSettings.CartThumbBundleItemPictureSize;

            // Cart warnings
            var warnings = new List<string>();
            var cartIsValid = await _shoppingCartValidator.ValidateCartAsync(cart, warnings);
            if (!cartIsValid)
            {
                model.Warnings.AddRange(warnings);
            }

            foreach (var item in cart)
            {
                model.Items.Add(await PrepareWishlistCartItemModelAsync(item));
            }

            return model;
        }

        // TODO: (ms) Encapsulates matching functionality with PrepareShoppingCartItemModel and extract as base method
        [NonAction]
        private async Task<WishlistModel.ShoppingCartItemModel> PrepareWishlistCartItemModelAsync(OrganizedShoppingCartItem cartItem)
        {
            Guard.NotNull(cartItem, nameof(cartItem));

            var item = cartItem.Item;
            var product = item.Product;
            var customer = item.Customer;
            var currency = Services.WorkContext.WorkingCurrency;

            await _productAttributeMaterializer.MergeWithCombinationAsync(product, item.AttributeSelection);

            var productSeName = await product.GetActiveSlugAsync();

            var model = new WishlistModel.ShoppingCartItemModel
            {
                Id = item.Id,
                Sku = product.Sku,
                ProductId = product.Id,
                ProductName = product.GetLocalized(x => x.Name),
                ProductSeName = productSeName,
                ProductUrl = await _productUrlHelper.GetProductUrlAsync(productSeName, cartItem),
                EnteredQuantity = item.Quantity,
                MinOrderAmount = product.OrderMinimumQuantity,
                MaxOrderAmount = product.OrderMaximumQuantity,
                QuantityStep = product.QuantityStep > 0 ? product.QuantityStep : 1,
                ShortDesc = product.GetLocalized(x => x.ShortDescription),
                ProductType = product.ProductType,
                VisibleIndividually = product.Visibility != ProductVisibility.Hidden,
                CreatedOnUtc = item.UpdatedOnUtc,
                DisableBuyButton = product.DisableBuyButton,
            };

            if (item.BundleItem != null)
            {
                model.BundleItem.Id = item.BundleItem.Id;
                model.BundleItem.DisplayOrder = item.BundleItem.DisplayOrder;
                model.BundleItem.HideThumbnail = item.BundleItem.HideThumbnail;
                model.BundlePerItemPricing = item.BundleItem.BundleProduct.BundlePerItemPricing;
                model.BundlePerItemShoppingCart = item.BundleItem.BundleProduct.BundlePerItemShoppingCart;
                model.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(item.AttributeSelection, product, customer,
                    includePrices: false, includeGiftCardAttributes: false, includeHyperlinks: false);

                var bundleItemName = item.BundleItem.GetLocalized(x => x.Name);
                if (bundleItemName.HasValue())
                {
                    model.ProductName = bundleItemName;
                }

                var bundleItemShortDescription = item.BundleItem.GetLocalized(x => x.ShortDescription);
                if (bundleItemShortDescription.HasValue())
                {
                    model.ShortDesc = bundleItemShortDescription;
                }

                if (model.BundlePerItemPricing && model.BundlePerItemShoppingCart)
                {
                    (var bundleItemPriceBase, var bundleItemTaxRate) = await _taxService.GetProductPriceAsync(product, await _priceCalculationService.GetSubTotalAsync(cartItem, true));
                    var bundleItemPrice = _currencyService.ConvertFromPrimaryCurrency(bundleItemPriceBase.Amount, currency);
                    model.BundleItem.PriceWithDiscount = bundleItemPrice.ToString();
                }
            }
            else
            {
                model.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(item.AttributeSelection, product, customer);
            }

            var allowedQuantities = product.ParseAllowedQuantities();
            foreach (var qty in allowedQuantities)
            {
                model.AllowedQuantities.Add(new SelectListItem
                {
                    Text = qty.ToString(),
                    Value = qty.ToString(),
                    Selected = item.Quantity == qty
                });
            }

            var quantityUnit = await _db.QuantityUnits
                .AsNoTracking()
                .ApplyQuantityUnitFilter(product.QuantityUnitId)
                .FirstOrDefaultAsync();

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
                var unitPriceWithDiscount = await _priceCalculationService.GetUnitPriceAsync(cartItem, true);
                var unitPriceBaseWithDiscount = await _taxService.GetProductPriceAsync(product, unitPriceWithDiscount);
                unitPriceWithDiscount = _currencyService.ConvertFromPrimaryCurrency(unitPriceBaseWithDiscount.Price.Amount, currency);

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
                    var shoppingCartItemDiscount = _currencyService.ConvertFromPrimaryCurrency(cartItemSubTotalDiscountBase.Amount, currency);
                    model.Discount = shoppingCartItemDiscount.ToString();
                }
            }

            if (item.BundleItem != null)
            {
                if (_shoppingCartSettings.ShowProductBundleImagesOnShoppingCart)
                {
                    model.Image = await PrepareCartItemImageModelAsync(product, _mediaSettings.CartThumbBundleItemPictureSize, model.ProductName, item.AttributeSelection);
                }
            }
            else
            {
                if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
                {
                    model.Image = await PrepareCartItemImageModelAsync(product, _mediaSettings.CartThumbPictureSize, model.ProductName, item.AttributeSelection);
                }
            }

            var itemWarnings = new List<string>();
            var itemIsValid = await _shoppingCartValidator.ValidateCartAsync(new List<OrganizedShoppingCartItem> { cartItem }, itemWarnings);
            if (!itemIsValid)
            {
                model.Warnings.AddRange(itemWarnings);
            }

            if (cartItem.ChildItems != null)
            {
                foreach (var childItem in cartItem.ChildItems.Where(x => x.Item.Id != item.Id))
                {
                    var childModel = await PrepareWishlistCartItemModelAsync(childItem);
                    model.ChildItems.Add(childModel);
                }
            }

            return model;
        }

        [NonAction]
        protected async Task<ImageModel> PrepareCartItemImageModelAsync(Product product, int pictureSize, string productName, ProductVariantAttributeSelection attributeSelection)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(attributeSelection, nameof(attributeSelection));

            MediaFileInfo file = null;
            var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, attributeSelection);
            if (combination != null)
            {
                var fileIds = combination.GetAssignedMediaIds();
                if (fileIds?.Any() ?? false)
                {
                    file = await _mediaService.GetFileByIdAsync(fileIds[0], MediaLoadFlags.AsNoTracking);
                }
            }

            // No attribute combination image, then load product picture.
            if (file == null)
            {
                var productMediaFile = await _db.ProductMediaFiles
                    .AsNoTracking()
                    .Include(x => x.MediaFile)
                    .ApplyProductFilter(new[] { product.Id })
                    .FirstOrDefaultAsync();

                if (productMediaFile != null)
                {
                    file = _mediaService.ConvertMediaFile(productMediaFile.MediaFile);
                }
            }

            // Let's check whether this product has some parent "grouped" product.
            if (file == null && product.Visibility == ProductVisibility.Hidden && product.ParentGroupedProductId > 0)
            {
                var productMediaFile = await _db.ProductMediaFiles
                    .AsNoTracking()
                    .Include(x => x.MediaFile)
                    .ApplyProductFilter(new[] { product.ParentGroupedProductId })
                    .FirstOrDefaultAsync();

                if (productMediaFile != null)
                {
                    file = _mediaService.ConvertMediaFile(productMediaFile.MediaFile);
                }
            }

            var pm = new ImageModel
            {
                Id = file?.Id ?? 0,
                ThumbSize = pictureSize,
                NoFallback = _catalogSettings.HideProductDefaultPictures,
                Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? T("Media.Product.ImageLinkTitleFormat", productName),
                Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? T("Media.Product.ImageAlternateTextFormat", productName),
                File = file
            };

            return pm;
        }
    }
}

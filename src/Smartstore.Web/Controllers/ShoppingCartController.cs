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

        // TODO: (ms) (core) Add methods dev documentations
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

            model.Items.Each(async x =>
            {
                // Do not display QuantityUnitName in OffCanvasWishlist
                x.QuantityUnitName = null;

                var item = cart.Where(c => c.Item.Id == x.Id).FirstOrDefault();

                if (item != null)
                {
                    x.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
                        item.Item.AttributeSelection,
                        item.Item.Product,
                        null,
                        htmlEncode: false,
                        separator: ", ",
                        includePrices: false,
                        includeGiftCardAttributes: false,
                        includeHyperlinks: false);
                }
            });

            return model;
        }

        // TODO: (ms) (core) Encapsulates matching functionality with PrepareShoppingCartItemModel and extract as base method
        [NonAction]
        protected async Task<WishlistModel.ShoppingCartItemModel> PrepareWishlistCartItemModelAsync(OrganizedShoppingCartItem cartItem)
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
        protected async Task<ShoppingCartModel> PrepareShoppingCartModelAsync(
        IList<OrganizedShoppingCartItem> cart,
        bool isEditable = true,
        bool validateCheckoutAttributes = false,
        bool prepareEstimateShippingIfEnabled = true,
        bool setEstimateShippingDefaultAddress = true,
        bool prepareAndDisplayOrderReviewData = false)
        {
            Guard.NotNull(cart, nameof(cart));

            if (cart.Count == 0)
            {
                return new();
            }

            var store = Services.StoreContext.CurrentStore;
            var customer = Services.WorkContext.CurrentCustomer;
            var currency = Services.WorkContext.WorkingCurrency;

            #region Simple properties

            var model = new ShoppingCartModel
            {
                MediaDimensions = _mediaSettings.CartThumbPictureSize,
                BundleThumbSize = _mediaSettings.CartThumbBundleItemPictureSize,
                DeliveryTimesPresentation = _shoppingCartSettings.DeliveryTimesInShoppingCart,
                DisplayShortDesc = _shoppingCartSettings.ShowShortDesc,
                DisplayBasePrice = _shoppingCartSettings.ShowBasePrice,
                DisplayWeight = _shoppingCartSettings.ShowWeight,
                DisplayMoveToWishlistButton = await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist),
                IsEditable = isEditable,
                ShowProductImages = _shoppingCartSettings.ShowProductImagesOnShoppingCart,
                ShowProductBundleImages = _shoppingCartSettings.ShowProductBundleImagesOnShoppingCart,
                ShowSku = _catalogSettings.ShowProductSku,
                TermsOfServiceEnabled = _orderSettings.TermsOfServiceEnabled,
                DisplayCommentBox = _shoppingCartSettings.ShowCommentBox,
                DisplayEsdRevocationWaiverBox = _shoppingCartSettings.ShowEsdRevocationWaiverBox

            };

            var measure = await _db.MeasureWeights.FindByIdAsync(_measureSettings.BaseWeightId, false);
            if (measure != null)
            {
                model.MeasureUnitName = measure.GetLocalized(x => x.Name);
            }

            model.CheckoutAttributeInfo = HtmlUtils.ConvertPlainTextToTable(
                HtmlUtils.ConvertHtmlToPlainText(
                    await _checkoutAttributeFormatter.FormatAttributesAsync(customer.GenericAttributes.CheckoutAttributes, customer))
                );

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
            var cartIsValid = await _shoppingCartValidator.ValidateCartAsync(cart, warnings, validateCheckoutAttributes, customer.GenericAttributes.CheckoutAttributes);
            if (!cartIsValid)
            {
                model.Warnings.AddRange(warnings);
            }

            #endregion

            #region Checkout attributes

            var checkoutAttributes = await _checkoutAttributeMaterializer.GetValidCheckoutAttributesAsync(cart);

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
                        model.EstimateShipping.AvailableStates.Add(new SelectListItem { Text = await _localizationService.GetResourceAsync("Address.OtherNonUS"), Value = "0" });
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
                // TODO: (ms) (core) Implement missing method PrepareShoppingCartItemModel()
                //var shoppingCartItemModel = PrepareCartItemImageModelAsync(sci);
                //model.Items.Add(shoppingCartItemModel);

            }

            #endregion

            #region Order review data

            if (prepareAndDisplayOrderReviewData)
            {
                //TODO: (ms)(core)GetCheckoutState (HTTP Extensions) is missing (Use TryGet/ TrySet)
                //var checkoutState = HttpContext.get();

                model.OrderReviewData.Display = true;

                // Billing info.
                // TODO: (ms) (core) Implement AddressModels PrepareModel()
                //var billingAddress = customer.BillingAddress;
                //if (billingAddress != null)
                //{
                //    model.OrderReviewData.BillingAddress.PrepareModel(billingAddress, false, _addressSettings);
                //}

                // Shipping info.
                if (cart.IsShippingRequired())
                {
                    model.OrderReviewData.IsShippable = true;

                    // TODO: (ms) (core) Implement AddressModels PrepareModel()
                    //var shippingAddress = customer.ShippingAddress;
                    //if (shippingAddress != null)
                    //{
                    //    model.OrderReviewData.ShippingAddress.PrepareModel(shippingAddress, false, _addressSettings);
                    //}

                    // Selected shipping method.
                    //var shippingOption = customer.GenericAttributes.SelectedShippingOption;
                    //if (shippingOption != null)
                    //{
                    //    model.OrderReviewData.ShippingMethod = shippingOption.Name;
                    //}

                    //if (checkoutState.CustomProperties.ContainsKey("HasOnlyOneActiveShippingMethod"))
                    //{
                    //    model.OrderReviewData.DisplayShippingMethodChangeOption = !(bool)checkoutState.CustomProperties.Get("HasOnlyOneActiveShippingMethod");
                    //}
                }

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

            await PrepareButtonPaymentMethodModelAsync(model.ButtonPaymentMethods, cart);

            return model;
        }

        //[NonAction]
        //protected async Task<ShoppingCartModel.ShoppingCartItemModel> PrepareShoppingCartItemModelAsync(OrganizedShoppingCartItem cartItem)
        //{
        //    var item = cartItem.Item;
        //    var product = cartItem.Item.Product;
        //    var currency = Services.WorkContext.WorkingCurrency;
        //    var customer = Services.WorkContext.CurrentCustomer;

        //    var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, item.AttributeSelection);
        //    product.MergeWithCombination(combination);

        //    var productSeName = await product.GetActiveSlugAsync();
        //    var model = new ShoppingCartModel.ShoppingCartItemModel
        //    {
        //        Id = item.Id,
        //        Sku = product.Sku,
        //        ProductId = product.Id,
        //        ProductName = product.GetLocalized(x => x.Name),
        //        ProductSeName = productSeName,
        //        ShortDesc = product.GetLocalized(x => x.ShortDescription),
        //        VisibleIndividually = product.Visibility != ProductVisibility.Hidden,
        //        EnteredQuantity = item.Quantity,
        //        MinOrderAmount = product.OrderMinimumQuantity,
        //        MaxOrderAmount = product.OrderMaximumQuantity,
        //        QuantityStep = product.QuantityStep > 0 ? product.QuantityStep : 1,
        //        IsShipEnabled = product.IsShippingEnabled,
        //        ProductUrl = await _productUrlHelper.GetProductUrlAsync(productSeName, cartItem),
        //        ProductType = product.ProductType,
        //        Weight = product.Weight,
        //        HasUserAgreement = product.HasUserAgreement,
        //        IsDownload = product.IsDownload,
        //        IsEsd = product.IsEsd,
        //        CreatedOnUtc = item.UpdatedOnUtc,
        //        DisableWishlistButton = product.DisableWishlistButton
        //    };

        //    if (item.BundleItem != null)
        //    {
        //        model.BundlePerItemPricing = item.BundleItem.BundleProduct.BundlePerItemPricing;
        //        model.BundlePerItemShoppingCart = item.BundleItem.BundleProduct.BundlePerItemShoppingCart;

        //        model.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(
        //            item.AttributeSelection, 
        //            product, 
        //            customer, 
        //            includePrices: false, 
        //            includeGiftCardAttributes: true,
        //            includeHyperlinks: true);

        //        var bundleItemName = item.BundleItem.GetLocalized(x => x.Name);
        //        if (bundleItemName.Value.HasValue())
        //        {
        //            model.ProductName = bundleItemName;
        //        }

        //        var bundleItemShortDescription = item.BundleItem.GetLocalized(x => x.ShortDescription);
        //        if (bundleItemShortDescription.Value.HasValue())
        //        {
        //            model.ShortDesc = bundleItemShortDescription;
        //        }

        //        model.BundleItem.Id = item.BundleItem.Id;
        //        model.BundleItem.DisplayOrder = item.BundleItem.DisplayOrder;
        //        model.BundleItem.HideThumbnail = item.BundleItem.HideThumbnail;

        //        if (model.BundlePerItemPricing && model.BundlePerItemShoppingCart)
        //        {
        //            var bundleItemSubTotalWithDiscountBase = await _taxService.GetProductPriceAsync(product, await _priceCalculationService.GetSubTotalAsync(cartItem, true));
        //            var bundleItemSubTotalWithDiscount = _currencyService.ConvertFromPrimaryCurrency(bundleItemSubTotalWithDiscountBase.Price.Amount, currency);
        //            model.BundleItem.PriceWithDiscount = bundleItemSubTotalWithDiscount.ToString();
        //        }
        //    }
        //    else
        //    {
        //        model.AttributeInfo = await _productAttributeFormatter.FormatAttributesAsync(item.AttributeSelection, product, customer);

        //        var selectedAttributeValues = _productAttributeParser.ParseProductVariantAttributeValues(item.AttributesXml).ToList();
        //        if (selectedAttributeValues != null)
        //        {
        //            foreach (var attributeValue in selectedAttributeValues)
        //            {
        //                model.Weight = decimal.Add(model.Weight, attributeValue.WeightAdjustment);
        //            }
        //        }
        //    }

        //    if (product.DisplayDeliveryTimeAccordingToStock(_catalogSettings))
        //    {
        //        var deliveryTime = _deliveryTimeService.GetDeliveryTime(product);
        //        if (deliveryTime != null)
        //        {
        //            model.DeliveryTimeName = deliveryTime.GetLocalized(x => x.Name);
        //            model.DeliveryTimeHexValue = deliveryTime.ColorHexValue;

        //            if (_shoppingCartSettings.DeliveryTimesInShoppingCart == DeliveryTimesPresentation.DateOnly ||
        //                _shoppingCartSettings.DeliveryTimesInShoppingCart == DeliveryTimesPresentation.LabelAndDate)
        //            {
        //                model.DeliveryTimeDate = _deliveryTimeService.GetFormattedDeliveryDate(deliveryTime);
        //            }
        //        }
        //    }

        //    var quantityUnit = _quantityUnitService.GetQuantityUnitById(product.QuantityUnitId);
        //    if (quantityUnit != null)
        //    {
        //        model.QuantityUnitName = quantityUnit.GetLocalized(x => x.Name);
        //    }

        //    var allowedQuantities = product.ParseAllowedQuatities();
        //    foreach (var qty in allowedQuantities)
        //    {
        //        model.AllowedQuantities.Add(new SelectListItem
        //        {
        //            Text = qty.ToString(),
        //            Value = qty.ToString(),
        //            Selected = item.Quantity == qty
        //        });
        //    }

        //    if (product.IsRecurring)
        //    {
        //        model.RecurringInfo = string.Format(T("ShoppingCart.RecurringPeriod"),
        //            product.RecurringCycleLength, product.RecurringCyclePeriod.GetLocalizedEnum(_localizationService, _workContext));
        //    }

        //    if (product.CallForPrice)
        //    {
        //        model.UnitPrice = T("Products.CallForPrice");
        //    }
        //    else
        //    {
        //        var shoppingCartUnitPriceWithDiscountBase = _taxService.GetProductPrice(product, _priceCalculationService.GetUnitPrice(cartItem, true), out var taxRate);
        //        var shoppingCartUnitPriceWithDiscount = _currencyService.ConvertFromPrimaryStoreCurrency(shoppingCartUnitPriceWithDiscountBase, currency);

        //        model.UnitPrice = _priceFormatter.FormatPrice(shoppingCartUnitPriceWithDiscount);
        //    }

        //    // Subtotal and discount.
        //    if (product.CallForPrice)
        //    {
        //        model.SubTotal = T("Products.CallForPrice");
        //    }
        //    else
        //    {
        //        decimal taxRate, itemSubTotalWithDiscountBase, itemSubTotalWithDiscount, itemSubTotalWithoutDiscountBase = decimal.Zero;

        //        if (currency.RoundOrderItemsEnabled)
        //        {
        //            // Gross > Net RoundFix.
        //            var priceWithDiscount = _taxService.GetProductPrice(product, _priceCalculationService.GetUnitPrice(cartItem, true), out taxRate);
        //            itemSubTotalWithDiscountBase = priceWithDiscount.RoundIfEnabledFor(currency) * cartItem.Item.Quantity;

        //            itemSubTotalWithDiscount = _currencyService.ConvertFromPrimaryStoreCurrency(itemSubTotalWithDiscountBase, currency);
        //            model.SubTotal = _priceFormatter.FormatPrice(itemSubTotalWithDiscount);

        //            var priceWithoutDiscount = _taxService.GetProductPrice(product, _priceCalculationService.GetUnitPrice(cartItem, false), out taxRate);
        //            itemSubTotalWithoutDiscountBase = priceWithoutDiscount.RoundIfEnabledFor(currency) * cartItem.Item.Quantity;
        //        }
        //        else
        //        {
        //            itemSubTotalWithDiscountBase = _taxService.GetProductPrice(product, _priceCalculationService.GetSubTotal(cartItem, true), out taxRate);

        //            itemSubTotalWithDiscount = _currencyService.ConvertFromPrimaryStoreCurrency(itemSubTotalWithDiscountBase, currency);
        //            model.SubTotal = _priceFormatter.FormatPrice(itemSubTotalWithDiscount);

        //            itemSubTotalWithoutDiscountBase = _taxService.GetProductPrice(product, _priceCalculationService.GetSubTotal(cartItem, false), out taxRate);
        //        }

        //        var itemDiscountBase = itemSubTotalWithoutDiscountBase - itemSubTotalWithDiscountBase;

        //        if (itemDiscountBase > decimal.Zero)
        //        {
        //            var itemDiscount = _currencyService.ConvertFromPrimaryStoreCurrency(itemDiscountBase, currency);
        //            model.Discount = _priceFormatter.FormatPrice(itemDiscount);
        //        }

        //        var basePriceAdjustment = (_priceCalculationService.GetFinalPrice(product, true) - _priceCalculationService.GetUnitPrice(cartItem, true)) * (-1);

        //        model.BasePrice = product.GetBasePriceInfo(
        //            _localizationService,
        //            _priceFormatter,
        //            _currencyService,
        //            _taxService,
        //            _priceCalculationService,
        //            customer,
        //            currency,
        //            basePriceAdjustment
        //        );
        //    }

        //    if (item.BundleItem != null)
        //    {
        //        if (_shoppingCartSettings.ShowProductBundleImagesOnShoppingCart)
        //        {
        //            model.Picture = PrepareCartItemPictureModel(product, _mediaSettings.CartThumbBundleItemPictureSize, model.ProductName, item.AttributesXml);
        //        }
        //    }
        //    else
        //    {
        //        if (_shoppingCartSettings.ShowProductImagesOnShoppingCart)
        //        {
        //            model.Picture = PrepareCartItemPictureModel(product, _mediaSettings.CartThumbPictureSize, model.ProductName, item.AttributesXml);
        //        }
        //    }

        //    var itemWarnings = _shoppingCartService.GetShoppingCartItemWarnings(customer, item.ShoppingCartType, product, item.StoreId,
        //        item.AttributesXml, item.CustomerEnteredPrice, item.Quantity, false, bundleItem: item.BundleItem, childItems: cartItem.ChildItems);

        //    itemWarnings.Each(x => model.Warnings.Add(x));

        //    if (cartItem.ChildItems != null)
        //    {
        //        foreach (var childItem in cartItem.ChildItems.Where(x => x.Item.Id != item.Id))
        //        {
        //            var childModel = PrepareShoppingCartItemModel(childItem);
        //            model.ChildItems.Add(childModel);
        //        }
        //    }

        //    return model;
        //}

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
                file = await _db.ProductMediaFiles
                    .AsNoTracking()
                    .Include(x => x.MediaFile)
                    .ApplyProductFilter(new int[] { product.Id }, 1)
                    .Select(x => _mediaService.ConvertMediaFile(x.MediaFile))
                    .FirstOrDefaultAsync();
            }

            // Let's check whether this product has some parent "grouped" product.
            if (file == null && product.Visibility == ProductVisibility.Hidden && product.ParentGroupedProductId > 0)
            {
                file = await _db.ProductMediaFiles
                    .AsNoTracking()
                    .Include(x => x.MediaFile)
                    .ApplyProductFilter(new int[] { product.ParentGroupedProductId }, 1)
                    .Select(x => _mediaService.ConvertMediaFile(x.MediaFile))
                    .FirstOrDefaultAsync();
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

            model.SubTotal = (await _orderCalculationService.GetShoppingCartSubTotalAsync(cart)).SubTotalWithoutDiscount.ToString();

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
                            .ApplyProductFilter(new int[] { bundleItem.Item.ProductId }, 1)
                            .FirstOrDefaultAsync();

                        if (file != null)
                        {
                            bundleItemModel.PictureUrl = await _mediaService.GetUrlAsync(file.MediaFileId, MediaSettings.ThumbnailSizeXxs);
                        }

                        cartItemModel.BundleItems.Add(bundleItemModel);
                    }
                }

                // Unit prices.
                if (product.CallForPrice)
                {
                    cartItemModel.UnitPrice = await _localizationService.GetResourceAsync("Products.CallForPrice");
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

        public async Task<IActionResult> Cart(ProductVariantQuery query)
        {
            Guard.NotNull(query, nameof(query));

            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessShoppingCart))
                return RedirectToRoute("HomePage");

            var cart = await _shoppingCartService.GetCartItemsAsync(storeId: Services.StoreContext.CurrentStore.Id);

            // Allow to fill checkout attributes with values from query string.
            if (query.CheckoutAttributes.Any())
            {
                await ParseAndSaveCheckoutAttributesAsync(cart, query);
            }

            var model = await PrepareShoppingCartModelAsync(cart);

            HttpContext.Session.TrySetObject(CheckoutState.CheckoutStateSessionKey, new CheckoutState());

            return View(model);
        }

        public async Task<IActionResult> Wishlist(Guid? customerGuid)
        {
            if (!await Services.Permissions.AuthorizeAsync(Permissions.Cart.AccessWishlist))
                return RedirectToRoute("HomePage");

            var customer = customerGuid.HasValue
                ? await _db.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.CustomerGuid == customerGuid.Value)
                : Services.WorkContext.CurrentCustomer;

            if (customer == null)
            {
                return RedirectToRoute("HomePage");
            }

            var cart = await _shoppingCartService.GetCartItemsAsync(customer, ShoppingCartType.Wishlist, Services.StoreContext.CurrentStore.Id);

            var model = await PrepareWishlistModelAsync(cart, !customerGuid.HasValue);

            return View(model);
        }

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

            model.ThumbSize = _mediaSettings.MiniCartThumbPictureSize;

            return PartialView(model);
        }
    }
}

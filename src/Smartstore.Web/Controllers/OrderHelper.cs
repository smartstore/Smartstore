using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
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
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Utilities.Html;
using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Media;
using Smartstore.Web.Models.Orders;

namespace Smartstore.Web.Controllers
{
    public partial class OrderHelper
    {
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IMediaService _mediaService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPaymentService _paymentService;
        private readonly ICurrencyService _currencyService;
        private readonly IGiftCardService _giftCardService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly IEncryptor _encryptor;

        public OrderHelper(
            SmartDbContext db,
            ICommonServices services,
            IDateTimeHelper dateTimeHelper,
            IMediaService mediaService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IPaymentService paymentService,
            ICurrencyService currencyService,
            IGiftCardService giftCardService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ProductUrlHelper productUrlHelper,
            IEncryptor encryptor)
        {
            _db = db;
            _services = services;
            _dateTimeHelper = dateTimeHelper;
            _mediaService = mediaService;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _paymentService = paymentService;
            _currencyService = currencyService;
            _giftCardService = giftCardService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _productUrlHelper = productUrlHelper;
            _encryptor = encryptor;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public static string OrderDetailsPrintViewPath => "~/Views/Order/Details.Print.cshtml";

        private async Task<ImageModel> PrepareOrderItemImageModelAsync(
            Product product,
            int pictureSize,
            string productName,
            ProductVariantAttributeSelection attributeSelection,
            CatalogSettings catalogSettings)
        {
            Guard.NotNull(product, nameof(product));

            MediaFileInfo file = null;
            var combination = await _productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, attributeSelection);

            if (combination != null)
            {
                var mediaIds = combination.GetAssignedMediaIds();
                if (mediaIds.Any())
                {
                    file = await _mediaService.GetFileByIdAsync(mediaIds[0], MediaLoadFlags.AsNoTracking);
                }
            }

            // No attribute combination image, then load product picture.
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
                NoFallback = catalogSettings.HideProductDefaultPictures
            };
        }

        private async Task<OrderDetailsModel.OrderItemModel> PrepareOrderItemModelAsync(
            Order order, 
            OrderItem orderItem,
            CatalogSettings catalogSettings,
            ShoppingCartSettings shoppingCartSettings,
            MediaSettings mediaSettings,
            Currency customerCurrency)
        {
            var language = _services.WorkContext.WorkingLanguage;

            var attributeCombination = await _productAttributeMaterializer.FindAttributeCombinationAsync(orderItem.ProductId, orderItem.AttributeSelection);
            if (attributeCombination != null)
            {
                orderItem.Product.MergeWithCombination(attributeCombination);
            }
            
            var model = new OrderDetailsModel.OrderItemModel
            {
                Id = orderItem.Id,
                Sku = orderItem.Product.Sku,
                ProductId = orderItem.Product.Id,
                ProductName = orderItem.Product.GetLocalized(x => x.Name),
                ProductSeName = await orderItem.Product.GetActiveSlugAsync(),
                ProductType = orderItem.Product.ProductType,
                Quantity = orderItem.Quantity,
                AttributeInfo = orderItem.AttributeDescription
            };

            var quantityUnit = await _db.QuantityUnits.FindByIdAsync(orderItem.Product.QuantityUnitId ?? 0, false);
            model.QuantityUnit = quantityUnit == null ? string.Empty : quantityUnit.GetLocalized(x => x.Name);

            if (orderItem.Product.ProductType == ProductType.BundledProduct && orderItem.BundleData.HasValue())
            {
                var bundleData = orderItem.GetBundleData();

                var bundleProducts = await _db.ProductBundleItem
                    .AsNoTracking()
                    .Include(x => x.Product)
                    .ApplyBundledProductsFilter(new[] { orderItem.ProductId })
                    .ToListAsync();

                var bundleItems = shoppingCartSettings.ShowProductBundleImagesOnShoppingCart
                    ? bundleProducts.ToDictionarySafe(x => x.ProductId)
                    : new Dictionary<int, ProductBundleItem>();

                model.BundlePerItemPricing = orderItem.Product.BundlePerItemPricing;
                model.BundlePerItemShoppingCart = bundleData.Any(x => x.PerItemShoppingCart);

                foreach (var bid in bundleData)
                {
                    var bundleItemModel = new OrderDetailsModel.BundleItemModel
                    {
                        Sku = bid.Sku,
                        ProductName = bid.ProductName,
                        ProductSeName = bid.ProductSeName,
                        VisibleIndividually = bid.VisibleIndividually,
                        Quantity = bid.Quantity,
                        DisplayOrder = bid.DisplayOrder,
                        AttributeInfo = bid.AttributesInfo
                    };

                    bundleItemModel.ProductUrl = await _productUrlHelper.GetProductUrlAsync(bid.ProductId, bundleItemModel.ProductSeName, bid.AttributeSelection);

                    if (model.BundlePerItemShoppingCart)
                    {
                        var priceWithDiscount = _currencyService.ConvertFromPrimaryCurrency(bid.PriceWithDiscount, customerCurrency);
                        //bundleItemModel.PriceWithDiscount = _priceFormatter.FormatPrice(priceWithDiscount, true, order.CustomerCurrencyCode, language, false, false);
                        bundleItemModel.PriceWithDiscount = priceWithDiscount.ToString();
                    }

                    // Bundle item picture.
                    if (shoppingCartSettings.ShowProductBundleImagesOnShoppingCart && bundleItems.TryGetValue(bid.ProductId, out var bundleItem))
                    {
                        bundleItemModel.HideThumbnail = bundleItem.HideThumbnail;

                        bundleItemModel.Image = await PrepareOrderItemImageModelAsync(
                            bundleItem.Product,
                            mediaSettings.CartThumbBundleItemPictureSize,
                            bid.ProductName,
                            bid.AttributeSelection,
                            catalogSettings);
                    }

                    model.BundleItems.Add(bundleItemModel);
                }
            }

            // Unit price, subtotal.
            switch (order.CustomerTaxDisplayType)
            {
                case TaxDisplayType.ExcludingTax:
                    {
                        var unitPriceExclTaxInCustomerCurrency = _currencyService.ConvertFromPrimaryCurrency(orderItem.UnitPriceExclTax, customerCurrency);
                        //model.UnitPrice = _priceFormatter.FormatPrice(unitPriceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, language, false, false);
                        model.UnitPrice = unitPriceExclTaxInCustomerCurrency.ToString();

                        var priceExclTaxInCustomerCurrency = _currencyService.ConvertFromPrimaryCurrency(orderItem.PriceExclTax, customerCurrency);
                        //model.SubTotal = _priceFormatter.FormatPrice(priceExclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, language, false, false);
                        model.SubTotal = priceExclTaxInCustomerCurrency.ToString();
                    }
                    break;

                case TaxDisplayType.IncludingTax:
                    {
                        var unitPriceInclTaxInCustomerCurrency = _currencyService.ConvertFromPrimaryCurrency(orderItem.UnitPriceInclTax, customerCurrency);
                        //model.UnitPrice = _priceFormatter.FormatPrice(unitPriceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, language, true, false);
                        model.UnitPrice = unitPriceInclTaxInCustomerCurrency.ToString();

                        var priceInclTaxInCustomerCurrency = _currencyService.ConvertFromPrimaryCurrency(orderItem.PriceInclTax, customerCurrency);
                        //model.SubTotal = _priceFormatter.FormatPrice(priceInclTaxInCustomerCurrency, true, order.CustomerCurrencyCode, language, true, false);
                        model.SubTotal = priceInclTaxInCustomerCurrency.ToString();
                    }
                    break;
            }

            model.ProductUrl = await _productUrlHelper.GetProductUrlAsync(orderItem.ProductId, model.ProductSeName, orderItem.AttributeSelection);

            if (shoppingCartSettings.ShowProductImagesOnShoppingCart)
            {
                model.Image = await PrepareOrderItemImageModelAsync(
                    orderItem.Product,
                    mediaSettings.CartThumbPictureSize,
                    model.ProductName,
                    orderItem.AttributeSelection,
                    catalogSettings);
            }

            return model;
        }

        public async Task<OrderDetailsModel> PrepareOrderDetailsModelAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            var settingFactory = _services.SettingFactory;
            var store = await _db.Stores.FindByIdAsync(order.StoreId, false) ?? _services.StoreContext.CurrentStore;
            var language = _services.WorkContext.WorkingLanguage;

            var orderSettings = await settingFactory.LoadSettingsAsync<OrderSettings>(store.Id);
            var catalogSettings = await settingFactory.LoadSettingsAsync<CatalogSettings>(store.Id);
            var taxSettings = await settingFactory.LoadSettingsAsync<TaxSettings>(store.Id);
            var pdfSettings = await settingFactory.LoadSettingsAsync<PdfSettings>(store.Id);
            var addressSettings = await settingFactory.LoadSettingsAsync<AddressSettings>(store.Id);
            var companyInfoSettings = await settingFactory.LoadSettingsAsync<CompanyInformationSettings>(store.Id);
            var shoppingCartSettings = await settingFactory.LoadSettingsAsync<ShoppingCartSettings>(store.Id);
            var mediaSettings = await settingFactory.LoadSettingsAsync<MediaSettings>(store.Id);

            var model = new OrderDetailsModel
            {
                Id = order.Id,
                StoreId = order.StoreId,
                CustomerLanguageId = order.CustomerLanguageId,
                CustomerComment = order.CustomerOrderComment,
                OrderNumber = order.GetOrderNumber(),
                CreatedOn = _dateTimeHelper.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc),
                OrderStatus = await _services.Localization.GetLocalizedEnumAsync(order.OrderStatus),
                IsReOrderAllowed = orderSettings.IsReOrderAllowed,
                IsReturnRequestAllowed = _orderProcessingService.IsReturnRequestAllowed(order),
                DisplayPdfInvoice = pdfSettings.Enabled,
                RenderOrderNotes = pdfSettings.RenderOrderNotes,
                // Shipping info.
                ShippingStatus = await _services.Localization.GetLocalizedEnumAsync(order.ShippingStatus)
            };

            // TODO: refactor modelling for multi-order processing.
            var companyCountry = await _db.Countries.FindByIdAsync(companyInfoSettings.CountryId, false);
            model.MerchantCompanyInfo = companyInfoSettings;
            model.MerchantCompanyCountryName = companyCountry?.GetLocalized(x => x.Name);

            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                model.IsShippable = true;
                await MapperFactory.MapAsync(order.ShippingAddress, model.ShippingAddress);
                model.ShippingMethod = order.ShippingMethod;

                // Shipments (only already shipped).
                var shipments = order.Shipments.Where(x => x.ShippedDateUtc.HasValue).OrderBy(x => x.CreatedOnUtc).ToList();
                foreach (var shipment in shipments)
                {
                    var shipmentModel = new OrderDetailsModel.ShipmentBriefModel
                    {
                        Id = shipment.Id,
                        TrackingNumber = shipment.TrackingNumber,
                    };

                    if (shipment.ShippedDateUtc.HasValue)
                    {
                        shipmentModel.ShippedDate = _dateTimeHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc);
                    }
                    if (shipment.DeliveryDateUtc.HasValue)
                    {
                        shipmentModel.DeliveryDate = _dateTimeHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc);
                    }

                    model.Shipments.Add(shipmentModel);
                }
            }

            await MapperFactory.MapAsync(order.BillingAddress, model.BillingAddress);
            model.VatNumber = order.VatNumber;

            // Payment method.
            var paymentMethod = await _paymentService.LoadPaymentMethodBySystemNameAsync(order.PaymentMethodSystemName);
            model.PaymentMethodSystemName = order.PaymentMethodSystemName;
            // TODO: (mh) (core) 
            //model.PaymentMethod = paymentMethod != null ? _pluginMediator.GetLocalizedFriendlyName(paymentMethod.Metadata) : order.PaymentMethodSystemName;
            model.PaymentMethod = order.PaymentMethodSystemName;
            model.CanRePostProcessPayment = await _paymentService.CanRePostProcessPaymentAsync(order);

            // Purchase order number (we have to find a better to inject this information because it's related to a certain plugin).
            if (paymentMethod != null && paymentMethod.Metadata.SystemName.Equals("Smartstore.PurchaseOrderNumber", StringComparison.InvariantCultureIgnoreCase))
            {
                model.DisplayPurchaseOrderNumber = true;
                model.PurchaseOrderNumber = order.PurchaseOrderNumber;
            }

            if (order.AllowStoringCreditCardNumber)
            {
                model.CardNumber = _encryptor.DecryptText(order.CardNumber);
                model.MaskedCreditCardNumber = _encryptor.DecryptText(order.MaskedCreditCardNumber);
                model.CardCvv2 = _encryptor.DecryptText(order.CardCvv2);
                model.CardExpirationMonth = _encryptor.DecryptText(order.CardExpirationMonth);
                model.CardExpirationYear = _encryptor.DecryptText(order.CardExpirationYear);
            }

            if (order.AllowStoringDirectDebit)
            {
                model.DirectDebitAccountHolder = _encryptor.DecryptText(order.DirectDebitAccountHolder);
                model.DirectDebitAccountNumber = _encryptor.DecryptText(order.DirectDebitAccountNumber);
                model.DirectDebitBankCode = _encryptor.DecryptText(order.DirectDebitBankCode);
                model.DirectDebitBankName = _encryptor.DecryptText(order.DirectDebitBankName);
                model.DirectDebitBIC = _encryptor.DecryptText(order.DirectDebitBIC);
                model.DirectDebitCountry = _encryptor.DecryptText(order.DirectDebitCountry);
                model.DirectDebitIban = _encryptor.DecryptText(order.DirectDebitIban);
            }

            // TODO: (mh) (core) Reimplement when pricing is ready.
            // Totals.
            var customerCurrency = await _db.Currencies
                .AsNoTracking()
                .Where(x => x.CurrencyCode == order.CustomerCurrencyCode)
                .FirstOrDefaultAsync();

            switch (order.CustomerTaxDisplayType)
            {
                case TaxDisplayType.ExcludingTax:
                    {
                        // Order subtotal.
                        var orderSubtotalExclTax = _currencyService.ConvertFromPrimaryCurrency(order.OrderSubtotalExclTax, customerCurrency);
                        //model.OrderSubtotal = _priceFormatter.FormatPrice(orderSubtotalExclTax, true, order.CustomerCurrencyCode, language, false, false);
                        model.OrderSubtotal = orderSubtotalExclTax.ToString();

                        // Discount (applied to order subtotal).
                        var orderSubTotalDiscountExclTax = _currencyService.ConvertFromPrimaryCurrency(order.OrderSubTotalDiscountExclTax, customerCurrency);
                        if (orderSubTotalDiscountExclTax > decimal.Zero)
                        {
                            //model.OrderSubTotalDiscount = _priceFormatter.FormatPrice(-orderSubTotalDiscountExclTax, true, order.CustomerCurrencyCode, language, false, false);
                            model.OrderSubTotalDiscount = (orderSubTotalDiscountExclTax * -1).ToString();
                        }

                        // Order shipping.
                        var orderShippingExclTax = _currencyService.ConvertFromPrimaryCurrency(order.OrderShippingExclTax, customerCurrency);
                        //model.OrderShipping = _priceFormatter.FormatShippingPrice(orderShippingExclTax, true, order.CustomerCurrencyCode, language, false, false);
                        model.OrderShipping = orderShippingExclTax.ToString();

                        // Payment method additional fee.
                        var paymentMethodAdditionalFeeExclTax = _currencyService.ConvertFromPrimaryCurrency(order.PaymentMethodAdditionalFeeExclTax, customerCurrency);
                        if (paymentMethodAdditionalFeeExclTax != decimal.Zero)
                        {
                            //model.PaymentMethodAdditionalFee = _priceFormatter.FormatPaymentMethodAdditionalFee(paymentMethodAdditionalFeeExclTax, true, order.CustomerCurrencyCode,
                            //    language, false, false);
                            model.PaymentMethodAdditionalFee = paymentMethodAdditionalFeeExclTax.ToString();
                        }
                    }
                    break;

                case TaxDisplayType.IncludingTax:
                    {
                        // Order subtotal.
                        var orderSubtotalInclTax = _currencyService.ConvertFromPrimaryCurrency(order.OrderSubtotalInclTax, customerCurrency);
                        //model.OrderSubtotal = _priceFormatter.FormatPrice(orderSubtotalInclTax, true, order.CustomerCurrencyCode, language, true, false);
                        model.OrderSubtotal = orderSubtotalInclTax.ToString();

                        // Discount (applied to order subtotal).
                        var orderSubTotalDiscountInclTax = _currencyService.ConvertFromPrimaryCurrency(order.OrderSubTotalDiscountInclTax, customerCurrency);
                        if (orderSubTotalDiscountInclTax > decimal.Zero)
                        {
                            //model.OrderSubTotalDiscount = _priceFormatter.FormatPrice(-orderSubTotalDiscountInclTax, true, order.CustomerCurrencyCode, language, true, false);
                            model.OrderSubTotalDiscount = (orderSubTotalDiscountInclTax * -1).ToString();
                        }

                        // Order shipping.
                        var orderShippingInclTax = _currencyService.ConvertFromPrimaryCurrency(order.OrderShippingInclTax, customerCurrency);
                        //model.OrderShipping = _priceFormatter.FormatShippingPrice(orderShippingInclTax, true, order.CustomerCurrencyCode, language, true, false);
                        model.OrderShipping = orderShippingInclTax.ToString();

                        // Payment method additional fee.
                        var paymentMethodAdditionalFeeInclTax = _currencyService.ConvertFromPrimaryCurrency(order.PaymentMethodAdditionalFeeInclTax, customerCurrency);
                        if (paymentMethodAdditionalFeeInclTax != decimal.Zero)
                        {
                            //model.PaymentMethodAdditionalFee = _priceFormatter.FormatPaymentMethodAdditionalFee(paymentMethodAdditionalFeeInclTax, true, order.CustomerCurrencyCode,
                            //    language, true, false);
                            model.PaymentMethodAdditionalFee = paymentMethodAdditionalFeeInclTax.ToString();
                        }
                    }
                    break;
            }

            // Tax.
            var displayTax = true;
            var displayTaxRates = true;

            if (taxSettings.HideTaxInOrderSummary && order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
            {
                displayTax = false;
                displayTaxRates = false;
            }
            else
            {
                if (order.OrderTax == 0 && taxSettings.HideZeroTax)
                {
                    displayTax = false;
                    displayTaxRates = false;
                }
                else
                {
                    displayTaxRates = taxSettings.DisplayTaxRates && order.TaxRatesDictionary.Count > 0;
                    displayTax = !displayTaxRates;

                    // TODO: (mh) (core) Check again when pricing is ready.
                    var orderTaxInCustomerCurrency = _currencyService.ConvertFromPrimaryCurrency(order.OrderTax, customerCurrency);
                    //model.Tax = _priceFormatter.FormatPrice(orderTaxInCustomerCurrency, true, order.CustomerCurrencyCode, false, language);
                    model.Tax = orderTaxInCustomerCurrency.ToString();

                    foreach (var tr in order.TaxRatesDictionary)
                    {
                        //var rate = _priceFormatter.FormatTaxRate(tr.Key);
                        var rate = tr.Key.ToString("G29");

                        //var labelKey = "ShoppingCart.Totals.TaxRateLine" + (_services.WorkContext.TaxDisplayType == TaxDisplayType.IncludingTax ? "Incl" : "Excl");
                        var labelKey = _services.WorkContext.TaxDisplayType == TaxDisplayType.IncludingTax ? "ShoppingCart.Totals.TaxRateLineIncl" : "ShoppingCart.Totals.TaxRateLineExcl";

                        model.TaxRates.Add(new OrderDetailsModel.TaxRate
                        {
                            Rate = rate,
                            Label = T(labelKey, rate),
                            //Value = _priceFormatter.FormatPrice(_currencyService.ConvertCurrency(tr.Value, order.CurrencyRate), true, order.CustomerCurrencyCode, false, language),
                            Value = _currencyService.ConvertFromPrimaryCurrency(tr.Value, customerCurrency).ToString()
                        });
                    }
                }
            }

            model.DisplayTaxRates = displayTaxRates;
            model.DisplayTax = displayTax;

            // Discount (applied to order total).
            var orderDiscountInCustomerCurrency = _currencyService.ConvertFromPrimaryCurrency(order.OrderDiscount, customerCurrency);
            if (orderDiscountInCustomerCurrency > decimal.Zero)
            {
                //model.OrderTotalDiscount = _priceFormatter.FormatPrice(-orderDiscountInCustomerCurrency, true, order.CustomerCurrencyCode, false, language);
                model.OrderTotalDiscount = (orderDiscountInCustomerCurrency * -1).ToString();
            }

            // Gift cards.
            foreach (var gcuh in order.GiftCardUsageHistory)
            {
                var remainingAmountBase = _giftCardService.GetRemainingAmount(gcuh.GiftCard);
                var remainingAmount = _currencyService.ConvertFromPrimaryCurrency(remainingAmountBase.Amount, customerCurrency);

                var gcModel = new OrderDetailsModel.GiftCard
                {
                    CouponCode = gcuh.GiftCard.GiftCardCouponCode,
                    //Amount = _priceFormatter.FormatPrice(-(_currencyService.ConvertCurrency(gcuh.UsedValue, order.CurrencyRate)), true, order.CustomerCurrencyCode, false, language),
                    //Remaining = _priceFormatter.FormatPrice(remainingAmount, true, false)
                    Amount = (_currencyService.ConvertFromPrimaryCurrency(gcuh.UsedValue, customerCurrency) * -1).ToString(),
                    Remaining = remainingAmount.ToString()
                };

                model.GiftCards.Add(gcModel);
            }

            // Reward points         .  
            if (order.RedeemedRewardPointsEntry != null)
            {
                model.RedeemedRewardPoints = -order.RedeemedRewardPointsEntry.Points;
                //model.RedeemedRewardPointsAmount = _priceFormatter.FormatPrice(-(_currencyService.ConvertCurrency(order.RedeemedRewardPointsEntry.UsedAmount, order.CurrencyRate)),
                //    true, order.CustomerCurrencyCode, false, language);
                model.RedeemedRewardPointsAmount = (_currencyService.ConvertFromPrimaryCurrency(order.RedeemedRewardPointsEntry.UsedAmount, customerCurrency) * -1).ToString();
            }

            // Credit balance.
            if (order.CreditBalance > decimal.Zero)
            {
                var convertedCreditBalance = _currencyService.ConvertFromPrimaryCurrency(order.CreditBalance, customerCurrency);
                //model.CreditBalance = _priceFormatter.FormatPrice(-convertedCreditBalance, true, order.CustomerCurrencyCode, false, language);
                model.CreditBalance = (convertedCreditBalance * -1).ToString();
            }

            // Total.
            (var orderTotal, var roundingAmount) = await _orderService.GetOrderTotalInCustomerCurrencyAsync(order);
            //model.OrderTotal = _priceFormatter.FormatPrice(orderTotal, true, order.CustomerCurrencyCode, false, language);
            model.OrderTotal = orderTotal.ToString();

            if (roundingAmount.Amount != decimal.Zero)
            {
                //model.OrderTotalRounding = _priceFormatter.FormatPrice(roundingAmount, true, order.CustomerCurrencyCode, false, language);
                model.OrderTotalRounding = roundingAmount.ToString();
            }

            // Checkout attributes.
            model.CheckoutAttributeInfo = HtmlUtils.ConvertPlainTextToTable(HtmlUtils.ConvertHtmlToPlainText(order.CheckoutAttributeDescription));

            // Order notes.
            foreach (var orderNote in order.OrderNotes
                .Where(on => on.DisplayToCustomer)
                .OrderByDescending(on => on.CreatedOnUtc)
                .ToList())
            {
                var createdOn = _dateTimeHelper.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc);

                model.OrderNotes.Add(new OrderDetailsModel.OrderNote
                {
                    Note = orderNote.FormatOrderNoteText(),
                    CreatedOn = createdOn,
                    FriendlyCreatedOn = createdOn.Humanize()
                });
            }

            // Purchased products.
            model.ShowSku = catalogSettings.ShowProductSku;
            model.ShowProductImages = shoppingCartSettings.ShowProductImagesOnShoppingCart;
            model.ShowProductBundleImages = shoppingCartSettings.ShowProductBundleImagesOnShoppingCart;
            model.BundleThumbSize = mediaSettings.CartThumbBundleItemPictureSize;

            var orderItems = await _db.OrderItems
                .AsNoTracking()
                .Include(x => x.Product)
                .ApplyStandardFilter(order.Id)
                .ToListAsync();

            foreach (var orderItem in orderItems)
            {
                var orderItemModel = await PrepareOrderItemModelAsync(order, orderItem, catalogSettings, shoppingCartSettings, mediaSettings, customerCurrency);
                model.Items.Add(orderItemModel);
            }

            return model;
        }
    }
}
 
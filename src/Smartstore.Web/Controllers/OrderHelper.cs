using Humanizer;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities.Html;
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
        private readonly ITaxService _taxService;
        private readonly IGiftCardService _giftCardService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly IEncryptor _encryptor;
        private readonly Lazy<ModuleManager> _moduleManager;

        public OrderHelper(
            SmartDbContext db,
            ICommonServices services,
            IDateTimeHelper dateTimeHelper,
            IMediaService mediaService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IPaymentService paymentService,
            ICurrencyService currencyService,
            ITaxService taxService,
            IGiftCardService giftCardService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ProductUrlHelper productUrlHelper,
            IEncryptor encryptor,
            Lazy<ModuleManager> moduleManager)
        {
            _db = db;
            _services = services;
            _dateTimeHelper = dateTimeHelper;
            _mediaService = mediaService;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _paymentService = paymentService;
            _currencyService = currencyService;
            _taxService = taxService;
            _giftCardService = giftCardService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _productUrlHelper = productUrlHelper;
            _encryptor = encryptor;
            _moduleManager = moduleManager;
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
                var bundleItems = new Dictionary<int, ProductBundleItem>();

                if (shoppingCartSettings.ShowProductBundleImagesOnShoppingCart)
                {
                    var bundleProducts = await _db.ProductBundleItem
                        .AsNoTracking()
                        .Include(x => x.Product)
                        .ApplyBundledProductsFilter(new[] { orderItem.ProductId })
                        .ToListAsync();

                    bundleItems = bundleProducts.ToDictionarySafe(x => x.ProductId);
                }

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
                        bundleItemModel.PriceWithDiscount = _currencyService.ConvertToExchangeRate(bid.PriceWithDiscount, order.CurrencyRate, customerCurrency);
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
                    var unitPriceExclTaxInCustomerCurrency = _currencyService.ConvertToExchangeRate(orderItem.UnitPriceExclTax, order.CurrencyRate, customerCurrency);
                    model.UnitPrice = unitPriceExclTaxInCustomerCurrency;
                    model.SubTotal = _currencyService.ConvertToExchangeRate(orderItem.PriceExclTax, order.CurrencyRate, customerCurrency);
                }
                break;

                case TaxDisplayType.IncludingTax:
                {
                    var unitPriceInclTaxInCustomerCurrency = _currencyService.ConvertToExchangeRate(orderItem.UnitPriceInclTax, order.CurrencyRate, customerCurrency);
                    model.UnitPrice = unitPriceInclTaxInCustomerCurrency;
                    model.SubTotal = _currencyService.ConvertToExchangeRate(orderItem.PriceInclTax, order.CurrencyRate, customerCurrency);
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
            var companyInfoSettings = await settingFactory.LoadSettingsAsync<CompanyInformationSettings>(store.Id);
            var shoppingCartSettings = await settingFactory.LoadSettingsAsync<ShoppingCartSettings>(store.Id);
            var mediaSettings = await settingFactory.LoadSettingsAsync<MediaSettings>(store.Id);

            var model = new OrderDetailsModel
            {
                Order = order,
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
                await _db.LoadCollectionAsync(order, x => x.Shipments);

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
            model.PaymentMethod = paymentMethod != null ? _moduleManager.Value.GetLocalizedFriendlyName(paymentMethod.Metadata) : order.PaymentMethodSystemName;
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
                    model.OrderSubtotal = _currencyService.ConvertToExchangeRate(order.OrderSubtotalExclTax, order.CurrencyRate, customerCurrency);

                    // Discount (applied to order subtotal).
                    var orderSubTotalDiscountExclTax = _currencyService.ConvertToExchangeRate(order.OrderSubTotalDiscountExclTax, order.CurrencyRate, customerCurrency);
                    if (orderSubTotalDiscountExclTax > 0)
                    {
                        model.OrderSubTotalDiscount = orderSubTotalDiscountExclTax * -1;
                    }

                    // Order shipping.
                    model.OrderShipping = _currencyService.ConvertToExchangeRate(order.OrderShippingExclTax, order.CurrencyRate, customerCurrency);

                    // Payment method additional fee.
                    var paymentMethodAdditionalFeeExclTax = _currencyService.ConvertToExchangeRate(order.PaymentMethodAdditionalFeeExclTax, order.CurrencyRate, customerCurrency);
                    if (paymentMethodAdditionalFeeExclTax != 0)
                    {
                        model.PaymentMethodAdditionalFee = paymentMethodAdditionalFeeExclTax;
                    }
                }
                break;

                case TaxDisplayType.IncludingTax:
                {
                    // Order subtotal.
                    model.OrderSubtotal = _currencyService.ConvertToExchangeRate(order.OrderSubtotalInclTax, order.CurrencyRate, customerCurrency);

                    // Discount (applied to order subtotal).
                    var orderSubTotalDiscountInclTax = _currencyService.ConvertToExchangeRate(order.OrderSubTotalDiscountInclTax, order.CurrencyRate, customerCurrency);
                    if (orderSubTotalDiscountInclTax > 0)
                    {
                        model.OrderSubTotalDiscount = orderSubTotalDiscountInclTax * -1;
                    }

                    // Order shipping.
                    model.OrderShipping = _currencyService.ConvertToExchangeRate(order.OrderShippingInclTax, order.CurrencyRate, customerCurrency);

                    // Payment method additional fee.
                    var paymentMethodAdditionalFeeInclTax = _currencyService.ConvertToExchangeRate(order.PaymentMethodAdditionalFeeInclTax, order.CurrencyRate, customerCurrency);
                    if (paymentMethodAdditionalFeeInclTax != 0)
                    {
                        model.PaymentMethodAdditionalFee = paymentMethodAdditionalFeeInclTax;
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

                    model.Tax = _currencyService.ConvertToExchangeRate(order.OrderTax, order.CurrencyRate, customerCurrency);

                    foreach (var tr in order.TaxRatesDictionary)
                    {
                        var rate = _taxService.FormatTaxRate(tr.Key);
                        var labelKey = _services.WorkContext.TaxDisplayType == TaxDisplayType.IncludingTax ? "ShoppingCart.Totals.TaxRateLineIncl" : "ShoppingCart.Totals.TaxRateLineExcl";

                        model.TaxRates.Add(new OrderDetailsModel.TaxRate
                        {
                            Rate = rate,
                            Label = T(labelKey, rate),
                            Value = _currencyService.ConvertToExchangeRate(tr.Value, order.CurrencyRate, customerCurrency).ToString()
                        });
                    }
                }
            }

            model.DisplayTaxRates = displayTaxRates;
            model.DisplayTax = displayTax;

            // Discount (applied to order total).
            var orderDiscountInCustomerCurrency = _currencyService.ConvertToExchangeRate(order.OrderDiscount, order.CurrencyRate, customerCurrency);
            if (orderDiscountInCustomerCurrency > 0)
            {
                model.OrderTotalDiscount = orderDiscountInCustomerCurrency * -1;
            }

            // Gift cards.
            await _db.LoadCollectionAsync(order, x => x.GiftCardUsageHistory, false, q => q.Include(x => x.GiftCard));

            foreach (var gcuh in order.GiftCardUsageHistory)
            {
                var remainingAmountBase = await _giftCardService.GetRemainingAmountAsync(gcuh.GiftCard);
                var remainingAmount = _currencyService.ConvertToExchangeRate(remainingAmountBase.Amount, order.CurrencyRate, customerCurrency);
                var usedAmount = _currencyService.ConvertToExchangeRate(gcuh.UsedValue, order.CurrencyRate, customerCurrency);

                var gcModel = new OrderDetailsModel.GiftCard
                {
                    CouponCode = gcuh.GiftCard.GiftCardCouponCode,
                    Amount = (usedAmount * -1).ToString(),
                    Remaining = remainingAmount.ToString()
                };

                model.GiftCards.Add(gcModel);
            }

            // Reward points.
            await _db.LoadReferenceAsync(order, x => x.RedeemedRewardPointsEntry);

            if (order.RedeemedRewardPointsEntry != null)
            {
                var usedAmount = _currencyService.ConvertToExchangeRate(order.RedeemedRewardPointsEntry.UsedAmount, order.CurrencyRate, customerCurrency);

                model.RedeemedRewardPoints = -order.RedeemedRewardPointsEntry.Points;
                model.RedeemedRewardPointsAmount = usedAmount * -1;
            }

            // Credit balance.
            if (order.CreditBalance > 0)
            {
                var convertedCreditBalance = _currencyService.ConvertToExchangeRate(order.CreditBalance, order.CurrencyRate, customerCurrency);
                model.CreditBalance = convertedCreditBalance * -1;
            }

            // Total.
            (var orderTotal, var roundingAmount) = await _orderService.GetOrderTotalInCustomerCurrencyAsync(order, customerCurrency);
            model.OrderTotal = orderTotal;

            if (roundingAmount != 0)
            {
                model.OrderTotalRounding = roundingAmount;
            }

            // Checkout attributes.
            model.CheckoutAttributeInfo = HtmlUtility.ConvertPlainTextToTable(HtmlUtility.ConvertHtmlToPlainText(order.CheckoutAttributeDescription));

            // Order notes.
            await _db.LoadCollectionAsync(order, x => x.OrderNotes);

            var orderNotes = order.OrderNotes
                .Where(x => x.DisplayToCustomer)
                .OrderByDescending(x => x.CreatedOnUtc)
                .ToList();

            foreach (var orderNote in orderNotes)
            {
                var createdOn = _dateTimeHelper.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc);

                model.OrderNotes.Add(new OrderDetailsModel.OrderNote
                {
                    Note = orderNote.FormatOrderNoteText(),
                    CreatedOn = createdOn,
                    FriendlyCreatedOn = createdOn.Humanize(false)
                });
            }

            // Purchased products.
            model.ShowSku = catalogSettings.ShowProductSku;
            model.ShowProductImages = shoppingCartSettings.ShowProductImagesOnShoppingCart;
            model.ShowProductBundleImages = shoppingCartSettings.ShowProductBundleImagesOnShoppingCart;
            model.BundleThumbSize = mediaSettings.CartThumbBundleItemPictureSize;

            await _db.LoadCollectionAsync(order, x => x.OrderItems, false, q => q.Include(x => x.Product));

            foreach (var orderItem in order.OrderItems)
            {
                var orderItemModel = await PrepareOrderItemModelAsync(order, orderItem, catalogSettings, shoppingCartSettings, mediaSettings, customerCurrency);
                model.Items.Add(orderItemModel);
            }

            return model;
        }
    }
}

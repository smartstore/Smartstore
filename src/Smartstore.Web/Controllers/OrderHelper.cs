#nullable enable

using Humanizer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
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
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Engine.Modularity;
using Smartstore.IO;
using Smartstore.Pdf;
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
        private readonly IMediaService _mediaService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IPaymentService _paymentService;
        private readonly ITaxService _taxService;
        private readonly IGiftCardService _giftCardService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly ProductUrlHelper _productUrlHelper;
        private readonly IUrlHelper _urlHelper;
        private readonly IEncryptor _encryptor;
        private readonly Lazy<ModuleManager> _moduleManager;
        private readonly IViewInvoker _viewInvoker;
        private readonly IPdfConverter _pdfConverter;

        public OrderHelper(
            SmartDbContext db,
            ICommonServices services,
            IMediaService mediaService,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IPaymentService paymentService,
            ITaxService taxService,
            IGiftCardService giftCardService,
            IProductAttributeMaterializer productAttributeMaterializer,
            ProductUrlHelper productUrlHelper,
            IUrlHelper urlHelper,
            IEncryptor encryptor,
            Lazy<ModuleManager> moduleManager,
            IViewInvoker viewInvoker,
            IPdfConverter pdfConverter)
        {
            _db = db;
            _services = services;
            _mediaService = mediaService;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _paymentService = paymentService;
            _taxService = taxService;
            _giftCardService = giftCardService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _productUrlHelper = productUrlHelper;
            _urlHelper = urlHelper;
            _encryptor = encryptor;
            _moduleManager = moduleManager;
            _viewInvoker = viewInvoker;
            _pdfConverter = pdfConverter;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public static string OrderDetailsPrintViewPath => "/{theme}/Views/Order/Details.Print.cshtml";

        private async Task<ImageModel> PrepareOrderItemImageModelAsync(
            Product product,
            int pictureSize,
            string productName,
            ProductVariantAttributeSelection attributeSelection,
            CatalogSettings catalogSettings)
        {
            Guard.NotNull(product);

            var file = (MediaFileInfo?)null;
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

            return new ImageModel(file, pictureSize)
            {
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
            Currency? customerCurrency)
        {
            var attributeCombination = await _productAttributeMaterializer.FindAttributeCombinationAsync(orderItem.ProductId, orderItem.AttributeSelection);
            if (attributeCombination != null)
            {
                orderItem.Product.MergeWithCombination(attributeCombination);
            }

            var model = new OrderDetailsModel.OrderItemModel
            {
                Id = orderItem.Id,
                Sku = orderItem.Sku.NullEmpty() ?? orderItem.Product.Sku,
                ProductId = orderItem.Product.Id,
                ProductName = orderItem.Product.GetLocalized(x => x.Name),
                ProductSeName = await orderItem.Product.GetActiveSlugAsync(),
                ProductType = orderItem.Product.ProductType,
                Quantity = orderItem.Quantity,
                AttributeInfo = HtmlUtility.FormatPlainText(HtmlUtility.ConvertHtmlToPlainText(orderItem.AttributeDescription))
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
                        AttributeInfo = HtmlUtility.FormatPlainText(HtmlUtility.ConvertHtmlToPlainText(bid.AttributesInfo))
                    };

                    bundleItemModel.ProductUrl = await _productUrlHelper.GetProductPathAsync(bid.ProductId, bundleItemModel.ProductSeName, bid.AttributeSelection);

                    if (model.BundlePerItemShoppingCart)
                    {
                        bundleItemModel.PriceWithDiscount = ConvertToExchangeRate(bid.PriceWithDiscount);
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
                    model.UnitPrice = ConvertToExchangeRate(orderItem.UnitPriceExclTax);
                    model.SubTotal = ConvertToExchangeRate(orderItem.PriceExclTax);
                }
                break;

                case TaxDisplayType.IncludingTax:
                {
                    model.UnitPrice = ConvertToExchangeRate(orderItem.UnitPriceInclTax);
                    model.SubTotal = ConvertToExchangeRate(orderItem.PriceInclTax);
                }
                break;
            }

            model.ProductUrl = await _productUrlHelper.GetProductPathAsync(orderItem.ProductId, model.ProductSeName, orderItem.AttributeSelection);

            if (shoppingCartSettings.ShowProductImagesOnShoppingCart)
            {
                model.Image = await PrepareOrderItemImageModelAsync(
                    orderItem.Product,
                    mediaSettings.CartThumbPictureSize,
                    model.ProductName!,
                    orderItem.AttributeSelection,
                    catalogSettings);
            }

            // Custom mapping
            await MapperFactory.MapWithRegisteredMapperAsync(orderItem, model, new { Order = order, Currency = customerCurrency });

            return model;

            Money ConvertToExchangeRate(decimal amount)
            {
                return _services.CurrencyService.ConvertToExchangeRate(amount, order.CurrencyRate, customerCurrency);
            }
        }

        public Task<OrderDetailsModel> PrepareOrderDetailsModelAsync(Order order)
        {
            return PrepareOrderDetailsModelInternal(order);
        }

        public async Task<List<OrderDetailsModel>> PrepareOrderDetailsModelsAsync(IEnumerable<Order> orders)
        {
            var context = await CreateOrderHelperContext();

            var models = await orders
                .SelectAwait(o => PrepareOrderDetailsModelInternal(o, context))
                .AsyncToList();

            return models;
        }

        private async Task<OrderDetailsModel> PrepareOrderDetailsModelInternal(Order o, OrderHelperContext? context = null)
        {
            Guard.NotNull(o);

            var dtHelper = _services.DateTimeHelper;
            var settingFactory = _services.SettingFactory;
            var store = _services.StoreContext.GetCachedStores().GetStoreById(o.StoreId) ?? _services.StoreContext.CurrentStore;

            var orderSettings = await settingFactory.LoadSettingsAsync<OrderSettings>(store.Id);
            var catalogSettings = await settingFactory.LoadSettingsAsync<CatalogSettings>(store.Id);
            var taxSettings = await settingFactory.LoadSettingsAsync<TaxSettings>(store.Id);
            var pdfSettings = await settingFactory.LoadSettingsAsync<PdfSettings>(store.Id);
            var companyInfoSettings = await settingFactory.LoadSettingsAsync<CompanyInformationSettings>(store.Id);
            var shoppingCartSettings = await settingFactory.LoadSettingsAsync<ShoppingCartSettings>(store.Id);
            var mediaSettings = await settingFactory.LoadSettingsAsync<MediaSettings>(store.Id);

            var countryName = context?.CountryNames?.Get(companyInfoSettings.CountryId) ??
                (await _db.Countries.FindByIdAsync(companyInfoSettings.CountryId, false))?.GetLocalized(x => x.Name)!;

            var customerCurrency = context?.Currencies?.Get(o.CustomerCurrencyCode) ??
                await _db.Currencies.AsNoTracking().FirstOrDefaultAsync(x => x.CurrencyCode == o.CustomerCurrencyCode);

            var model = new OrderDetailsModel
            {
                Order = o,
                Id = o.Id,
                StoreId = o.StoreId,
                CustomerLanguageId = o.CustomerLanguageId,
                CustomerComment = o.CustomerOrderComment,
                OrderNumber = o.GetOrderNumber(),
                CreatedOn = dtHelper.ConvertToUserTime(o.CreatedOnUtc, DateTimeKind.Utc),
                OrderStatus = _services.Localization.GetLocalizedEnum(o.OrderStatus),
                IsReOrderAllowed = orderSettings.IsReOrderAllowed,
                IsReturnRequestAllowed = _orderProcessingService.IsReturnRequestAllowed(o),
                DisplayPdfInvoice = pdfSettings.Enabled,
                RenderOrderNotes = pdfSettings.RenderOrderNotes,
                ShippingStatus = _services.Localization.GetLocalizedEnum(o.ShippingStatus),
                MerchantCompanyInfo = companyInfoSettings,
                MerchantCompanyCountryName = countryName
            };

            if (o.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                model.IsShippable = true;
                model.ShippingMethod = o.ShippingMethod;

                if (o.ShippingAddress != null)
                {
                    model.ShippingAddress = await MapperFactory.MapAsync<Address, AddressModel>(o.ShippingAddress);
                }

                // Shipments (only already shipped).
                await _db.LoadCollectionAsync(o, x => x.Shipments);

                var shipments = o.Shipments
                    .Where(x => x.ShippedDateUtc.HasValue)
                    .OrderBy(x => x.CreatedOnUtc)
                    .ToList();

                foreach (var shipment in shipments)
                {
                    model.Shipments.Add(new OrderDetailsModel.ShipmentBriefModel
                    {
                        Id = shipment.Id,
                        TrackingNumber = shipment.TrackingNumber,
                        ShippedDate = shipment.ShippedDateUtc.HasValue
                            ? dtHelper.ConvertToUserTime(shipment.ShippedDateUtc.Value, DateTimeKind.Utc)
                            : null,
                        DeliveryDate = shipment.DeliveryDateUtc.HasValue
                            ? dtHelper.ConvertToUserTime(shipment.DeliveryDateUtc.Value, DateTimeKind.Utc)
                            : null
                    });
                }
            }

            if (o.BillingAddress != null)
            {
                model.BillingAddress = await MapperFactory.MapAsync<Address, AddressModel>(o.BillingAddress);
            }

            model.VatNumber = o.VatNumber;

            var paymentMethod = await _paymentService.LoadPaymentProviderBySystemNameAsync(o.PaymentMethodSystemName);
            model.PaymentMethod = paymentMethod != null ? _moduleManager.Value.GetLocalizedFriendlyName(paymentMethod.Metadata) : o.PaymentMethodSystemName;
            model.PaymentMethodSystemName = o.PaymentMethodSystemName;
            model.CanRePostProcessPayment = await _paymentService.CanRePostProcessPaymentAsync(o);

            // Purchase order number (we have to find a better to inject this information because it's related to a certain plugin).
            if (o.PaymentMethodSystemName.EqualsNoCase("Smartstore.PurchaseOrderNumber"))
            {
                model.DisplayPurchaseOrderNumber = true;
                model.PurchaseOrderNumber = o.PurchaseOrderNumber;
            }

            if (o.AllowStoringCreditCardNumber)
            {
                model.CardNumber = _encryptor.DecryptText(o.CardNumber);
                model.MaskedCreditCardNumber = _encryptor.DecryptText(o.MaskedCreditCardNumber);
                model.CardCvv2 = _encryptor.DecryptText(o.CardCvv2);
                model.CardExpirationMonth = _encryptor.DecryptText(o.CardExpirationMonth);
                model.CardExpirationYear = _encryptor.DecryptText(o.CardExpirationYear);
            }

            if (o.AllowStoringDirectDebit)
            {
                model.DirectDebitAccountHolder = _encryptor.DecryptText(o.DirectDebitAccountHolder);
                model.DirectDebitAccountNumber = _encryptor.DecryptText(o.DirectDebitAccountNumber);
                model.DirectDebitBankCode = _encryptor.DecryptText(o.DirectDebitBankCode);
                model.DirectDebitBankName = _encryptor.DecryptText(o.DirectDebitBankName);
                model.DirectDebitBIC = _encryptor.DecryptText(o.DirectDebitBIC);
                model.DirectDebitCountry = _encryptor.DecryptText(o.DirectDebitCountry);
                model.DirectDebitIban = _encryptor.DecryptText(o.DirectDebitIban);
            }

            // Totals.
            switch (o.CustomerTaxDisplayType)
            {
                case TaxDisplayType.ExcludingTax:
                {
                    // Order subtotal.
                    model.OrderSubtotal = ConvertToExchangeRate(o.OrderSubtotalExclTax);

                    // Discount (applied to order subtotal).
                    var orderSubTotalDiscountExclTax = ConvertToExchangeRate(o.OrderSubTotalDiscountExclTax);
                    if (orderSubTotalDiscountExclTax > 0)
                    {
                        model.OrderSubTotalDiscount = orderSubTotalDiscountExclTax * -1;
                    }

                    // Order shipping.
                    model.OrderShipping = ConvertToExchangeRate(o.OrderShippingExclTax);

                    // Payment method additional fee.
                    var paymentMethodAdditionalFeeExclTax = ConvertToExchangeRate(o.PaymentMethodAdditionalFeeExclTax);
                    if (paymentMethodAdditionalFeeExclTax != 0)
                    {
                        model.PaymentMethodAdditionalFee = paymentMethodAdditionalFeeExclTax;
                    }
                }
                break;

                case TaxDisplayType.IncludingTax:
                {
                    // Order subtotal.
                    model.OrderSubtotal = ConvertToExchangeRate(o.OrderSubtotalInclTax);

                    // Discount (applied to order subtotal).
                    var orderSubTotalDiscountInclTax = ConvertToExchangeRate(o.OrderSubTotalDiscountInclTax);
                    if (orderSubTotalDiscountInclTax > 0)
                    {
                        model.OrderSubTotalDiscount = orderSubTotalDiscountInclTax * -1;
                    }

                    // Order shipping.
                    model.OrderShipping = ConvertToExchangeRate(o.OrderShippingInclTax);

                    // Payment method additional fee.
                    var paymentMethodAdditionalFeeInclTax = ConvertToExchangeRate(o.PaymentMethodAdditionalFeeInclTax);
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

            if (taxSettings.HideTaxInOrderSummary && o.CustomerTaxDisplayType == TaxDisplayType.IncludingTax)
            {
                displayTax = false;
                displayTaxRates = false;
            }
            else
            {
                if (o.OrderTax == 0 && taxSettings.HideZeroTax)
                {
                    displayTax = false;
                    displayTaxRates = false;
                }
                else
                {
                    displayTaxRates = taxSettings.DisplayTaxRates && o.TaxRatesDictionary.Count > 0;
                    displayTax = !displayTaxRates;

                    model.Tax = ConvertToExchangeRate(o.OrderTax);

                    foreach (var tr in o.TaxRatesDictionary)
                    {
                        var formattedRate = _taxService.FormatTaxRate(tr.Key);
                        var labelKey = _services.WorkContext.TaxDisplayType == TaxDisplayType.IncludingTax ? "ShoppingCart.Totals.TaxRateLineIncl" : "ShoppingCart.Totals.TaxRateLineExcl";
                        var amount = ConvertToExchangeRate(tr.Value);

                        model.TaxRates.Add(new OrderDetailsModel.TaxRate
                        {
                            Rate = tr.Key,
                            FormattedRate = formattedRate,
                            Amount = amount,
                            Label = T(labelKey, formattedRate)
                        });
                    }
                }
            }

            model.DisplayTaxRates = displayTaxRates;
            model.DisplayTax = displayTax;

            // Discount (applied to order total).
            var orderDiscountInCustomerCurrency = ConvertToExchangeRate(o.OrderDiscount);
            if (orderDiscountInCustomerCurrency > 0)
            {
                model.OrderTotalDiscount = orderDiscountInCustomerCurrency * -1;
            }

            // Gift cards.
            await _db.LoadCollectionAsync(o, x => x.GiftCardUsageHistory, false, q => q.Include(x => x.GiftCard));

            foreach (var gcuh in o.GiftCardUsageHistory)
            {
                var remainingAmount = await _giftCardService.GetRemainingAmountAsync(gcuh.GiftCard);
                var amount = ConvertToExchangeRate(gcuh.UsedValue);

                model.GiftCards.Add(new OrderDetailsModel.GiftCard
                {
                    Amount = amount,
                    FormattedAmount = (amount * -1).ToString(),
                    Remaining = ConvertToExchangeRate(remainingAmount.Amount),
                    CouponCode = gcuh.GiftCard.GiftCardCouponCode
                });
            }

            // Reward points.
            await _db.LoadReferenceAsync(o, x => x.RedeemedRewardPointsEntry);

            if (o.RedeemedRewardPointsEntry != null)
            {
                model.RedeemedRewardPoints = -o.RedeemedRewardPointsEntry.Points;
                model.RedeemedRewardPointsAmount = ConvertToExchangeRate(o.RedeemedRewardPointsEntry.UsedAmount) * -1;
            }

            // Credit balance.
            if (o.CreditBalance > 0)
            {
                model.CreditBalance = ConvertToExchangeRate(o.CreditBalance) * -1;
            }

            // Total.
            (var orderTotal, var roundingAmount) = await _orderService.GetOrderTotalInCustomerCurrencyAsync(o, customerCurrency);
            model.OrderTotal = orderTotal;

            if (roundingAmount != 0)
            {
                model.OrderTotalRounding = roundingAmount;
            }

            // Checkout attributes.
            model.CheckoutAttributeInfo = HtmlUtility.ConvertPlainTextToTable(HtmlUtility.ConvertHtmlToPlainText(o.CheckoutAttributeDescription));

            // Order notes.
            await _db.LoadCollectionAsync(o, x => x.OrderNotes);

            var orderNotes = o.OrderNotes
                .Where(x => x.DisplayToCustomer)
                .OrderByDescending(x => x.CreatedOnUtc)
                .ToList();

            foreach (var orderNote in orderNotes)
            {
                var createdOn = dtHelper.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc);

                model.OrderNotes.Add(new OrderDetailsModel.OrderNote
                {
                    Note = orderNote.FormatOrderNoteText(),
                    CreatedOn = createdOn,
                    FriendlyCreatedOn = createdOn.ToHumanizedString(false)
                });
            }

            // Purchased products.
            model.ShowSku = catalogSettings.ShowProductSku;
            model.ShowProductImages = shoppingCartSettings.ShowProductImagesOnShoppingCart;
            model.ShowProductBundleImages = shoppingCartSettings.ShowProductBundleImagesOnShoppingCart;
            model.BundleThumbSize = mediaSettings.CartThumbBundleItemPictureSize;

            await _db.LoadCollectionAsync(o, x => x.OrderItems, false, q => q.Include(x => x.Product));

            foreach (var orderItem in o.OrderItems)
            {
                var orderItemModel = await PrepareOrderItemModelAsync(o, orderItem, catalogSettings, shoppingCartSettings, mediaSettings, customerCurrency);
                model.Items.Add(orderItemModel);
            }

            // Custom mapping
            await MapperFactory.MapWithRegisteredMapperAsync(o, model, new { Context = context });

            return model;

            Money ConvertToExchangeRate(decimal amount)
            {
                return _services.CurrencyService.ConvertToExchangeRate(amount, o.CurrencyRate, customerCurrency);
            }
        }

        public async Task<(Stream Content, string FileName)> GeneratePdfAsync(IEnumerable<Order> orders)
        {
            Guard.NotNull(orders);

            var model = await PrepareOrderDetailsModelsAsync(orders);

            // TODO: (mc) this is bad for multi-document processing, where orders can originate from different stores.
            var storeId = model?[0].StoreId ?? _services.StoreContext.CurrentStore.Id;
            var routeValues = new RouteValueDictionary
            {
                ["storeId"] = storeId,
                ["lid"] = _services.WorkContext.WorkingLanguage.Id
            };

            var pdfSettings = _services.SettingFactory.LoadSettings<PdfSettings>(storeId);

            var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
            {
                Model = model,
                ["PdfMode"] = true
            };

            var orderHtml = await _viewInvoker.InvokeViewAsync(OrderDetailsPrintViewPath, null, viewData);

            var conversionSettings = new PdfConversionSettings
            {
                Size = pdfSettings.LetterPageSizeEnabled ? PdfPageSize.Letter : PdfPageSize.A4,
                Margins = new PdfPageMargins { Top = 35, Bottom = 35 },
                Header = _pdfConverter.CreateFileInput(_urlHelper.Action("ReceiptHeader", "Pdf", routeValues)),
                Footer = _pdfConverter.CreateFileInput(_urlHelper.Action("ReceiptFooter", "Pdf", routeValues)),
                Page = _pdfConverter.CreateHtmlInput(orderHtml.ToString())
            };

            var content = await _pdfConverter.GeneratePdfAsync(conversionSettings);
            var fileName = model?.Count == 1
                ? PathUtility.SanitizeFileName(T("Order.PdfInvoiceFileName", model[0].Id))
                : "orders.pdf";

            return (content, fileName!);
        }

        #region Utilities

        private async Task<OrderHelperContext> CreateOrderHelperContext()
        {
            var countries = await _db.Countries.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x);
            var currencies = await _db.Currencies.AsNoTracking().ToListAsync();

            var countryNames = countries.Select(x => new
            { 
                x.Value.Id,
                Name = x.Value.GetLocalized(y => y.Name).Value }
            );

            var context = new OrderHelperContext
            {
                CountryNames = countryNames.ToDictionary(x => x.Id, x => x.Name),
                Currencies = currencies.ToDictionarySafe(x => x.CurrencyCode, x => x, StringComparer.OrdinalIgnoreCase)
            };

            return context;
        }

        class OrderHelperContext
        {
            // Country.Id -> localized Country.Name.
            public Dictionary<int, string?> CountryNames { get; set; } = default!;

            // Currency code -> Currency.
            public Dictionary<string, Currency> Currencies { get; set; } = default!;
        }

        #endregion
    }
}

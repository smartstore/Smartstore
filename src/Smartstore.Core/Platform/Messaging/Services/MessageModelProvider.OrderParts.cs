using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.ComponentModel;
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
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Engine.Modularity;
using Smartstore.Utilities.Html;

namespace Smartstore.Core.Messaging
{
    public partial class MessageModelProvider
    {
        protected virtual async Task<object> CreateModelPartAsync(Order part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var allow = new HashSet<string>
            {
                nameof(part.Id),
                nameof(part.OrderNumber),
                nameof(part.OrderGuid),
                nameof(part.StoreId),
                nameof(part.OrderStatus),
                nameof(part.PaymentStatus),
                nameof(part.ShippingStatus),
                nameof(part.CustomerTaxDisplayType),
                nameof(part.TaxRatesDictionary),
                nameof(part.VatNumber),
                nameof(part.AffiliateId),
                nameof(part.CustomerIp),
                nameof(part.CardType),
                nameof(part.CardName),
                nameof(part.MaskedCreditCardNumber),
                nameof(part.DirectDebitAccountHolder),
                nameof(part.DirectDebitBankCode), // TODO: (mc) Liquid > Bank data (?)
				nameof(part.PurchaseOrderNumber),
                nameof(part.ShippingMethod),
                nameof(part.PaymentMethodSystemName),
                nameof(part.ShippingRateComputationMethodSystemName)
				// TODO: (mc) Liquid > More whitelisting?
			};

            var m = new HybridExpando(part, allow, MemberOptMethod.Allow);
            var d = m as dynamic;

            d.ID = part.Id;
            d.Billing = await CreateModelPartAsync(part.BillingAddress, messageContext);
            if (part.ShippingAddress != null)
            {
                d.Shipping = part.ShippingAddress == part.BillingAddress ? null : (await CreateModelPartAsync(part.ShippingAddress, messageContext));
            }
            d.CustomerEmail = part.BillingAddress.Email.NullEmpty();
            d.CustomerComment = part.CustomerOrderComment.NullEmpty();
            d.Disclaimer = await _helper.GetTopicAsync("Disclaimer", messageContext);
            d.ConditionsOfUse = await _helper.GetTopicAsync("ConditionsOfUse", messageContext);
            d.Status = await part.OrderStatus.GetLocalizedEnumAsync(messageContext.Language.Id);
            d.CreatedOn = _helper.ToUserDate(part.CreatedOnUtc, messageContext);
            d.PaidOn = _helper.ToUserDate(part.PaidDateUtc, messageContext);

            // Payment method
            var paymentMethodName = part.PaymentMethodSystemName;
            var paymentMethod = _services.Resolve<IProviderManager>().GetProvider<IPaymentMethod>(part.PaymentMethodSystemName);
            if (paymentMethod != null)
            {
                paymentMethodName = _moduleManager.GetLocalizedFriendlyName(paymentMethod.Metadata, messageContext.Language.Id);
            }
            d.PaymentMethod = paymentMethodName.NullEmpty();

            d.Url = part.Customer != null && !part.Customer.IsGuest()
                ? _helper.BuildActionUrl("Details", "Order", new { id = part.Id, area = "" }, messageContext)
                : null;

            // Overrides
            m.Properties["OrderNumber"] = part.GetOrderNumber().NullEmpty();
            m.Properties["AcceptThirdPartyEmailHandOver"] = _helper.GetBoolResource(part.AcceptThirdPartyEmailHandOver, messageContext);

            // Items, Totals & Co.
            d.Items = await part.OrderItems.Where(x => x.Product != null).SelectAsync(async x => await CreateModelPartAsync(x, messageContext)).AsyncToList();
            d.Totals = await CreateOrderTotalsPartAsync(part, messageContext);

            // Checkout Attributes
            if (part.CheckoutAttributeDescription.HasValue())
            {
                d.CheckoutAttributes = HtmlUtility.ConvertPlainTextToTable(HtmlUtility.ConvertHtmlToPlainText(part.CheckoutAttributeDescription)).NullEmpty();
            }

            await _helper.PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateOrderTotalsPartAsync(Order order, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(order, nameof(order));

            var language = messageContext.Language;
            var currencyService = _services.Resolve<ICurrencyService>();
            var giftCardService = _services.Resolve<IGiftCardService>();
            var orderService = _services.Resolve<IOrderService>();
            var taxService = _services.Resolve<ITaxService>();
            var taxSettings = await _services.SettingFactory.LoadSettingsAsync<TaxSettings>(messageContext.Store.Id);

            var taxRates = new SortedDictionary<decimal, decimal>();
            Money cusTaxTotal = new();
            Money cusDiscount = new();
            Money cusRounding = new();
            Money cusTotal = new();

            var customerCurrency = (await _db.Currencies
                .AsNoTracking()
                .Where(x => x.CurrencyCode == order.CustomerCurrencyCode)
                .FirstOrDefaultAsync()) ?? new Currency { CurrencyCode = order.CustomerCurrencyCode };

            var subTotals = GetSubTotals(order, messageContext);

            // Shipping
            bool dislayShipping = order.ShippingStatus != ShippingStatus.ShippingNotRequired;

            // Payment method fee
            bool displayPaymentMethodFee = true;
            if (order.PaymentMethodAdditionalFeeExclTax == decimal.Zero)
            {
                displayPaymentMethodFee = false;
            }

            // Tax
            bool displayTax = true;
            bool displayTaxRates = true;
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
                    taxRates = new SortedDictionary<decimal, decimal>();
                    foreach (var tr in order.TaxRatesDictionary)
                    {
                        taxRates.Add(tr.Key, tr.Value * order.CurrencyRate);
                    }

                    displayTaxRates = taxSettings.DisplayTaxRates && taxRates.Count > 0;
                    displayTax = !displayTaxRates;

                    cusTaxTotal = _helper.FormatPrice(order.OrderTax, order, messageContext);
                }
            }

            // Discount
            bool dislayDiscount = false;
            if (order.OrderDiscount > decimal.Zero)
            {
                cusDiscount = _helper.FormatPrice(-order.OrderDiscount, order, messageContext);
                dislayDiscount = true;
            }

            // Total
            (var orderTotal, var roundingAmount) = await orderService.GetOrderTotalInCustomerCurrencyAsync(order, customerCurrency);
            cusTotal = _helper.FormatPrice(orderTotal.Amount, customerCurrency, messageContext);

            //// Rounding
            if (roundingAmount != decimal.Zero)
            {
                cusRounding = _helper.FormatPrice(roundingAmount.Amount, customerCurrency, messageContext);
            }

            //// Model
            dynamic m = new ExpandoObject();

            m.SubTotal = subTotals.SubTotal;
            m.SubTotalDiscount = subTotals.DisplaySubTotalDiscount ? (decimal?)subTotals.SubTotalDiscount.Amount : null;
            m.Shipping = dislayShipping ? (decimal?)subTotals.ShippingTotal.Amount : null;
            m.Payment = displayPaymentMethodFee ? (decimal?)subTotals.PaymentFee.Amount : null;
            m.Tax = displayTax ? (decimal?)cusTaxTotal.Amount : null;
            m.Discount = dislayDiscount ? (decimal?)cusDiscount.Amount : null;
            m.RoundingDiff = cusRounding;
            m.Total = cusTotal;
            m.IsGross = order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax;

            // TaxRates
            m.TaxRates = !displayTaxRates ? null : taxRates.Select(x =>
            {
                return new
                {
                    Rate = T("Order.TaxRateLine", language.Id, taxService.FormatTaxRate(x.Key)).ToString(),
                    Value = _helper.FormatPrice(x.Value, order, messageContext)
                };
            }).ToArray();

            // Gift Cards
            m.GiftCardUsage = order.GiftCardUsageHistory.Count == 0 ? null : order.GiftCardUsageHistory.Select(x =>
            {
                var remainingAmount = giftCardService.GetRemainingAmount(x.GiftCard);

                return new
                {
                    GiftCard = T("Order.GiftCardInfo", language.Id, x.GiftCard.GiftCardCouponCode).ToString(),
                    UsedAmount = _helper.FormatPrice(-x.UsedValue, order, messageContext),
                    RemainingAmount = _helper.FormatPrice(remainingAmount.Amount, order, messageContext)
                };
            }).ToArray();

            // Reward Points
            m.RedeemedRewardPoints = order.RedeemedRewardPointsEntry == null ? null : new
            {
                Title = T("Order.RewardPoints", language.Id, -order.RedeemedRewardPointsEntry.Points).ToString(),
                Amount = _helper.FormatPrice(-order.RedeemedRewardPointsEntry.UsedAmount, order, messageContext)
            };

            await _helper.PublishModelPartCreatedEventAsync(order, m);

            return m;
        }

        private (Money SubTotal, Money SubTotalDiscount, Money ShippingTotal, Money PaymentFee, bool DisplaySubTotalDiscount) GetSubTotals(Order order, MessageContext messageContext)
        {
            var isNet = order.CustomerTaxDisplayType == TaxDisplayType.ExcludingTax;

            var subTotal = isNet ? order.OrderSubtotalExclTax : order.OrderSubtotalInclTax;
            var subTotalDiscount = isNet ? order.OrderSubTotalDiscountExclTax : order.OrderSubTotalDiscountInclTax;
            var shipping = isNet ? order.OrderShippingExclTax : order.OrderShippingInclTax;
            var payment = isNet ? order.PaymentMethodAdditionalFeeExclTax : order.PaymentMethodAdditionalFeeInclTax;

            // Subtotal
            var cusSubTotal = _helper.FormatPrice(subTotal, order, messageContext);

            // Shipping
            var cusShipTotal = _helper.FormatPrice(shipping, order, messageContext);

            // Payment method additional fee
            var cusPaymentMethodFee = _helper.FormatPrice(payment, order, messageContext);

            // Discount (applied to order subtotal)
            Money cusSubTotalDiscount = new();
            bool dislaySubTotalDiscount = false;
            if (subTotalDiscount > decimal.Zero)
            {
                cusSubTotalDiscount = _helper.FormatPrice(-subTotalDiscount, order, messageContext);
                dislaySubTotalDiscount = true;
            }

            return (cusSubTotal, cusSubTotalDiscount, cusShipTotal, cusPaymentMethodFee, dislaySubTotalDiscount);
        }

        protected virtual async Task<object> CreateModelPartAsync(OrderItem part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var productAttributeMaterializer = _services.Resolve<IProductAttributeMaterializer>();
            var downloadService = _services.Resolve<IDownloadService>();
            var order = part.Order;
            var isNet = order.CustomerTaxDisplayType == TaxDisplayType.ExcludingTax;
            var product = part.Product;
            var attributeCombination = await productAttributeMaterializer.FindAttributeCombinationAsync(product.Id, part.AttributeSelection);
            product.MergeWithCombination(attributeCombination);

            // Bundle items.
            object bundleItems = null;
            if (product.ProductType == ProductType.BundledProduct && part.BundleData.HasValue())
            {
                var bundleData = part.GetBundleData();
                if (bundleData.Any())
                {
                    var productIds = bundleData.Select(x => x.ProductId).ToArray();
                    var products = await _db.Products.GetManyAsync(productIds);
                    var productsDic = products.ToDictionarySafe(x => x.Id, x => x);

                    bundleItems = bundleData
                        .OrderBy(x => x.DisplayOrder)
                        .Select(async x =>
                        {
                            productsDic.TryGetValue(x.ProductId, out Product bundleItemProduct);
                            return await CreateModelPartAsync(x, part, bundleItemProduct, messageContext);
                        })
                        .ToList();
                }
            }
            
            var m = new Dictionary<string, object>
            {
                { "DownloadUrl", !downloadService.IsDownloadAllowed(part) ? null : _helper.BuildActionUrl("GetDownload", "Download", new { id = part.OrderItemGuid, area = "" }, messageContext) },
                { "AttributeDescription", part.AttributeDescription.NullEmpty() },
                { "Weight", part.ItemWeight },
                { "TaxRate", part.TaxRate },
                { "Qty", part.Quantity },
                { "UnitPrice", _helper.FormatPrice(isNet ? part.UnitPriceExclTax : part.UnitPriceInclTax, part.Order, messageContext) },
                { "LineTotal", _helper.FormatPrice(isNet ? part.PriceExclTax : part.PriceInclTax, part.Order, messageContext) },
                { "Product", await CreateModelPartAsync(product, messageContext, part.AttributeSelection) },
                { "BundleItems", bundleItems },
                { "IsGross", !isNet },
                { "DisplayDeliveryTime", part.DisplayDeliveryTime },
            };

            if (part.DeliveryTimeId.HasValue)
            {
                var deliveryTime = await _db.DeliveryTimes.FindByIdAsync(part.DeliveryTimeId ?? 0, false);
                if (deliveryTime is DeliveryTime dt)
                {
                    m["DeliveryTime"] = new Dictionary<string, object>
                    {
                        { "Color", dt.ColorHexValue },
                        { "Name", dt.GetLocalized(x => x.Name, messageContext.Language).Value },
                    };
                }
            }

            await _helper.PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(ProductBundleItemOrderData part, OrderItem orderItem, Product product, MessageContext messageContext)
        {
            Guard.NotNull(part, nameof(part));
            Guard.NotNull(orderItem, nameof(orderItem));
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(messageContext, nameof(messageContext));

            var priceWithDiscount = _helper.FormatPrice(part.PriceWithDiscount, orderItem.Order, messageContext);

            var m = new Dictionary<string, object>
            {
                { "AttributeDescription", part.AttributesInfo.NullEmpty() },
                { "Quantity", part.Quantity > 1 && part.PerItemShoppingCart ? part.Quantity.ToString() : null },
                { "PerItemShoppingCart", part.PerItemShoppingCart },
                { "PriceWithDiscount", priceWithDiscount },
                { "Product", await CreateModelPartAsync(product, messageContext, part.AttributeSelection) }
            };

            await _helper.PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(OrderNote part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                { "Id", part.Id },
                { "CreatedOn", _helper.ToUserDate(part.CreatedOnUtc, messageContext) },
                { "Text", part.FormatOrderNoteText().NullEmpty() }
            };

            await _helper.PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(ShoppingCartItem part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                { "Id", part.Id },
                { "Quantity", part.Quantity },
                { "Product", await CreateModelPartAsync(part.Product, messageContext, part.AttributeSelection) },
            };

            await _helper.PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(Shipment part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var itemParts = new List<object>();

            // TODO: (mh) (core) ToListAsync, ToMultimap, ToDictionarySafe. Debug & Test. 
            var db = _services.Resolve<SmartDbContext>();
            var orderItems = await db.OrderItems.AsNoTracking().ApplyStandardFilter(part.OrderId).ToListAsync();
            var map = orderItems.ToMultimap(x => x.OrderId, x => x);
            var orderItemsDic = orderItems.ToDictionarySafe(x => x.Id);

            foreach (var shipmentItem in part.ShipmentItems)
            {
                if (orderItemsDic.TryGetValue(shipmentItem.OrderItemId, out var orderItem) && orderItem.Product != null)
                {
                    var itemPart = await CreateModelPartAsync(orderItem, messageContext) as Dictionary<string, object>;
                    itemPart["Qty"] = shipmentItem.Quantity;
                    itemParts.Add(itemPart);
                }
            }

            var trackingUrl = part.TrackingUrl;

            if (trackingUrl.IsEmpty() && part.TrackingNumber.HasValue() && part.Order.ShippingRateComputationMethodSystemName.HasValue())
            {
                // Try to get URL from tracker.
                var srcm = _services.Resolve<IShippingService>()
                    .LoadActiveShippingRateComputationMethods(systemName: part.Order.ShippingRateComputationMethodSystemName)
                    .FirstOrDefault();

                if (srcm != null && srcm.Value.IsActive)
                {
                    var tracker = srcm.Value.ShipmentTracker;
                    if (tracker != null)
                    {
                        var shippingSettings = await _services.SettingFactory.LoadSettingsAsync<ShippingSettings>(part.Order.StoreId);
                        if (srcm.IsShippingRateComputationMethodActive(shippingSettings))
                        {
                            trackingUrl = tracker.GetUrl(part.TrackingNumber);
                        }
                    }
                }
            }

            var m = new Dictionary<string, object>
            {
                { "Id", part.Id },
                { "TrackingNumber", part.TrackingNumber.NullEmpty() },
                { "TrackingUrl", trackingUrl.NullEmpty() },
                { "TotalWeight", part.TotalWeight },
                { "CreatedOn", _helper.ToUserDate(part.CreatedOnUtc, messageContext) },
                { "DeliveredOn", _helper.ToUserDate(part.DeliveryDateUtc, messageContext) },
                { "ShippedOn", _helper.ToUserDate(part.ShippedDateUtc, messageContext) },
                { "Url", _helper.BuildActionUrl("ShipmentDetails", "Order", new { id = part.Id, area = "" }, messageContext)},
                { "Items", itemParts },
            };

            await _helper.PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(RecurringPayment part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                { "Id", part.Id },
                { "CreatedOn", _helper.ToUserDate(part.CreatedOnUtc, messageContext) },
                { "StartedOn", _helper.ToUserDate(part.StartDateUtc, messageContext) },
                { "NextOn", _helper.ToUserDate(part.NextPaymentDate, messageContext) },
                { "CycleLength", part.CycleLength },
                { "CyclePeriod", part.CyclePeriod.GetLocalizedEnum(messageContext.Language.Id) },
                { "CyclesRemaining", part.CyclesRemaining },
                { "TotalCycles", part.TotalCycles },
                { "Url", _helper.BuildActionUrl("Edit", "RecurringPayment", new { id = part.Id, area = "Admin" }, messageContext) }
            };

            await _helper.PublishModelPartCreatedEventAsync(part, m);

            return m;
        }

        protected virtual async Task<object> CreateModelPartAsync(ReturnRequest part, MessageContext messageContext)
        {
            Guard.NotNull(messageContext, nameof(messageContext));
            Guard.NotNull(part, nameof(part));

            var m = new Dictionary<string, object>
            {
                { "Id", part.Id },
                { "Reason", part.ReasonForReturn.NullEmpty() },
                { "Status", await part.ReturnRequestStatus.GetLocalizedEnumAsync(messageContext.Language.Id) },
                { "RequestedAction", part.RequestedAction.NullEmpty() },
                { "CustomerComments", HtmlUtility.StripTags(part.CustomerComments).NullEmpty() },
                { "StaffNotes", HtmlUtility.StripTags(part.StaffNotes).NullEmpty() },
                { "Quantity", part.Quantity },
                { "RefundToWallet", part.RefundToWallet },
                { "Url", _helper.BuildActionUrl("Edit", "ReturnRequest", new { id = part.Id, area = "Admin" }, messageContext) }
            };

            await _helper.PublishModelPartCreatedEventAsync(part, m);

            return m;
        }
    }
}

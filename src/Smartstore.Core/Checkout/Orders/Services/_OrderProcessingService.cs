using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messages;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderProcessingService : IOrderProcessingService
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizationService _localizationService;
        private readonly ICurrencyService _currencyService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IMessageFactory _messageFactory;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly OrderSettings _orderSettings;
        private readonly Currency _primaryCurrency;

        public OrderProcessingService(
            SmartDbContext db,
            ILocalizationService localizationService,
            ICurrencyService currencyService,
            IPaymentService paymentService,
            IOrderCalculationService orderCalculationService,
            IShoppingCartService shoppingCartService,
            IMessageFactory messageFactory,
            RewardPointsSettings rewardPointsSettings,
            OrderSettings orderSettings)
        {
            _db = db;
            _localizationService = localizationService;
            _currencyService = currencyService;
            _paymentService = paymentService;
            _orderCalculationService = orderCalculationService;
            _shoppingCartService = shoppingCartService;
            _messageFactory = messageFactory;
            _rewardPointsSettings = rewardPointsSettings;
            _orderSettings = orderSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual async Task<bool> CanRefundAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderTotal == decimal.Zero)
                return false;

            // Only partial refunds allowed if already refunded.
            if (order.RefundedAmount > decimal.Zero)
                return false;

            // Uncomment the lines below in order to allow this operation for cancelled orders.
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            if (order.PaymentStatus == PaymentStatus.Paid && await _paymentService.SupportRefundAsync(order.PaymentMethodSystemName))
                return true;

            return false;
        }

        public virtual async Task<IList<string>> RefundAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (!await CanRefundAsync(order))
            {
                throw new SmartException(T("Order.CannotRefund"));
            }

            var request = new RefundPaymentRequest();
            RefundPaymentResult result = null;

            try
            {
                request.Order = order;
                request.AmountToRefund = new Money(order.OrderTotal, _primaryCurrency);
                request.IsPartialRefund = false;

                result = await _paymentService.RefundAsync(request);

                if (result.Success)
                {
                    var totalAmountRefunded = order.RefundedAmount + request.AmountToRefund.Amount;
                    order.RefundedAmount = totalAmountRefunded;
                    order.PaymentStatus = result.NewPaymentStatus;

                    order.AddOrderNote(T("Admin.OrderNotice.OrderRefunded", request.AmountToRefund.ToString(true)));
                    await _db.SaveChangesAsync();

                    await CheckOrderStatusAsync(order);
                }
            }
            catch (Exception ex)
            {
                result ??= new();
                result.Errors.Add(ex.ToAllMessages());
            }

            if (result.Errors.Any())
            {
                ProcessErrors(order, result.Errors, "Admin.OrderNotice.OrderRefundError");
                await _db.SaveChangesAsync();
            }

            return result.Errors;
        }

        public virtual bool CanRefundOffline(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderTotal == decimal.Zero)
                return false;

            // Only partial refunds allowed if already refunded.
            if (order.RefundedAmount > decimal.Zero)
                return false;

            // Uncomment the lines below in order to allow this operation for cancelled orders.
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //     return false;

            if (order.PaymentStatus == PaymentStatus.Paid)
                return true;

            return false;
        }

        public virtual async Task RefundOfflineAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (!CanRefundOffline(order))
            {
                throw new SmartException(T("Order.CannotRefund"));
            }

            var amountToRefund = new Money(order.OrderTotal, _primaryCurrency);
            var totalAmountRefunded = order.RefundedAmount + amountToRefund.Amount;

            order.RefundedAmount = totalAmountRefunded;
            order.PaymentStatus = PaymentStatus.Refunded;

            order.AddOrderNote(T("Admin.OrderNotice.OrderMarkedAsRefunded", amountToRefund.ToString(true)));
            await _db.SaveChangesAsync();

            await CheckOrderStatusAsync(order);
        }

        public virtual async Task<bool> CanPartiallyRefundAsync(Order order, Money amountToRefund)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderTotal == decimal.Zero)
                return false;

            // Uncomment the lines below in order to allow this operation for cancelled orders.
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            var canBeRefunded = order.OrderTotal - order.RefundedAmount;
            if (canBeRefunded <= decimal.Zero)
                return false;

            if (amountToRefund > canBeRefunded)
                return false;

            if ((order.PaymentStatus == PaymentStatus.Paid || order.PaymentStatus == PaymentStatus.PartiallyRefunded) &&
                await _paymentService.SupportPartiallyRefundAsync(order.PaymentMethodSystemName))
                return true;

            return false;
        }

        public virtual async Task<IList<string>> PartiallyRefundAsync(Order order, Money amountToRefund)
        {
            Guard.NotNull(order, nameof(order));

            if (!await CanPartiallyRefundAsync(order, amountToRefund))
            {
                throw new SmartException(T("Order.CannotPartialRefund"));
            }

            var request = new RefundPaymentRequest();
            RefundPaymentResult result = null;

            try
            {
                request.Order = order;
                request.AmountToRefund = amountToRefund;
                request.IsPartialRefund = true;

                result = await _paymentService.RefundAsync(request);

                if (result.Success)
                {
                    var totalAmountRefunded = order.RefundedAmount + amountToRefund.Amount;
                    order.RefundedAmount = totalAmountRefunded;
                    order.PaymentStatus = result.NewPaymentStatus;

                    order.AddOrderNote(T("Admin.OrderNotice.OrderPartiallyRefunded", amountToRefund.ToString(true)));
                    await _db.SaveChangesAsync();

                    await CheckOrderStatusAsync(order);
                }
            }
            catch (Exception ex)
            {
                result ??= new();
                result.Errors.Add(ex.ToAllMessages());
            }

            if (result.Errors.Any())
            {
                ProcessErrors(order, result.Errors, "Admin.OrderNotice.OrderPartiallyRefundError");
                await _db.SaveChangesAsync();
            }

            return result.Errors;
        }

        public virtual bool CanPartiallyRefundOffline(Order order, Money amountToRefund)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderTotal == decimal.Zero)
                return false;

            // Uncomment the lines below in order to allow this operation for cancelled orders.
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            var canBeRefunded = order.OrderTotal - order.RefundedAmount;
            if (canBeRefunded <= decimal.Zero)
                return false;

            if (amountToRefund > canBeRefunded)
                return false;

            if (order.PaymentStatus == PaymentStatus.Paid ||
                order.PaymentStatus == PaymentStatus.PartiallyRefunded)
                return true;

            return false;
        }

        public virtual async Task PartiallyRefundOfflineAsync(Order order, Money amountToRefund)
        {
            Guard.NotNull(order, nameof(order));

            if (!CanPartiallyRefundOffline(order, amountToRefund))
            {
                throw new SmartException(T("Order.CannotPartialRefund"));
            }

            var totalAmountRefunded = order.RefundedAmount + amountToRefund.Amount;
            order.RefundedAmount = totalAmountRefunded;
            order.PaymentStatus = PaymentStatus.PartiallyRefunded;

            order.AddOrderNote(T("Admin.OrderNotice.OrderMarkedAsPartiallyRefunded", amountToRefund.ToString(true)));
            await _db.SaveChangesAsync();

            await CheckOrderStatusAsync(order);
        }

        public virtual async Task<bool> CanVoidAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderTotal == decimal.Zero)
                return false;

            // Uncomment the lines below in order to allow this operation for cancelled orders.
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            if (order.PaymentStatus == PaymentStatus.Authorized && await _paymentService.SupportVoidAsync(order.PaymentMethodSystemName))
                return true;

            return false;
        }

        public virtual async Task<IList<string>> VoidAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (!await CanVoidAsync(order))
            {
                throw new SmartException(T("Order.CannotVoid"));
            }

            var request = new VoidPaymentRequest();
            VoidPaymentResult result = null;

            try
            {
                request.Order = order;
                result = await _paymentService.VoidAsync(request);

                if (result.Success)
                {
                    order.PaymentStatus = result.NewPaymentStatus;
                    order.AddOrderNote(T("Admin.OrderNotice.OrderVoided"));
                    await _db.SaveChangesAsync();

                    await CheckOrderStatusAsync(order);
                }
            }
            catch (Exception ex)
            {
                result ??= new();
                result.Errors.Add(ex.ToAllMessages());
            }

            if (result.Errors.Any())
            {
                ProcessErrors(order, result.Errors, "Admin.OrderNotice.OrderVoidError");
                await _db.SaveChangesAsync();
            }

            return result.Errors;
        }

        public virtual bool CanVoidOffline(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderTotal == decimal.Zero)
                return false;

            // Uncomment the lines below in order to allow this operation for cancelled orders.
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            if (order.PaymentStatus == PaymentStatus.Authorized)
                return true;

            return false;
        }

        public virtual async Task VoidOfflineAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (!CanVoidOffline(order))
            {
                throw new SmartException(T("Order.CannotVoid"));
            }

            order.PaymentStatusId = (int)PaymentStatus.Voided;
            order.AddOrderNote(T("Admin.OrderNotice.OrderMarkedAsVoided"));
            await _db.SaveChangesAsync();

            await CheckOrderStatusAsync(order);
        }

        public virtual async Task ReOrderAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            foreach (var orderItem in order.OrderItems)
            {
                var isBundle = orderItem.Product.ProductType == ProductType.BundledProduct;
                
                var addToCartContext = new AddToCartContext
                {
                    Customer = order.Customer,
                    Product = orderItem.Product,
                    CartType = ShoppingCartType.ShoppingCart,
                    StoreId = order.StoreId,
                    RawAttributes = orderItem.RawAttributes,
                    CustomerEnteredPrice = new(isBundle ? decimal.Zero : orderItem.UnitPriceExclTax, _primaryCurrency),
                    Quantity = orderItem.Quantity,
                    AutomaticallyAddRequiredProductsIfEnabled = false
                };

                var valid = await _shoppingCartService.AddToCartAsync(addToCartContext);

                if (valid && isBundle && orderItem.BundleData.HasValue())
                {
                    var bundleData = orderItem.GetBundleData();
                    var bundleItemIds = bundleData.Select(x => x.BundleItemId).Distinct().ToArray();

                    var bundleItems = await _db.ProductBundleItem
                        .AsNoTracking()
                        .Include(x => x.Product)
                        .Where(x => bundleItemIds.Contains(x.Id))
                        .ToListAsync();

                    var bundleItemsDic = bundleItems.ToDictionarySafe(x => x.Id);

                    foreach (var itemData in bundleData)
                    {
                        bundleItemsDic.TryGetValue(itemData.BundleItemId, out var bundleItem);

                        // TODO: (mg) (core) Add data of bundle items when re-ordering, as soon as ShoppingCartService allows it.
                        //var itemContext = new AddToCartContext
                        //{
                        //    Customer = order.Customer,
                        //    Product = bundleItem.Product,
                        //    BundleItem = bundleItem,
                        //    CartType = ShoppingCartType.ShoppingCart,
                        //    StoreId = order.StoreId,
                        //    RawAttributes = itemData.RawAttributes,
                        //    CustomerEnteredPrice = new(_primaryCurrency),
                        //    Quantity = itemData.Quantity,
                        //    AutomaticallyAddRequiredProductsIfEnabled = false
                        //};

                        //await _shoppingCartService.AddToCartAsync(itemContext);
                    }
                }
            }
        }

        public virtual bool IsReturnRequestAllowed(Order order)
        {
            if (!_orderSettings.ReturnRequestsEnabled ||
                order == null || 
                order.Deleted ||
                order.OrderStatus != OrderStatus.Complete)
            {
                return false;
            }

            if (_orderSettings.NumberOfDaysReturnRequestAvailable == 0)
            {
                return true;
            }
            else
            {
                var daysPassed = (DateTime.UtcNow - order.CreatedOnUtc).TotalDays;
                return (daysPassed - _orderSettings.NumberOfDaysReturnRequestAvailable) < 0;
            }
        }

        public virtual async Task<(bool Valid, Money OrderTotalMinimum)> IsAboveOrderTotalMinimumAsync(IList<OrganizedShoppingCartItem> cart, int[] customerRoleIds)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(customerRoleIds, nameof(customerRoleIds));

            var roleQuery = _db.CustomerRoles
                .AsNoTracking()
                .Where(x => x.OrderTotalMinimum > decimal.Zero && customerRoleIds.Contains(x.Id));

            roleQuery = _orderSettings.MultipleOrderTotalRestrictionsExpandRange
                ? roleQuery.OrderBy(x => x.OrderTotalMinimum)
                : roleQuery.OrderByDescending(x => x.OrderTotalMinimum);

            var customerRole = await roleQuery.FirstOrDefaultAsync();
            var orderTotalMin = (customerRole == null ? _orderSettings.OrderTotalMinimum : customerRole.OrderTotalMinimum) ?? decimal.Zero;
            var isValid = true;

            if (cart.Any() && orderTotalMin > decimal.Zero)
            {
                var cartSubTotal = await _orderCalculationService.GetShoppingCartSubTotalAsync(cart);

                isValid = cartSubTotal.SubTotalWithoutDiscount < orderTotalMin;
            }

            return (isValid, new(orderTotalMin, _primaryCurrency));
        }

        public virtual async Task<(bool Valid, Money OrderTotalMaximum)> IsBelowOrderTotalMaximumAsync(IList<OrganizedShoppingCartItem> cart, int[] customerRoleIds)
        {
            Guard.NotNull(cart, nameof(cart));
            Guard.NotNull(customerRoleIds, nameof(customerRoleIds));

            var roleQuery = _db.CustomerRoles
                .AsNoTracking()
                .Where(x => x.OrderTotalMaximum > decimal.Zero && customerRoleIds.Contains(x.Id));

            roleQuery = _orderSettings.MultipleOrderTotalRestrictionsExpandRange
                ? roleQuery.OrderByDescending(x => x.OrderTotalMaximum)
                : roleQuery.OrderBy(x => x.OrderTotalMaximum);

            var customerRole = await roleQuery.FirstOrDefaultAsync();
            var orderTotalMax = (customerRole == null ? _orderSettings.OrderTotalMaximum : customerRole.OrderTotalMaximum) ?? decimal.Zero;
            var isValid = true;

            if (cart.Any() && orderTotalMax > decimal.Zero)
            {
                var cartSubTotal = await _orderCalculationService.GetShoppingCartSubTotalAsync(cart);

                isValid = cartSubTotal.SubTotalWithoutDiscount > orderTotalMax;
            }

            return (isValid, new(orderTotalMax, _primaryCurrency));
        }

        public virtual async Task<Shipment> AddShipmentAsync(Order order, string trackingNumber, string trackingUrl, Dictionary<int, int> quantities)
        {
            Guard.NotNull(order, nameof(order));

            Shipment shipment = null;
            decimal? totalWeight = null;

            foreach (var orderItem in order.OrderItems)
            {
                if (!orderItem.Product.IsShippingEnabled)
                    continue;

                // Ensure that this product can be shipped (have at least one item to ship).
                var maxQtyToAdd = orderItem.GetItemsCanBeAddedToShipmentCount();
                if (maxQtyToAdd <= 0)
                    continue;

                var qtyToAdd = 0;

                if (quantities != null && quantities.ContainsKey(orderItem.Id))
                {
                    qtyToAdd = quantities[orderItem.Id];
                }
                else if (quantities == null)
                {
                    qtyToAdd = maxQtyToAdd;
                }

                if (qtyToAdd <= 0)
                    continue;

                if (qtyToAdd > maxQtyToAdd)
                    qtyToAdd = maxQtyToAdd;

                var orderItemTotalWeight = orderItem.ItemWeight.HasValue ? orderItem.ItemWeight * qtyToAdd : null;
                if (orderItemTotalWeight.HasValue)
                {
                    if (!totalWeight.HasValue)
                        totalWeight = 0;

                    totalWeight += orderItemTotalWeight.Value;
                }

                if (shipment == null)
                {
                    shipment = new Shipment
                    {
                        OrderId = order.Id,
                        // Otherwise order updated event would not be fired during InsertShipment:
                        Order = order,
                        TrackingNumber = trackingNumber,
                        TrackingUrl = trackingUrl,
                        TotalWeight = null,
                        ShippedDateUtc = null,
                        DeliveryDateUtc = null,
                        CreatedOnUtc = DateTime.UtcNow
                    };
                }

                shipment.ShipmentItems.Add(new ShipmentItem
                {
                    OrderItemId = orderItem.Id,
                    Quantity = qtyToAdd
                });
            }

            if (shipment?.ShipmentItems?.Any() ?? false)
            {
                shipment.TotalWeight = totalWeight;

                _db.Shipments.Add(shipment);
                await _db.SaveChangesAsync();

                return shipment;
            }

            return null;
        }

        #region Utilities

        protected virtual string FormatTaxRates(SortedDictionary<decimal, decimal> taxRates)
        {
            return string.Join("   ", taxRates.Select(x => "{0}:{1};".FormatInvariant(x.Key.ToString(CultureInfo.InvariantCulture), x.Value.ToString(CultureInfo.InvariantCulture))));
        }

        /// <summary>
        /// Logs errors and adds order notes. The caller is responsible for database commit.
        /// </summary>
        /// <param name="order"></param>
        /// <param name="errors"></param>
        /// <param name="messageKey"></param>
        protected virtual void ProcessErrors(Order order, IList<string> errors, string messageKey)
        {
            var msg = T(messageKey, order.GetOrderNumber()).ToString() + " " + string.Join(" ", errors);

            order.AddOrderNote(msg);
            Logger.Error(msg);
        }

        /// <summary>
        /// Applies reward points. The caller is responsible for database commit.
        /// </summary>
        protected virtual void ApplyRewardPoints(Order order, bool reduce, decimal? amount = null)
        {
            if (!_rewardPointsSettings.Enabled ||
                _rewardPointsSettings.PointsForPurchases_Amount <= decimal.Zero ||
                // Ensure that reward points were not added before. We should not add reward points if they were already earned for this order.
                order.RewardPointsWereAdded ||
                order.Customer == null ||
                order.Customer.IsGuest())
            {
                return;
            }

            var rewardAmount = _rewardPointsSettings.PointsForPurchases_Amount * _rewardPointsSettings.PointsForPurchases_Points;

            if (reduce)
            {
                // We use Math.Round here because Truncate increases the risk of inaccuracy of rounding.
                var points = (int)Math.Round((amount ?? order.OrderTotal) / rewardAmount);

                if (order.RewardPointsRemaining.HasValue && order.RewardPointsRemaining.Value < points)
                {
                    points = order.RewardPointsRemaining.Value;
                }

                if (points != 0)
                {
                    order.Customer.AddRewardPointsHistoryEntry(-points, T("RewardPoints.Message.ReducedForOrder", order.GetOrderNumber()));

                    if (!order.RewardPointsRemaining.HasValue)
                    {
                        order.RewardPointsRemaining = (int)Math.Round(order.OrderTotal / rewardAmount);
                    }

                    order.RewardPointsRemaining = Math.Max(order.RewardPointsRemaining.Value - points, 0);
                }
            }
            else
            {
                // Truncate same as Floor for positive amounts.
                var points = (int)Math.Truncate((amount ?? order.OrderTotal) / rewardAmount);
                if (points != 0)
                {
                    order.Customer.AddRewardPointsHistoryEntry(points, T("RewardPoints.Message.EarnedForOrder", order.GetOrderNumber()));
                    order.RewardPointsWereAdded = true;
                }
            }
        }

        /// <summary>
        /// Activates gift cards. The caller is responsible for database commit.
        /// </summary>
        protected virtual async Task ActivateGiftCardsAsync(Order order)
        {
            var activateGiftCards = _orderSettings.GiftCards_Activated_OrderStatusId > 0 && _orderSettings.GiftCards_Activated_OrderStatusId == (int)order.OrderStatus;
            var deactivateGiftCards = _orderSettings.GiftCards_Deactivated_OrderStatusId > 0 && _orderSettings.GiftCards_Deactivated_OrderStatusId == (int)order.OrderStatus;

            if (!activateGiftCards && !deactivateGiftCards)
            {
                return;
            }

            var giftCards = await _db.GiftCards
                .Include(x => x.PurchasedWithOrderItem)
                .ApplyOrderFilter(new[] { order.Id })
                .ToListAsync();

            if (!giftCards.Any())
            {
                return;
            }

            var allLanguages = await _db.Languages.AsNoTracking().ToListAsync();
            var allLanguagesDic = allLanguages.ToDictionary(x => x.Id);

            foreach (var gc in giftCards)
            {
                if (activateGiftCards && !gc.IsGiftCardActivated)
                {
                    var isRecipientNotified = gc.IsRecipientNotified;

                    if (gc.GiftCardType == GiftCardType.Virtual)
                    {
                        // Send email for virtual gift card.
                        if (gc.RecipientEmail.HasValue() && gc.SenderEmail.HasValue())
                        {
                            if (!allLanguagesDic.TryGetValue(order.CustomerLanguageId, out var customerLang))
                            {
                                customerLang = allLanguages.FirstOrDefault();
                            }

                            var msgResult = await _messageFactory.SendGiftCardNotificationAsync(gc, customerLang.Id);
                            isRecipientNotified = msgResult?.Email.Id != null;
                        }
                    }

                    gc.IsGiftCardActivated = true;
                    gc.IsRecipientNotified = isRecipientNotified;
                }
                else if (deactivateGiftCards && gc.IsGiftCardActivated)
                {
                    gc.IsGiftCardActivated = false;
                }
            }
        }

        protected virtual async Task SetOrderStatusAsync(Order order, OrderStatus status, bool notifyCustomer)
        {
            Guard.NotNull(order, nameof(order));

            var prevOrderStatus = order.OrderStatus;
            if (prevOrderStatus == status)
            {
                return;
            }

            order.OrderStatusId = (int)status;

            // Save new order status.
            await _db.SaveChangesAsync();

            order.AddOrderNote(T("Admin.OrderNotice.OrderStatusChanged", await _localizationService.GetLocalizedEnumAsync(status)));

            if (prevOrderStatus != OrderStatus.Complete && status == OrderStatus.Complete && notifyCustomer)
            {
                var msgResult = await _messageFactory.SendOrderCompletedCustomerNotificationAsync(order, order.CustomerLanguageId);
                if (msgResult?.Email?.Id != null)
                {
                    order.AddOrderNote(T("Admin.OrderNotice.CustomerCompletedEmailQueued", msgResult.Email.Id));
                }
            }

            if (prevOrderStatus != OrderStatus.Cancelled && status == OrderStatus.Cancelled && notifyCustomer)
            {
                var msgResult = await _messageFactory.SendOrderCancelledCustomerNotificationAsync(order, order.CustomerLanguageId);
                if (msgResult?.Email?.Id != null)
                {
                    order.AddOrderNote(T("Admin.OrderNotice.CustomerCancelledEmailQueued", msgResult.Email.Id));
                }
            }

            // Reward points.
            if (_rewardPointsSettings.PointsForPurchases_Awarded == order.OrderStatus)
            {
                ApplyRewardPoints(order, false);
            }
            if (_rewardPointsSettings.PointsForPurchases_Canceled == order.OrderStatus)
            {
                ApplyRewardPoints(order, true);
            }

            // Gift cards activation.
            await ActivateGiftCardsAsync(order);

            // Update order.
            await _db.SaveChangesAsync();
        }

        protected virtual async Task CheckOrderStatusAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.PaymentStatus == PaymentStatus.Paid && !order.PaidDateUtc.HasValue)
            {
                order.PaidDateUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            if (order.OrderStatus == OrderStatus.Pending && 
                (order.PaymentStatus == PaymentStatus.Authorized || order.PaymentStatus == PaymentStatus.Paid))
            {
                await SetOrderStatusAsync(order, OrderStatus.Processing, false);
            }

            if (order.OrderStatus == OrderStatus.Pending &&
                (order.ShippingStatus == ShippingStatus.PartiallyShipped || order.ShippingStatus == ShippingStatus.Shipped || order.ShippingStatus == ShippingStatus.Delivered))
            {
                await SetOrderStatusAsync(order, OrderStatus.Processing, false);
            }

            if (order.OrderStatus != OrderStatus.Cancelled && 
                order.OrderStatus != OrderStatus.Complete &&
                order.PaymentStatus == PaymentStatus.Paid &&
                (order.ShippingStatus == ShippingStatus.ShippingNotRequired || order.ShippingStatus == ShippingStatus.Delivered))
            {
                await SetOrderStatusAsync(order, OrderStatus.Complete, true);
            }
        }

        #endregion
    }
}

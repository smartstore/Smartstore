using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.GiftCards;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Checkout.Shipping;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Messages;
using Smartstore.Core.Security;
using Smartstore.Core.Web;
using Smartstore.Data;
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderProcessingService : IOrderProcessingService
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly ICurrencyService _currencyService;
        private readonly IPaymentService _paymentService;
        private readonly IProductService _productService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ITaxService _taxService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IMessageFactory _messageFactory;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly IEncryptor _encryptor;
        private readonly IEventPublisher _eventPublisher;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly OrderSettings _orderSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly TaxSettings _taxSettings;
        private readonly Currency _primaryCurrency;
        private readonly Currency _workingCurrency;

        public OrderProcessingService(
            SmartDbContext db,
            IWorkContext workContext,
            IWebHelper webHelper,
            ILocalizationService localizationService,
            ICurrencyService currencyService,
            IPaymentService paymentService,
            IProductService productService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IOrderCalculationService orderCalculationService,
            ITaxService taxService,
            IShoppingCartService shoppingCartService,
            IShoppingCartValidator shoppingCartValidator,
            IMessageFactory messageFactory,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            IEncryptor encryptor,
            IEventPublisher eventPublisher,
            RewardPointsSettings rewardPointsSettings,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings,
            LocalizationSettings localizationSettings,
            TaxSettings taxSettings)
        {
            _db = db;
            _workContext = workContext;
            _webHelper = webHelper;
            _localizationService = localizationService;
            _currencyService = currencyService;
            _paymentService = paymentService;
            _productService = productService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _orderCalculationService = orderCalculationService;
            _taxService = taxService;
            _shoppingCartService = shoppingCartService;
            _shoppingCartValidator = shoppingCartValidator;
            _messageFactory = messageFactory;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _encryptor = encryptor;
            _eventPublisher = eventPublisher;
            _rewardPointsSettings = rewardPointsSettings;
            _orderSettings = orderSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _localizationSettings = localizationSettings;
            _taxSettings = taxSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
            _workingCurrency = workContext.WorkingCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual bool CanCancelOrder(Order order)
        {
            // TODO: (mg) (core) Make extension method for Order.
            Guard.NotNull(order, nameof(order));

            if (order.OrderStatus == OrderStatus.Cancelled)
                return false;

            return true;
        }

        public virtual async Task CancelOrderAsync(Order order, bool notifyCustomer)
        {
            Guard.NotNull(order, nameof(order));

            if (!CanCancelOrder(order))
            {
                throw new SmartException(T("Order.CannotCancel"));
            }

            await SetOrderStatusAsync(order, OrderStatus.Cancelled, notifyCustomer);

            order.AddOrderNote(T("Admin.OrderNotice.OrderCancelled"));

            // Cancel recurring payments.
            var recurringPayments = await _db.RecurringPayments
                .ApplyStandardFilter(order.Id)
                .ToListAsync();

            foreach (var rp in recurringPayments)
            {
                await CancelRecurringPaymentAsync(rp);
            }

            // Adjust inventory.
            foreach (var orderItem in order.OrderItems)
            {
                await _productService.AdjustInventoryAsync(orderItem, false, orderItem.Quantity);
            }

            await _db.SaveChangesAsync();
        }

        public virtual bool CanCompleteOrder(Order order)
        {
            // TODO: (mg) (core) Make extension method for Order.
            Guard.NotNull(order, nameof(order));

            return order.OrderStatus != OrderStatus.Complete && order.OrderStatus != OrderStatus.Cancelled;
        }

        public virtual async Task CompleteOrderAsync(Order order)
        {
            if (!CanCompleteOrder(order))
            {
                throw new SmartException(T("Order.CannotMarkCompleted"));
            }

            if (CanMarkOrderAsPaid(order))
            {
                await MarkOrderAsPaidAsync(order);
            }

            if (order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                order.ShippingStatusId = (int)ShippingStatus.Delivered;
            }

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
        }

        public virtual async Task DeleteOrderAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderStatus != OrderStatus.Cancelled)
            {
                ApplyRewardPoints(order, true);

                // Cancel recurring payments.
                var recurringPayments = await _db.RecurringPayments
                    .ApplyStandardFilter(order.Id)
                    .ToListAsync();

                foreach (var rp in recurringPayments)
                {
                    await CancelRecurringPaymentAsync(rp);
                }

                // Adjust inventory.
                foreach (var orderItem in order.OrderItems)
                {
                    await _productService.AdjustInventoryAsync(orderItem, false, orderItem.Quantity);
                }
            }

            _db.Orders.Remove(order);

            //order.AddOrderNote(T("Admin.OrderNotice.OrderDeleted"));

            await _db.SaveChangesAsync();
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

                        // TODO: (mg) (core) Add data of bundle items for re-ordering, as soon as ShoppingCartService allows it.
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

        public virtual async Task ShipAsync(Shipment shipment, bool notifyCustomer)
        {
            Guard.NotNull(shipment, nameof(shipment));

            var order = shipment.Order;
            if (order == null)
                throw new SmartException(T("Order.NotFound", shipment.OrderId));

            if (shipment.ShippedDateUtc.HasValue)
                throw new SmartException(T("Shipment.AlreadyShipped"));

            shipment.ShippedDateUtc = DateTime.UtcNow;

            // Check whether we have more items to ship.
            order.ShippingStatusId = order.CanAddItemsToShipment() || order.HasItemsToDispatch()
                ? (int)ShippingStatus.PartiallyShipped
                : (int)ShippingStatus.Shipped;

            order.AddOrderNote(T("Admin.OrderNotice.ShipmentSent", shipment.Id));

            if (notifyCustomer)
            {
                var msg = await _messageFactory.SendShipmentSentCustomerNotificationAsync(shipment, order.CustomerLanguageId);
                if (msg?.Email?.Id != null)
                {
                    order.AddOrderNote(T("Admin.OrderNotice.CustomerShippedEmailQueued", msg.Email.Id));
                }
            }

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
        }

        public virtual async Task DeliverAsync(Shipment shipment, bool notifyCustomer)
        {
            Guard.NotNull(shipment, nameof(shipment));

            var order = shipment.Order;
            if (order == null)
                throw new SmartException(T("Order.NotFound", shipment.OrderId));

            if (shipment.DeliveryDateUtc.HasValue)
                throw new SmartException(T("Shipment.AlreadyDelivered"));

            shipment.DeliveryDateUtc = DateTime.UtcNow;

            if (!order.CanAddItemsToShipment() && !order.HasItemsToDispatch() && !order.HasItemsToDeliver())
            {
                order.ShippingStatusId = (int)ShippingStatus.Delivered;
            }

            order.AddOrderNote(T("Admin.OrderNotice.ShipmentDelivered", shipment.Id));

            if (notifyCustomer)
            {
                var msg = await _messageFactory.SendShipmentDeliveredCustomerNotificationAsync(shipment, order.CustomerLanguageId);
                if (msg?.Email?.Id != null)
                {
                    order.AddOrderNote(T("Admin.OrderNotice.CustomerDeliveredEmailQueued", msg.Email.Id));
                }
            }

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
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

        public virtual async Task<OrderTotalValidationResult> ValidateOrderTotalAsync(IList<OrganizedShoppingCartItem> cart, params CustomerRole[] customerRoles)
        {
            Guard.NotNull(cart, nameof(cart));

            var minRolesQuery = _orderSettings.MultipleOrderTotalRestrictionsExpandRange
                ? customerRoles.Where(x => x.OrderTotalMinimum > decimal.Zero).OrderBy(x => x.OrderTotalMinimum)
                : customerRoles.Where(x => x.OrderTotalMinimum > decimal.Zero).OrderByDescending(x => x.OrderTotalMinimum);

            var maxRolesQuery = _orderSettings.MultipleOrderTotalRestrictionsExpandRange
                ? customerRoles.Where(x => x.OrderTotalMaximum > decimal.Zero).OrderByDescending(x => x.OrderTotalMaximum)
                : customerRoles.Where(x => x.OrderTotalMaximum > decimal.Zero).OrderBy(x => x.OrderTotalMaximum);

            var minRole = minRolesQuery.FirstOrDefault();
            var maxRole = maxRolesQuery.FirstOrDefault();

            var orderTotalMin = (minRole == null ? _orderSettings.OrderTotalMinimum : minRole.OrderTotalMinimum) ?? decimal.Zero;
            var orderTotalMax = (maxRole == null ? _orderSettings.OrderTotalMaximum : maxRole.OrderTotalMaximum) ?? decimal.Zero;
            var isAboveMin = true;
            var isBelowMax = true;

            if (cart.Any() && (orderTotalMin > decimal.Zero || orderTotalMax > decimal.Zero))
            {
                var cartSubTotal = await _orderCalculationService.GetShoppingCartSubTotalAsync(cart);

                if (orderTotalMin > decimal.Zero)
                {
                    isAboveMin = cartSubTotal.SubTotalWithoutDiscount >= orderTotalMin;
                }

                if (orderTotalMax > decimal.Zero)
                {
                    isBelowMax = cartSubTotal.SubTotalWithoutDiscount <= orderTotalMax;
                }
            }

            var result = new OrderTotalValidationResult
            {
                OrderTotalMinimum = orderTotalMin,
                OrderTotalMaximum = orderTotalMax,
                IsAboveMinimum = isAboveMin,
                IsBelowMaximum = isBelowMax
            };

            return result;
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
                var maxQtyToAdd = orderItem.GetShippableItemsCount();
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

        public virtual async Task AutoUpdateOrderDetailsAsync(AutoUpdateOrderItemContext context)
        {
            var oi = context.OrderItem;

            context.RewardPointsOld = context.RewardPointsNew = oi.Order.Customer.GetRewardPointsBalance();

            if (context.UpdateTotals && oi.Order.OrderStatusId <= (int)OrderStatus.Pending)
            {
                var currency = await _db.Currencies.AsNoTracking().FirstOrDefaultAsync(x => x.CurrencyCode == oi.Order.CustomerCurrencyCode);

                decimal priceInclTax = currency.RoundIfEnabledFor(context.QuantityNew * oi.UnitPriceInclTax);
                decimal priceExclTax = currency.RoundIfEnabledFor(context.QuantityNew * oi.UnitPriceExclTax);

                decimal deltaPriceInclTax = context.IsNewOrderItem
                    ? priceInclTax
                    : priceInclTax - (context.PriceInclTaxOld?.Amount ?? oi.PriceInclTax);

                decimal deltaPriceExclTax = context.IsNewOrderItem
                    ? priceExclTax
                    : priceExclTax - (context.PriceExclTaxOld?.Amount ?? oi.PriceExclTax);

                oi.Quantity = context.QuantityNew;
                oi.PriceInclTax = currency.RoundIfEnabledFor(priceInclTax);
                oi.PriceExclTax = currency.RoundIfEnabledFor(priceExclTax);

                decimal subtotalInclTax = oi.Order.OrderSubtotalInclTax + deltaPriceInclTax;
                decimal subtotalExclTax = oi.Order.OrderSubtotalExclTax + deltaPriceExclTax;

                oi.Order.OrderSubtotalInclTax = currency.RoundIfEnabledFor(subtotalInclTax);
                oi.Order.OrderSubtotalExclTax = currency.RoundIfEnabledFor(subtotalExclTax);

                decimal discountInclTax = oi.DiscountAmountInclTax * context.QuantityChangeFactor;
                decimal discountExclTax = oi.DiscountAmountExclTax * context.QuantityChangeFactor;

                //decimal deltaDiscountInclTax = discountInclTax - oi.DiscountAmountInclTax;
                //decimal deltaDiscountExclTax = discountExclTax - oi.DiscountAmountExclTax;

                oi.DiscountAmountInclTax = currency.RoundIfEnabledFor(discountInclTax);
                oi.DiscountAmountExclTax = currency.RoundIfEnabledFor(discountExclTax);

                decimal total = Math.Max(oi.Order.OrderTotal + deltaPriceInclTax, 0);
                decimal tax = Math.Max(oi.Order.OrderTax + (deltaPriceInclTax - deltaPriceExclTax), 0);

                oi.Order.OrderTotal = currency.RoundIfEnabledFor(total);
                oi.Order.OrderTax = currency.RoundIfEnabledFor(tax);

                // Update tax rate value.
                var deltaTax = deltaPriceInclTax - deltaPriceExclTax;
                if (deltaTax != decimal.Zero)
                {
                    var taxRates = oi.Order.TaxRatesDictionary;

                    taxRates[oi.TaxRate] = taxRates.ContainsKey(oi.TaxRate)
                        ? Math.Max(taxRates[oi.TaxRate] + deltaTax, 0)
                        : Math.Max(deltaTax, 0);

                    oi.Order.TaxRates = FormatTaxRates(taxRates);
                }

                await _db.SaveChangesAsync();
            }

            if (context.AdjustInventory && context.QuantityDelta != 0)
            {
                context.Inventory = await _productService.AdjustInventoryAsync(oi, context.QuantityDelta > 0, Math.Abs(context.QuantityDelta));

                await _db.SaveChangesAsync();
            }

            if (context.UpdateRewardPoints && context.QuantityDelta < 0)
            {
                // We reduce but we do not award points subsequently. They can be awarded once per order anyway (see Order.RewardPointsWereAdded).
                // UpdateRewardPoints only visible for unpending orders (see RewardPointsSettingsValidator).
                // Note: reducing can of course only work if oi.UnitPriceExclTax has not been changed!
                decimal reduceAmount = Math.Abs(context.QuantityDelta) * oi.UnitPriceInclTax;
                ApplyRewardPoints(oi.Order, true, reduceAmount);

                await _db.SaveChangesAsync();

                context.RewardPointsNew = oi.Order.Customer.GetRewardPointsBalance();
            }
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

            var allLanguages = await _db.Languages.AsNoTracking().ToDictionaryAsync(x => x.Id);

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
                            if (!allLanguages.TryGetValue(order.CustomerLanguageId, out var customerLang))
                            {
                                customerLang = allLanguages.Values.FirstOrDefault();
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

            using var scope = new DbContextScope(_db, deferCommit: true);

            if (order.PaymentStatus == PaymentStatus.Paid && !order.PaidDateUtc.HasValue)
            {
                order.PaidDateUtc = DateTime.UtcNow;
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

            await scope.CommitAsync();
        }

        #endregion
    }
}

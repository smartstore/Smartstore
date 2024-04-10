using System.Globalization;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
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
using Smartstore.Core.Logging;
using Smartstore.Core.Messaging;
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
        private readonly IRoundingHelper _roundingHelper;
        private readonly IPaymentService _paymentService;
        private readonly IProductService _productService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly ITaxCalculator _taxCalculator;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly IShippingService _shippingService;
        private readonly IGiftCardService _giftCardService;
        private readonly INewsletterSubscriptionService _newsletterSubscriptionService;
        private readonly ICheckoutAttributeFormatter _checkoutAttributeFormatter;
        private readonly IEncryptor _encryptor;
        private readonly IMessageFactory _messageFactory;
        private readonly IEventPublisher _eventPublisher;
        private readonly IActivityLogger _activityLogger;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly CatalogSettings _catalogSettings;
        private readonly OrderSettings _orderSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly TaxSettings _taxSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly Currency _primaryCurrency;
        private readonly Currency _workingCurrency;

        public OrderProcessingService(
            SmartDbContext db,
            IWorkContext workContext,
            IWebHelper webHelper,
            ILocalizationService localizationService,
            ICurrencyService currencyService,
            IRoundingHelper roundingHelper,
            IPaymentService paymentService,
            IProductService productService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IProductAttributeFormatter productAttributeFormatter,
            IPriceCalculationService priceCalculationService,
            IOrderCalculationService orderCalculationService,
            ITaxCalculator taxCalculator,
            IShoppingCartService shoppingCartService,
            IShoppingCartValidator shoppingCartValidator,
            IShippingService shippingService,
            IGiftCardService giftCardService,
            INewsletterSubscriptionService newsletterSubscriptionService,
            ICheckoutAttributeFormatter checkoutAttributeFormatter,
            IEncryptor encryptor,
            IMessageFactory messageFactory,
            IEventPublisher eventPublisher,
            IActivityLogger activityLogger,
            RewardPointsSettings rewardPointsSettings,
            CatalogSettings catalogSettings,
            OrderSettings orderSettings,
            ShoppingCartSettings shoppingCartSettings,
            LocalizationSettings localizationSettings,
            TaxSettings taxSettings,
            PaymentSettings paymentSettings)
        {
            _db = db;
            _workContext = workContext;
            _webHelper = webHelper;
            _localizationService = localizationService;
            _currencyService = currencyService;
            _roundingHelper = roundingHelper;
            _paymentService = paymentService;
            _productService = productService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _productAttributeFormatter = productAttributeFormatter;
            _priceCalculationService = priceCalculationService;
            _orderCalculationService = orderCalculationService;
            _taxCalculator = taxCalculator;
            _shoppingCartService = shoppingCartService;
            _shoppingCartValidator = shoppingCartValidator;
            _shippingService = shippingService;
            _giftCardService = giftCardService;
            _newsletterSubscriptionService = newsletterSubscriptionService;
            _checkoutAttributeFormatter = checkoutAttributeFormatter;
            _encryptor = encryptor;
            _messageFactory = messageFactory;
            _eventPublisher = eventPublisher;
            _activityLogger = activityLogger;
            _rewardPointsSettings = rewardPointsSettings;
            _catalogSettings = catalogSettings;
            _orderSettings = orderSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _localizationSettings = localizationSettings;
            _taxSettings = taxSettings;
            _paymentSettings = paymentSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
            _workingCurrency = workContext.WorkingCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual Task<int> GetDispatchedItemsCountAsync(OrderItem orderItem, bool dispatched)
        {
            Guard.NotNull(orderItem);

            if (dispatched)
            {
                return SumUpQuantity(orderItem, x => x.ShippedDateUtc.HasValue);
            }
            else
            {
                return SumUpQuantity(orderItem, x => !x.ShippedDateUtc.HasValue);
            }
        }

        public virtual async Task<bool> HasItemsToDispatchAsync(Order order)
        {
            Guard.NotNull(order);

            await LoadNavigationProperties(order, false, true);

            foreach (var orderItem in order.OrderItems.Where(x => x.Product.IsShippingEnabled))
            {
                var notDispatchedItems = await GetDispatchedItemsCountAsync(orderItem, false);
                if (notDispatchedItems <= 0)
                    continue;

                // Yes, we have at least one item to ship.
                return true;
            }

            return false;
        }

        public virtual Task<int> GetDeliveredItemsCountAsync(OrderItem orderItem, bool delivered)
        {
            Guard.NotNull(orderItem);

            if (delivered)
            {
                return SumUpQuantity(orderItem, x => x.DeliveryDateUtc.HasValue);
            }
            else
            {
                return SumUpQuantity(orderItem, x => !x.DeliveryDateUtc.HasValue);
            }
        }

        public virtual async Task<bool> HasItemsToDeliverAsync(Order order)
        {
            Guard.NotNull(order);

            await LoadNavigationProperties(order, false, true);

            foreach (var orderItem in order.OrderItems.Where(x => x.Product.IsShippingEnabled))
            {
                var dispatchedItems = await GetDispatchedItemsCountAsync(orderItem, true);
                var deliveredItems = await GetDeliveredItemsCountAsync(orderItem, true);

                if (dispatchedItems <= deliveredItems)
                    continue;

                // Yes, we have at least one item to deliver.
                return true;
            }

            return false;
        }

        public virtual async Task<int> GetShippableItemsCountAsync(OrderItem orderItem)
        {
            var itemsCount = await GetShipmentItemsCountAsync(orderItem);

            return Math.Max(orderItem.Quantity - itemsCount, 0);
        }

        public virtual Task<int> GetShipmentItemsCountAsync(OrderItem orderItem)
        {
            Guard.NotNull(orderItem);

            return SumUpQuantity(orderItem, null);
        }

        public virtual async Task<bool> CanAddItemsToShipmentAsync(Order order)
        {
            Guard.NotNull(order);

            await LoadNavigationProperties(order, false, true);

            foreach (var orderItem in order.OrderItems.Where(x => x.Product.IsShippingEnabled))
            {
                var canBeAddedToShipment = await GetShippableItemsCountAsync(orderItem);
                if (canBeAddedToShipment <= 0)
                    continue;

                // Yes, we have at least one item to create a new shipment.
                return true;
            }

            return false;
        }

        public virtual async Task CancelOrderAsync(Order order, bool notifyCustomer)
        {
            Guard.NotNull(order);

            await LoadNavigationProperties(order, true);

            if (!order.CanCancelOrder())
            {
                throw new InvalidOperationException(T("Order.CannotCancel"));
            }

            await SetOrderStatusAsync(order, OrderStatus.Cancelled, notifyCustomer);

            order.AddOrderNote(T("Admin.OrderNotice.OrderCancelled"));

            // Cancel recurring payments.
            var recurringPayments = await _db.RecurringPayments
                .Include(x => x.InitialOrder)
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

        public virtual async Task CompleteOrderAsync(Order order)
        {
            await LoadNavigationProperties(order, true);

            if (!order.CanCompleteOrder())
            {
                throw new InvalidOperationException(T("Order.CannotMarkCompleted"));
            }

            if (_paymentSettings.CapturePaymentReason == CapturePaymentReason.OrderCompleted && await CanCaptureAsync(order))
            {
                await CaptureAsync(order);
            }

            if (order.CanMarkOrderAsPaid())
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
            Guard.NotNull(order);

            if (order.OrderStatus != OrderStatus.Cancelled)
            {
                await LoadNavigationProperties(order, true);

                ApplyRewardPoints(order, true);

                // Cancel recurring payments.
                var recurringPayments = await _db.RecurringPayments
                    .Include(x => x.InitialOrder)
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

            order.Deleted = true;

            await _db.SaveChangesAsync();
        }

        public virtual async Task ReOrderAsync(Order order)
        {
            Guard.NotNull(order);

            await LoadNavigationProperties(order, true);

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
                };

                var valid = await _shoppingCartService.AddToCartAsync(addToCartContext);

                if (valid && isBundle && orderItem.BundleData.HasValue())
                {
                    var bundleData = orderItem.GetBundleData();
                    var bundleItemIds = bundleData.Select(x => x.BundleItemId).Distinct().ToArray();

                    var bundleItems = await _db.ProductBundleItem
                        .Include(x => x.Product)
                        .Include(x => x.BundleProduct)
                        .Where(x => bundleItemIds.Contains(x.Id))
                        .ToListAsync();

                    var bundleItemsDic = bundleItems.ToDictionarySafe(x => x.Id);

                    foreach (var itemData in bundleData)
                    {
                        bundleItemsDic.TryGetValue(itemData.BundleItemId, out var bundleItem);

                        var itemContext = new AddToCartContext
                        {
                            Item = addToCartContext.Item,
                            ChildItems = addToCartContext.ChildItems,
                            Customer = order.Customer,
                            Product = bundleItem.Product,
                            BundleItem = bundleItem,
                            CartType = ShoppingCartType.ShoppingCart,
                            StoreId = order.StoreId,
                            RawAttributes = itemData.RawAttributes,
                            CustomerEnteredPrice = new(_primaryCurrency),
                            Quantity = itemData.Quantity,
                        };

                        if (!await _shoppingCartService.AddToCartAsync(itemContext))
                        {
                            valid = false;
                            break;
                        }
                    }
                }

                if (valid && isBundle)
                {
                    await _shoppingCartService.AddItemToCartAsync(addToCartContext);
                }
            }
        }

        public virtual async Task ShipAsync(Shipment shipment, bool notifyCustomer)
        {
            Guard.NotNull(shipment);
            Guard.NotNull(shipment.Order);

            var order = shipment.Order;

            if (shipment.ShippedDateUtc.HasValue)
            {
                throw new Exception(T("Shipment.AlreadyShipped"));
            }

            shipment.ShippedDateUtc = DateTime.UtcNow;

            // Check whether we have more items to ship.
            order.ShippingStatusId = await CanAddItemsToShipmentAsync(order) || await HasItemsToDispatchAsync(order)
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
            Guard.NotNull(shipment);
            Guard.NotNull(shipment.Order);

            var order = shipment.Order;

            shipment.DeliveryDateUtc = DateTime.UtcNow;

            if (!await CanAddItemsToShipmentAsync(order) &&
                !await HasItemsToDispatchAsync(order) &&
                !await HasItemsToDeliverAsync(order))
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

        public virtual async Task<OrderTotalValidationResult> ValidateOrderTotalAsync(ShoppingCart cart, params CustomerRole[] customerRoles)
        {
            Guard.NotNull(cart);

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

            if (cart.HasItems && (orderTotalMin > decimal.Zero || orderTotalMax > decimal.Zero))
            {
                var subtotal = await _orderCalculationService.GetShoppingCartSubtotalAsync(cart, activeOnly: true);

                if (orderTotalMin > decimal.Zero)
                {
                    isAboveMin = subtotal.SubtotalWithoutDiscount >= orderTotalMin;
                }

                if (orderTotalMax > decimal.Zero)
                {
                    isBelowMax = subtotal.SubtotalWithoutDiscount <= orderTotalMax;
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
            Guard.NotNull(order);

            await LoadNavigationProperties(order, true, true);

            Shipment shipment = null;
            decimal? totalWeight = null;

            foreach (var orderItem in order.OrderItems)
            {
                if (!orderItem.Product.IsShippingEnabled)
                    continue;

                // Ensure that this product can be shipped (have at least one item to ship).
                var maxQtyToAdd = await GetShippableItemsCountAsync(orderItem);
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

        public virtual async Task UpdateOrderDetailsAsync(OrderItem orderItem, UpdateOrderDetailsContext context)
        {
            Guard.NotNull(orderItem);
            Guard.NotNull(orderItem.Order);

            await LoadNavigationProperties(orderItem.Order, true, true);

            var oi = orderItem;
            var order = oi.Order;
            var oldQuantity = context.OldQuantity ?? oi.Quantity;
            var newQuantity = context.NewQuantity ?? oi.Quantity;
            var oldPriceInclTax = context.OldPriceInclTax ?? oi.PriceInclTax;
            var oldPriceExclTax = context.OldPriceExclTax ?? oi.PriceExclTax;

            if (context.ReduceQuantity > 0)
            {
                var reduceQuantity = context.ReduceQuantity > oi.Quantity ? oi.Quantity : context.ReduceQuantity;
                newQuantity = Math.Max(oi.Quantity - reduceQuantity, 0);
            }

            if (context.UpdateOrderItem)
            {
                if (newQuantity == 0)
                {
                    return;
                }

                oi.Quantity = newQuantity;
                oi.UnitPriceInclTax = context.NewUnitPriceInclTax ?? oi.UnitPriceInclTax;
                oi.UnitPriceExclTax = context.NewUnitPriceExclTax ?? oi.UnitPriceExclTax;
                oi.TaxRate = context.NewTaxRate ?? oi.TaxRate;
                oi.DiscountAmountInclTax = context.NewDiscountInclTax ?? oi.DiscountAmountInclTax;
                oi.DiscountAmountExclTax = context.NewDiscountExclTax ?? oi.DiscountAmountExclTax;
                oi.PriceInclTax = context.NewPriceInclTax ?? oi.PriceInclTax;
                oi.PriceExclTax = context.NewPriceExclTax ?? oi.PriceExclTax;
            }

            context.OldRewardPoints = context.NewRewardPoints = order.Customer.GetRewardPointsBalance();

            if (context.UpdateTotals && order.OrderStatusId <= (int)OrderStatus.Pending)
            {
                var currency = await _db.Currencies
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.CurrencyCode == order.CustomerCurrencyCode) ?? _primaryCurrency;

                decimal priceInclTax = _roundingHelper.RoundIfEnabledFor(newQuantity * oi.UnitPriceInclTax, currency, order.CustomerTaxDisplayType);
                decimal priceExclTax = _roundingHelper.RoundIfEnabledFor(newQuantity * oi.UnitPriceExclTax, currency, order.CustomerTaxDisplayType);

                decimal priceInclTaxDiff = priceInclTax - oldPriceInclTax;
                decimal priceExclTaxDiff = priceExclTax - oldPriceExclTax;

                oi.Quantity = newQuantity;
                oi.PriceInclTax = _roundingHelper.RoundIfEnabledFor(priceInclTax, currency, order.CustomerTaxDisplayType);
                oi.PriceExclTax = _roundingHelper.RoundIfEnabledFor(priceExclTax, currency, order.CustomerTaxDisplayType);

                decimal subtotalInclTax = order.OrderSubtotalInclTax + priceInclTaxDiff;
                decimal subtotalExclTax = order.OrderSubtotalExclTax + priceExclTaxDiff;

                order.OrderSubtotalInclTax = _roundingHelper.RoundIfEnabledFor(subtotalInclTax, currency, order.CustomerTaxDisplayType);
                order.OrderSubtotalExclTax = _roundingHelper.RoundIfEnabledFor(subtotalExclTax, currency, order.CustomerTaxDisplayType);

                decimal quantityChangeFactor = oldQuantity != 0 ? newQuantity / oldQuantity : 1.0M;

                decimal discountInclTax = oi.DiscountAmountInclTax * quantityChangeFactor;
                decimal discountExclTax = oi.DiscountAmountExclTax * quantityChangeFactor;

                //decimal deltaDiscountInclTax = discountInclTax - oi.DiscountAmountInclTax;
                //decimal deltaDiscountExclTax = discountExclTax - oi.DiscountAmountExclTax;

                oi.DiscountAmountInclTax = _roundingHelper.RoundIfEnabledFor(discountInclTax, currency, order.CustomerTaxDisplayType);
                oi.DiscountAmountExclTax = _roundingHelper.RoundIfEnabledFor(discountExclTax, currency, order.CustomerTaxDisplayType);

                decimal total = Math.Max(order.OrderTotal + priceInclTaxDiff, 0);
                decimal tax = Math.Max(order.OrderTax + (priceInclTaxDiff - priceExclTaxDiff), 0);

                order.OrderTotal = _roundingHelper.RoundIfEnabledFor(total, currency, order.CustomerTaxDisplayType);
                order.OrderTax = _roundingHelper.RoundIfEnabledFor(tax, currency, order.CustomerTaxDisplayType);

                // Update tax rate value.
                var deltaTax = priceInclTaxDiff - priceExclTaxDiff;
                if (deltaTax != decimal.Zero)
                {
                    var taxRates = order.TaxRatesDictionary;

                    taxRates[oi.TaxRate] = taxRates.ContainsKey(oi.TaxRate)
                        ? Math.Max(taxRates[oi.TaxRate] + deltaTax, 0)
                        : Math.Max(deltaTax, 0);

                    order.TaxRates = FormatTaxRates(taxRates);
                }

                await _db.SaveChangesAsync();
            }

            var quantityDiff = newQuantity - oldQuantity;

            if (context.AdjustInventory && quantityDiff != 0)
            {
                context.Inventory = await _productService.AdjustInventoryAsync(oi, quantityDiff > 0, Math.Abs(quantityDiff));
            }

            if (context.UpdateRewardPoints && quantityDiff < 0)
            {
                // We reduce but we do not award points subsequently. They can be awarded once per order anyway (see Order.RewardPointsWereAdded).
                // UpdateRewardPoints only visible for unpending orders (see RewardPointsSettingsValidator).
                // Note: reducing can of course only work if oi.UnitPriceExclTax has not been changed!
                decimal reduceAmount = Math.Abs(quantityDiff) * oi.UnitPriceInclTax;
                ApplyRewardPoints(order, true, reduceAmount);

                context.NewRewardPoints = order.Customer.GetRewardPointsBalance();
            }

            await _db.SaveChangesAsync();
        }

        #region Utilities

        protected virtual string FormatTaxRates(SortedDictionary<decimal, decimal> taxRates)
        {
            return string.Join("   ", taxRates.Select(x => "{0}:{1};".FormatInvariant(x.Key.ToString(CultureInfo.InvariantCulture), x.Value.ToString(CultureInfo.InvariantCulture))));
        }

        /// <summary>
        /// Applies reward points. The caller is responsible for database commit.
        /// </summary>
        protected virtual void ApplyRewardPoints(Order order, bool reduce, decimal? amount = null)
        {
            if (!_rewardPointsSettings.Enabled ||
                _rewardPointsSettings.PointsForPurchases_Amount <= decimal.Zero ||
                (!reduce && order.RewardPointsWereAdded) || 
                (reduce && !order.RewardPointsWereAdded) ||
                order.Customer == null ||
                order.Customer.IsGuest())
            {
                return;
            }

            var rewardAmount = (amount ?? order.OrderTotal) / _rewardPointsSettings.PointsForPurchases_Amount * _rewardPointsSettings.PointsForPurchases_Points;

            if (reduce)
            {
                // We use IRoundingHelper here because Truncate increases the risk of inaccuracy of rounding.
                var points = (int)_roundingHelper.Round(rewardAmount, 0, _primaryCurrency.MidpointRounding);

                if (order.RewardPointsRemaining.HasValue && order.RewardPointsRemaining.Value < points)
                {
                    points = order.RewardPointsRemaining.Value;
                }

                if (points != 0)
                {
                    order.Customer.AddRewardPointsHistoryEntry(-points, T("RewardPoints.Message.ReducedForOrder", order.GetOrderNumber()));

                    if (!order.RewardPointsRemaining.HasValue)
                    {
                        var remainingPoints = order.OrderTotal / _rewardPointsSettings.PointsForPurchases_Amount * _rewardPointsSettings.PointsForPurchases_Points;
                        order.RewardPointsRemaining = (int)_roundingHelper.Round(remainingPoints, 0, _primaryCurrency.MidpointRounding);
                    }

                    order.RewardPointsRemaining = Math.Max(order.RewardPointsRemaining.Value - points, 0);
                }
            }
            else
            {
                // Truncate same as Floor for positive amounts.
                var points = (int)Math.Truncate(rewardAmount);
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
            var activateGiftCards = _orderSettings.GiftCards_Activated_OrderStatusId > 0 && _orderSettings.GiftCards_Activated_OrderStatusId == order.OrderStatusId;
            var deactivateGiftCards = _orderSettings.GiftCards_Deactivated_OrderStatusId > 0 && _orderSettings.GiftCards_Deactivated_OrderStatusId == order.OrderStatusId;

            if (!activateGiftCards && !deactivateGiftCards)
            {
                return;
            }

            var giftCards = await _db.GiftCards
                .Include(x => x.PurchasedWithOrderItem)
                .ThenInclude(x => x.Order)
                .ApplyOrderFilter(new[] { order.Id })
                .ToListAsync();

            if (giftCards.Count == 0)
            {
                return;
            }

            var customerLanguage = 
                await _db.Languages.FindByIdAsync(order.CustomerLanguageId, false) ?? 
                await _db.Languages.AsNoTracking().Where(x => x.Published).OrderBy(x => x.DisplayOrder).FirstOrDefaultAsync();

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
                            var msgResult = await _messageFactory.SendGiftCardNotificationAsync(gc, customerLanguage.Id);
                            // INFO: QueuedEmail.Id may be 0 here because CheckOrderStatusAsync uses scope commit.
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
            Guard.NotNull(order);

            var prevOrderStatus = order.OrderStatus;
            if (prevOrderStatus == status)
            {
                return;
            }

            order.OrderStatusId = (int)status;

            // Save new order status.
            await _db.SaveChangesAsync();

            order.AddOrderNote(T("Admin.OrderNotice.OrderStatusChanged", _localizationService.GetLocalizedEnum(status)));

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
            var rewardPointsAwarded = order.OrderStatus == _rewardPointsSettings.PointsForPurchases_Awarded;
            var rewardPointsCanceled = order.OrderStatus == _rewardPointsSettings.PointsForPurchases_Canceled;

            if (rewardPointsAwarded || rewardPointsCanceled)
            {
                await LoadNavigationProperties(order, true, false);

                ApplyRewardPoints(order, rewardPointsCanceled);
            }

            // Gift cards activation.
            await ActivateGiftCardsAsync(order);

            // Update order.
            await _db.SaveChangesAsync();
        }

        protected virtual async Task CheckOrderStatusAsync(Order order)
        {
            Guard.NotNull(order);

            using (var scope = new DbContextScope(_db, deferCommit: true))
            {
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

                await scope.CommitAsync();
            }

            if (order.OrderStatus != OrderStatus.Cancelled &&
                order.OrderStatus != OrderStatus.Complete &&
                order.PaymentStatus == PaymentStatus.Paid &&
                (order.ShippingStatus == ShippingStatus.ShippingNotRequired || order.ShippingStatus == ShippingStatus.Delivered))
            {
                // INFO: SetOrderStatusAsync performs commit. Exclude from scope commit because QueuedEmail.Id is required for messages.
                await SetOrderStatusAsync(order, OrderStatus.Complete, true);
            }
        }

        private async Task<int> SumUpQuantity(OrderItem orderItem, Func<Shipment, bool> predicate)
        {
            await _db.LoadReferenceAsync(orderItem, x => x.Order, false, q => q
                .Include(x => x.Shipments)
                .ThenInclude(x => x.ShipmentItems));

            var result = 0;
            var shipments = predicate != null
                ? orderItem.Order.Shipments.Where(predicate)
                : orderItem.Order.Shipments;

            foreach (var shipment in shipments)
            {
                var item = shipment.ShipmentItems.FirstOrDefault(x => x.OrderItemId == orderItem.Id);
                if (item != null)
                {
                    result += item.Quantity;
                }
            }

            return result;
        }

        private async Task LoadNavigationProperties(Order order, bool includeCustomer = false, bool includeShipments = false)
        {
            // Resolve customer related navigation properties.
            if (includeCustomer)
            {
                await _db.LoadReferenceAsync(order, x => x.RedeemedRewardPointsEntry);

                await _db.LoadReferenceAsync(order, x => x.Customer, false, q => q
                    .AsSplitQuery()
                    .Include(x => x.RewardPointsHistory)
                    .Include(x => x.CustomerRoleMappings)
                    .ThenInclude(x => x.CustomerRole));
            }

            // Lazy load all order items in one go. Optionally also resolve more required navigation properties.
            await _db.LoadCollectionAsync(order, x => x.OrderItems, false, q =>
            {
                q = q.Include(x => x.Product);

                if (includeShipments)
                {
                    q = q.AsSplitQuery()
                        .Include(x => x.Order.Shipments)
                        .ThenInclude(x => x.ShipmentItems);
                }

                return q;
            });
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Server.IIS.Core;
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
using Smartstore.Events;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderProcessingService : IOrderProcessingService
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizationService _localizationService;
        private readonly ICurrencyService _currencyService;
        private readonly IPaymentService _paymentService;
        private readonly IProductService _productService;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IMessageFactory _messageFactory;
        private readonly IEventPublisher _eventPublisher;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly OrderSettings _orderSettings;
        private readonly LocalizationSettings _localizationSettings;
        private readonly Currency _primaryCurrency;

        public OrderProcessingService(
            SmartDbContext db,
            ILocalizationService localizationService,
            ICurrencyService currencyService,
            IPaymentService paymentService,
            IProductService productService,
            IOrderCalculationService orderCalculationService,
            IShoppingCartService shoppingCartService,
            IMessageFactory messageFactory,
            IEventPublisher eventPublisher,
            RewardPointsSettings rewardPointsSettings,
            OrderSettings orderSettings,
            LocalizationSettings localizationSettings)
        {
            _db = db;
            _localizationService = localizationService;
            _currencyService = currencyService;
            _paymentService = paymentService;
            _productService = productService;
            _orderCalculationService = orderCalculationService;
            _shoppingCartService = shoppingCartService;
            _messageFactory = messageFactory;
            _eventPublisher = eventPublisher;
            _rewardPointsSettings = rewardPointsSettings;
            _orderSettings = orderSettings;
            _localizationSettings = localizationSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual Task<PlaceOrderResult> PlaceOrderAsync(ProcessPaymentRequest processPaymentRequest, Dictionary<string, string> extraData)
        {
            throw new NotImplementedException();
        }


        public virtual async Task ShipAsync(Shipment shipment, bool notifyCustomer)
        {
            Guard.NotNull(shipment, nameof(shipment));

            var order = await _db.Orders.FindByIdAsync(shipment.OrderId);
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

            await _db.SaveChangesAsync();

            if (notifyCustomer)
            {
                var msg = await _messageFactory.SendShipmentSentCustomerNotificationAsync(shipment, order.CustomerLanguageId);
                if (msg?.Email?.Id != null)
                {
                    order.AddOrderNote(T("Admin.OrderNotice.CustomerShippedEmailQueued", msg.Email.Id));
                    await _db.SaveChangesAsync();
                }
            }

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

            await _db.SaveChangesAsync();

            if (notifyCustomer)
            {
                var msg = await _messageFactory.SendShipmentDeliveredCustomerNotificationAsync(shipment, order.CustomerLanguageId);
                if (msg?.Email?.Id != null)
                {
                    order.AddOrderNote(T("Admin.OrderNotice.CustomerDeliveredEmailQueued", msg.Email.Id));
                    await _db.SaveChangesAsync();
                }
            }

            await CheckOrderStatusAsync(order);
        }

        public virtual bool CanCancelOrder(Order order)
        {
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
                .AsNoTracking()
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

        public virtual bool CanCancelRecurringPayment(Customer customerToValidate, RecurringPayment recurringPayment)
        {
            if (customerToValidate == null || recurringPayment == null)
                return false;

            var initialOrder = recurringPayment.InitialOrder;
            if (initialOrder == null)
                return false;

            var customer = recurringPayment.InitialOrder.Customer;
            if (customer == null)
                return false;

            if (initialOrder.OrderStatus == OrderStatus.Cancelled)
                return false;

            if (!customerToValidate.IsAdmin())
            {
                if (customer.Id != customerToValidate.Id)
                    return false;
            }

            if (!recurringPayment.NextPaymentDate.HasValue)
                return false;

            return true;
        }

        public virtual async Task<IList<string>> CancelRecurringPaymentAsync(RecurringPayment recurringPayment)
        {
            Guard.NotNull(recurringPayment, nameof(recurringPayment));

            var initialOrder = recurringPayment.InitialOrder;
            if (initialOrder == null)
            {
                return new List<string> { T("Order.InitialOrderDoesNotExistForRecurringPayment") };
            }

            var request = new CancelRecurringPaymentRequest();
            CancelRecurringPaymentResult result = null;

            try
            {
                request.Order = initialOrder;
                result = await _paymentService.CancelRecurringPaymentAsync(request);

                if (result.Success)
                {
                    recurringPayment.IsActive = false;

                    initialOrder.AddOrderNote(T("Admin.OrderNotice.RecurringPaymentCancelled"));
                    await _db.SaveChangesAsync();

                    await _messageFactory.SendRecurringPaymentCancelledStoreOwnerNotificationAsync(recurringPayment, _localizationSettings.DefaultAdminLanguageId);
                }
            }
            catch (Exception ex)
            {
                result ??= new();
                result.Errors.Add(ex.ToAllMessages());
            }

            if (result.Errors.Any())
            {
                ProcessErrors(initialOrder, result.Errors, "Admin.OrderNotice.RecurringPaymentCancellationError");
                await _db.SaveChangesAsync();
            }

            return result.Errors;
        }

        public virtual async Task ProcessNextRecurringPaymentAsync(RecurringPayment recurringPayment)
        {
            Guard.NotNull(recurringPayment, nameof(recurringPayment));

            try
            {
                if (!recurringPayment.IsActive)
                    throw new SmartException(T("Payment.RecurringPaymentNotActive"));

                var initialOrder = recurringPayment.InitialOrder;
                if (initialOrder == null)
                    throw new SmartException(T("Order.InitialOrderDoesNotExistForRecurringPayment"));

                var customer = initialOrder.Customer;
                if (customer == null)
                    throw new SmartException(T("Customer.DoesNotExist"));

                var nextPaymentDate = recurringPayment.NextPaymentDate;
                if (!nextPaymentDate.HasValue)
                    throw new SmartException(T("Payment.CannotCalculateNextPaymentDate"));

                var paymentInfo = new ProcessPaymentRequest
                {
                    StoreId = initialOrder.StoreId,
                    CustomerId = customer.Id,
                    OrderGuid = Guid.NewGuid(),
                    IsRecurringPayment = true,
                    InitialOrderId = initialOrder.Id,
                    RecurringCycleLength = recurringPayment.CycleLength,
                    RecurringCyclePeriod = recurringPayment.CyclePeriod,
                    RecurringTotalCycles = recurringPayment.TotalCycles,
                };

                var result = await PlaceOrderAsync(paymentInfo, new());

                if (result.Success)
                {
                    if (result.PlacedOrder == null)
                        throw new SmartException(T("Order.NotFound", "".NaIfEmpty()));

                    recurringPayment.RecurringPaymentHistory.Add(new RecurringPaymentHistory
                    {
                        RecurringPayment = recurringPayment,
                        CreatedOnUtc = DateTime.UtcNow,
                        OrderId = result.PlacedOrder.Id
                    });
                    
                    await _db.SaveChangesAsync();
                }
                else if (result.Errors.Count > 0)
                {
                    throw new SmartException(string.Join(" ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                Logger.ErrorsAll(ex);
                throw;
            }
        }

        public virtual bool CanMarkOrderAsAuthorized(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderStatus == OrderStatus.Cancelled)
                return false;

            if (order.PaymentStatus == PaymentStatus.Pending)
                return true;

            return false;
        }

        public virtual async Task MarkAsAuthorizedAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            order.PaymentStatusId = (int)PaymentStatus.Authorized;

            order.AddOrderNote(T("Admin.OrderNotice.OrderMarkedAsAuthorized"));
            await _db.SaveChangesAsync();

            await CheckOrderStatusAsync(order);
        }

        public virtual bool CanCompleteOrder(Order order)
        {
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

            await _db.SaveChangesAsync();

            await CheckOrderStatusAsync(order);
        }

        public virtual async Task<bool> CanCaptureAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderStatus == OrderStatus.Cancelled || order.OrderStatus == OrderStatus.Pending)
                return false;

            if (order.PaymentStatus == PaymentStatus.Authorized && await _paymentService.SupportCaptureAsync(order.PaymentMethodSystemName))
                return true;

            return false;
        }

        public virtual async Task<IList<string>> CaptureAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (!await CanCaptureAsync(order))
            {
                throw new SmartException(T("Order.CannotCapture"));
            }

            var request = new CapturePaymentRequest();
            CapturePaymentResult result = null;

            try
            {
                request.Order = order;
                result = await _paymentService.CaptureAsync(request);

                if (result.Success)
                {
                    var paidDate = result.NewPaymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : order.PaidDateUtc;

                    order.CaptureTransactionId = result.CaptureTransactionId;
                    order.CaptureTransactionResult = result.CaptureTransactionResult;
                    order.PaymentStatus = result.NewPaymentStatus;
                    order.PaidDateUtc = paidDate;

                    order.AddOrderNote(T("Admin.OrderNotice.OrderCaptured"));
                    await _db.SaveChangesAsync();

                    await CheckOrderStatusAsync(order);

                    if (order.PaymentStatus == PaymentStatus.Paid)
                    {
                        await _eventPublisher.PublishOrderPaidAsync(order);
                    }
                }
            }
            catch (Exception ex)
            {
                result ??= new();
                result.Errors.Add(ex.ToAllMessages());
            }

            if (result.Errors.Any())
            {
                ProcessErrors(order, result.Errors, "Admin.OrderNotice.OrderCaptureError");
                await _db.SaveChangesAsync();
            }

            return result.Errors;
        }

        public virtual bool CanMarkOrderAsPaid(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (order.OrderStatus == OrderStatus.Cancelled)
                return false;

            if (order.PaymentStatus == PaymentStatus.Paid ||
                order.PaymentStatus == PaymentStatus.Refunded ||
                order.PaymentStatus == PaymentStatus.Voided)
                return false;

            return true;
        }

        public virtual async Task MarkOrderAsPaidAsync(Order order)
        {
            Guard.NotNull(order, nameof(order));

            if (!CanMarkOrderAsPaid(order))
            {
                throw new SmartException(T("Order.CannotMarkPaid"));
            }

            order.PaymentStatusId = (int)PaymentStatus.Paid;
            order.PaidDateUtc = DateTime.UtcNow;

            order.AddOrderNote(T("Admin.OrderNotice.OrderMarkedAsPaid"));
            await _db.SaveChangesAsync();

            await CheckOrderStatusAsync(order);

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                await _eventPublisher.PublishOrderPaidAsync(order);
            }
        }

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

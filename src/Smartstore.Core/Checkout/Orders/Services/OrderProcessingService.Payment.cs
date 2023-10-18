using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderProcessingService : IOrderProcessingService
    {
        public virtual async Task MarkAsAuthorizedAsync(Order order)
        {
            Guard.NotNull(order);

            order.PaymentStatusId = (int)PaymentStatus.Authorized;

            order.AddOrderNote(T("Admin.OrderNotice.OrderMarkedAsAuthorized"));

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
        }

        public virtual async Task MarkOrderAsPaidAsync(Order order)
        {
            Guard.NotNull(order);

            if (!order.CanMarkOrderAsPaid())
            {
                throw new PaymentException(T("Order.CannotMarkPaid"));
            }

            order.PaymentStatusId = (int)PaymentStatus.Paid;
            order.PaidDateUtc = DateTime.UtcNow;

            order.AddOrderNote(T("Admin.OrderNotice.OrderMarkedAsPaid"));

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                await _eventPublisher.PublishOrderPaidAsync(order);
            }
        }

        public virtual async Task<bool> CanCaptureAsync(Order order)
        {
            Guard.NotNull(order);

            if (order.OrderStatus == OrderStatus.Cancelled || order.OrderStatus == OrderStatus.Pending)
                return false;

            if (order.PaymentStatus == PaymentStatus.Authorized && await _paymentService.SupportCaptureAsync(order.PaymentMethodSystemName))
                return true;

            return false;
        }

        public virtual async Task CaptureAsync(Order order)
        {
            Guard.NotNull(order);

            if (!await CanCaptureAsync(order))
            {
                throw new PaymentException(T("Order.CannotCapture"));
            }

            try
            {
                var result = await _paymentService.CaptureAsync(new()
                {
                    Order = order
                });

                order.CaptureTransactionId = result.CaptureTransactionId;
                order.CaptureTransactionResult = result.CaptureTransactionResult.Truncate(400);
                order.PaymentStatus = result.NewPaymentStatus;
                order.PaidDateUtc = result.NewPaymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : order.PaidDateUtc;

                order.AddOrderNote(T("Admin.OrderNotice.OrderCaptured"));

                // INFO: CheckOrderStatus performs commit.
                await CheckOrderStatusAsync(order);

                if (order.PaymentStatus == PaymentStatus.Paid)
                {
                    await _eventPublisher.PublishOrderPaidAsync(order);
                }
            }
            catch (Exception ex)
            {
                await AddPaymentFailureNote(ex, order, "Admin.OrderNotice.OrderCaptureError");
                ex.ReThrow();
            }
        }

        public virtual async Task<bool> CanRefundAsync(Order order)
        {
            Guard.NotNull(order);

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

        public virtual async Task RefundAsync(Order order)
        {
            Guard.NotNull(order);

            if (!await CanRefundAsync(order))
            {
                throw new PaymentException(T("Order.CannotRefund"));
            }

            try
            {
                var request = new RefundPaymentRequest
                {
                    Order = order,
                    AmountToRefund = new Money(order.OrderTotal, _primaryCurrency),
                    IsPartialRefund = false
                };

                var result = await _paymentService.RefundAsync(request);
                var totalAmountRefunded = order.RefundedAmount + order.OrderTotal;

                order.RefundedAmount = totalAmountRefunded;
                order.PaymentStatus = result.NewPaymentStatus;

                order.AddOrderNote(T("Admin.OrderNotice.OrderRefunded", request.AmountToRefund.ToString(true)));

                // INFO: CheckOrderStatus performs commit.
                await CheckOrderStatusAsync(order);
            }
            catch (Exception ex)
            {
                await AddPaymentFailureNote(ex, order, "Admin.OrderNotice.OrderRefundError");
                ex.ReThrow();
            }
        }

        public virtual async Task RefundOfflineAsync(Order order)
        {
            Guard.NotNull(order);

            if (!order.CanRefundOffline())
            {
                throw new PaymentException(T("Order.CannotRefund"));
            }

            var amountToRefund = new Money(order.OrderTotal, _primaryCurrency);
            var totalAmountRefunded = order.RefundedAmount + amountToRefund.Amount;

            order.RefundedAmount = totalAmountRefunded;
            order.PaymentStatus = PaymentStatus.Refunded;

            order.AddOrderNote(T("Admin.OrderNotice.OrderMarkedAsRefunded", amountToRefund.ToString(true)));

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
        }

        public virtual async Task<bool> CanPartiallyRefundAsync(Order order, decimal amountToRefund)
        {
            Guard.NotNull(order);

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

        public virtual async Task PartiallyRefundAsync(Order order, decimal amountToRefund)
        {
            Guard.NotNull(order);

            if (!await CanPartiallyRefundAsync(order, amountToRefund))
            {
                throw new PaymentException(T("Order.CannotPartialRefund"));
            }

            try
            {
                var request = new RefundPaymentRequest
                {
                    Order = order,
                    AmountToRefund = new Money(amountToRefund, _primaryCurrency),
                    IsPartialRefund = true
                };

                var result = await _paymentService.RefundAsync(request);

                var totalAmountRefunded = order.RefundedAmount + amountToRefund;
                order.RefundedAmount = totalAmountRefunded;
                order.PaymentStatus = result.NewPaymentStatus;

                order.AddOrderNote(T("Admin.OrderNotice.OrderPartiallyRefunded", request.AmountToRefund.ToString(true)));

                // INFO: CheckOrderStatus performs commit.
                await CheckOrderStatusAsync(order);
            }
            catch (Exception ex)
            {
                await AddPaymentFailureNote(ex, order, "Admin.OrderNotice.OrderPartiallyRefundError");
                ex.ReThrow();
            }
        }

        public virtual async Task PartiallyRefundOfflineAsync(Order order, decimal amountToRefund)
        {
            Guard.NotNull(order);

            if (!order.CanPartiallyRefundOffline(amountToRefund))
            {
                throw new PaymentException(T("Order.CannotPartialRefund"));
            }

            var totalAmountRefunded = order.RefundedAmount + amountToRefund;
            order.RefundedAmount = totalAmountRefunded;
            order.PaymentStatus = PaymentStatus.PartiallyRefunded;

            var formattedAmount = new Money(amountToRefund, _primaryCurrency).ToString(true);
            order.AddOrderNote(T("Admin.OrderNotice.OrderMarkedAsPartiallyRefunded", formattedAmount));

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
        }

        public virtual async Task<bool> CanVoidAsync(Order order)
        {
            Guard.NotNull(order);

            if (order.OrderTotal == decimal.Zero)
                return false;

            // Uncomment the lines below in order to allow this operation for cancelled orders.
            //if (order.OrderStatus == OrderStatus.Cancelled)
            //    return false;

            if (order.PaymentStatus == PaymentStatus.Authorized && await _paymentService.SupportVoidAsync(order.PaymentMethodSystemName))
                return true;

            return false;
        }

        public virtual async Task VoidAsync(Order order)
        {
            Guard.NotNull(order);

            if (!await CanVoidAsync(order))
            {
                throw new PaymentException(T("Order.CannotVoid"));
            }

            try
            {
                var request = new VoidPaymentRequest
                {
                    Order = order
                };

                var result = await _paymentService.VoidAsync(request);

                order.PaymentStatus = result.NewPaymentStatus;
                order.AddOrderNote(T("Admin.OrderNotice.OrderVoided"));

                // INFO: CheckOrderStatus performs commit.
                await CheckOrderStatusAsync(order);
            }
            catch (Exception ex)
            {
                await AddPaymentFailureNote(ex, order, "Admin.OrderNotice.OrderVoidError");
                ex.ReThrow();
            }
        }

        public virtual async Task VoidOfflineAsync(Order order)
        {
            Guard.NotNull(order);

            if (!order.CanVoidOffline())
            {
                throw new PaymentException(T("Order.CannotVoid"));
            }

            order.PaymentStatusId = (int)PaymentStatus.Voided;
            order.AddOrderNote(T("Admin.OrderNotice.OrderMarkedAsVoided"));

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
        }

        public virtual async Task<bool> CanCancelRecurringPaymentAsync(RecurringPayment recurringPayment, Customer customerToValidate)
        {
            Guard.NotNull(recurringPayment);
            Guard.NotNull(customerToValidate);

            await _db.LoadReferenceAsync(recurringPayment, x => x.InitialOrder, false, q => q.Include(x => x.Customer));
            await _db.LoadCollectionAsync(customerToValidate, x => x.CustomerRoleMappings, false, q => q.Include(x => x.CustomerRole));

            var initialOrder = recurringPayment.InitialOrder;
            var customer = initialOrder?.Customer;

            if (customer == null || initialOrder == null || initialOrder.OrderStatus == OrderStatus.Cancelled)
            {
                return false;
            }

            if (customer.Id != customerToValidate.Id && !customerToValidate.IsAdmin())
            {
                return false;
            }

            var nextPaymentDate = await _paymentService.GetNextRecurringPaymentDateAsync(recurringPayment);
            return nextPaymentDate.HasValue;
        }

        public virtual async Task CancelRecurringPaymentAsync(RecurringPayment recurringPayment)
        {
            Guard.NotNull(recurringPayment);

            await _db.LoadReferenceAsync(recurringPayment, x => x.InitialOrder, false, q => q.Include(x => x.Customer));

            var initialOrder = recurringPayment.InitialOrder ?? throw new PaymentException(T("Order.InitialOrderDoesNotExistForRecurringPayment"));
            try
            {
                var request = new CancelRecurringPaymentRequest
                {
                    Order = initialOrder
                };

                await _paymentService.CancelRecurringPaymentAsync(request);

                recurringPayment.IsActive = false;

                initialOrder.AddOrderNote(T("Admin.OrderNotice.RecurringPaymentCancelled"));
                await _db.SaveChangesAsync();

                await _messageFactory.SendRecurringPaymentCancelledStoreOwnerNotificationAsync(recurringPayment, _localizationSettings.DefaultAdminLanguageId);
            }
            catch (Exception ex)
            {
                await AddPaymentFailureNote(ex, initialOrder, "Admin.OrderNotice.RecurringPaymentCancellationError");
                ex.ReThrow();
            }
        }

        public virtual async Task ProcessNextRecurringPaymentAsync(RecurringPayment recurringPayment)
        {
            Guard.NotNull(recurringPayment);

            if (!recurringPayment.IsActive)
            {
                throw new PaymentException(T("Payment.RecurringPaymentNotActive"));
            }

            await _db.LoadReferenceAsync(recurringPayment, x => x.InitialOrder, false, q => q.Include(x => x.Customer));

            var initialOrder = recurringPayment.InitialOrder ?? throw new PaymentException(T("Order.InitialOrderDoesNotExistForRecurringPayment"));
            var customer = initialOrder.Customer ?? throw new Exception(T("Customer.DoesNotExist"));
            var nextPaymentDate = await _paymentService.GetNextRecurringPaymentDateAsync(recurringPayment);

            if (!nextPaymentDate.HasValue)
            {
                throw new PaymentException(T("Payment.CannotCalculateNextPaymentDate"));
            }

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
                {
                    throw new Exception(T("Order.NotFound", StringExtensions.NotAvailable));
                }

                recurringPayment.RecurringPaymentHistory.Add(new()
                {
                    RecurringPayment = recurringPayment,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = result.PlacedOrder.Id
                });

                await _db.SaveChangesAsync();
            }
            else if (result.Errors.Count > 0)
            {
                throw new Exception(string.Join(" ", result.Errors));
            }
        }

        private Task AddPaymentFailureNote(Exception ex, Order order, string messageKey)
        {
            order.AddOrderNote(T(messageKey, order.GetOrderNumber()).ToString() + " " + ex.Message);
            return _db.SaveChangesAsync();
        }
    }
}

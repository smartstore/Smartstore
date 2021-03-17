using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Common;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Checkout.Orders
{
    public partial class OrderProcessingService : IOrderProcessingService
    {
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

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
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

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);

            if (order.PaymentStatus == PaymentStatus.Paid)
            {
                await _eventPublisher.PublishOrderPaidAsync(order);
            }
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

            CapturePaymentResult result = null;

            try
            {
                var request = new CapturePaymentRequest
                {
                    Order = order
                };

                result = await _paymentService.CaptureAsync(request);

                if (result.Success)
                {
                    var paidDate = result.NewPaymentStatus == PaymentStatus.Paid ? DateTime.UtcNow : order.PaidDateUtc;

                    order.CaptureTransactionId = result.CaptureTransactionId;
                    order.CaptureTransactionResult = result.CaptureTransactionResult;
                    order.PaymentStatus = result.NewPaymentStatus;
                    order.PaidDateUtc = paidDate;

                    order.AddOrderNote(T("Admin.OrderNotice.OrderCaptured"));

                    // INFO: CheckOrderStatus performs commit.
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

            RefundPaymentResult result = null;

            try
            {
                var request = new RefundPaymentRequest
                {
                    Order = order,
                    AmountToRefund = new Money(order.OrderTotal, _primaryCurrency),
                    IsPartialRefund = false
                };

                result = await _paymentService.RefundAsync(request);

                if (result.Success)
                {
                    var totalAmountRefunded = order.RefundedAmount + order.OrderTotal;
                    order.RefundedAmount = totalAmountRefunded;
                    order.PaymentStatus = result.NewPaymentStatus;

                    order.AddOrderNote(T("Admin.OrderNotice.OrderRefunded", request.AmountToRefund.ToString(true)));

                    // INFO: CheckOrderStatus performs commit.
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

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
        }

        public virtual async Task<bool> CanPartiallyRefundAsync(Order order, decimal amountToRefund)
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

        public virtual async Task<IList<string>> PartiallyRefundAsync(Order order, decimal amountToRefund)
        {
            Guard.NotNull(order, nameof(order));

            if (!await CanPartiallyRefundAsync(order, amountToRefund))
            {
                throw new SmartException(T("Order.CannotPartialRefund"));
            }

            RefundPaymentResult result = null;

            try
            {
                var request = new RefundPaymentRequest
                {
                    Order = order,
                    AmountToRefund = new Money(amountToRefund, _primaryCurrency),
                    IsPartialRefund = true
                };

                result = await _paymentService.RefundAsync(request);

                if (result.Success)
                {
                    var totalAmountRefunded = order.RefundedAmount + amountToRefund;
                    order.RefundedAmount = totalAmountRefunded;
                    order.PaymentStatus = result.NewPaymentStatus;

                    order.AddOrderNote(T("Admin.OrderNotice.OrderPartiallyRefunded", request.AmountToRefund.ToString(true)));

                    // INFO: CheckOrderStatus performs commit.
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

        public virtual bool CanPartiallyRefundOffline(Order order, decimal amountToRefund)
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

        public virtual async Task PartiallyRefundOfflineAsync(Order order, decimal amountToRefund)
        {
            Guard.NotNull(order, nameof(order));

            if (!CanPartiallyRefundOffline(order, amountToRefund))
            {
                throw new SmartException(T("Order.CannotPartialRefund"));
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

            VoidPaymentResult result = null;

            try
            {
                var request = new VoidPaymentRequest
                {
                    Order = order
                };

                result = await _paymentService.VoidAsync(request);

                if (result.Success)
                {
                    order.PaymentStatus = result.NewPaymentStatus;
                    order.AddOrderNote(T("Admin.OrderNotice.OrderVoided"));

                    // INFO: CheckOrderStatus performs commit.
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

            // INFO: CheckOrderStatus performs commit.
            await CheckOrderStatusAsync(order);
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

            CancelRecurringPaymentResult result = null;

            try
            {
                var request = new CancelRecurringPaymentRequest
                {
                    Order = initialOrder
                };

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
    }
}

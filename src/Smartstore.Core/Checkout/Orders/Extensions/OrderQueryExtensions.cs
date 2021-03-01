using System;
using System.Linq;
using Smartstore.Core.Checkout.Orders;

namespace Smartstore
{
    public static class OrderQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter and sorts by <see cref="Order.CreatedOnUtc"/> descending.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <param name="customerId">Customer identifier.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Order query.</returns>
        public static IOrderedQueryable<Order> ApplyStandardFilter(this IQueryable<Order> query, int? customerId = null, int? storeId = null)
        {
            Guard.NotNull(query, nameof(query));

            if (customerId.HasValue)
            {
                query = query.Where(x => x.CustomerId == customerId.Value);
            }

            if (storeId.HasValue)
            {
                query = query.Where(x => x.StoreId == storeId.Value);
            }

            return query.OrderByDescending(o => o.CreatedOnUtc);
        }

        /// <summary>
        /// Applies a status filter.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <param name="orderStatusIds">Order status identifiers.</param>
        /// <param name="paymentStatusIds">Payment status identifiers.</param>
        /// <param name="shippingStatusIds">Shipping status identifiers.</param>
        /// <returns>Order query.</returns>
        public static IQueryable<Order> ApplyStatusFilter(this IQueryable<Order> query, int[] orderStatusIds = null, int[] paymentStatusIds = null, int[] shippingStatusIds = null)
        {
            Guard.NotNull(query, nameof(query));

            if (orderStatusIds?.Any() ?? false)
            {
                query = query.Where(x => orderStatusIds.Contains(x.OrderStatusId));
            }

            if (paymentStatusIds?.Any() ?? false)
            {
                query = query.Where(x => paymentStatusIds.Contains(x.PaymentStatusId));
            }

            if (shippingStatusIds?.Any() ?? false)
            {
                query = query.Where(x => shippingStatusIds.Contains(x.ShippingStatusId));
            }

            return query;
        }

        /// <summary>
        /// Applies a filter for payment methods and transaction\capture identifier.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <param name="paymentMethodSystemNames">System name of payment methods.</param>
        /// <param name="authorizationTransactionId">Authorization transaction identifier. Typically a foreign identifier provided by the payment provider.</param>
        /// <param name="captureTransactionId">Capture transaction identifier. Typically a foreign identifier provided by the payment provider.</param>
        /// <returns>Order query.</returns>
        public static IQueryable<Order> ApplyPaymentFilter(
            this IQueryable<Order> query,
            string[] paymentMethodSystemNames = null,
            string authorizationTransactionId = null,
            string captureTransactionId = null)
        {
            Guard.NotNull(query, nameof(query));

            if (authorizationTransactionId.HasValue())
            {
                query = query.Where(x => x.AuthorizationTransactionId == authorizationTransactionId);
            }

            if (captureTransactionId.HasValue())
            {
                query = query.Where(x => x.CaptureTransactionId == captureTransactionId);
            }

            if (paymentMethodSystemNames?.Any() ?? false)
            {
                query = query.Where(x => paymentMethodSystemNames.Contains(x.PaymentMethodSystemName));
            }

            return query;
        }

        /// <summary>
        /// Applies a filter for billing data.
        /// </summary>
        /// <param name="query">Order query.</param>
        /// <param name="billingEmail">Email of billing address.</param>
        /// <param name="billingName">First or last name of billing address.</param>
        /// <returns>Order query.</returns>
        public static IQueryable<Order> ApplyBillingFilter(this IQueryable<Order> query, string billingEmail = null, string billingName = null)
        {
            Guard.NotNull(query, nameof(query));

            if (billingEmail.HasValue())
            {
                query = query.Where(x => x.BillingAddress != null && !string.IsNullOrEmpty(x.BillingAddress.Email) && x.BillingAddress.Email.Contains(billingEmail));
            }

            if (billingName.HasValue())
            {
                query = query.Where(x => x.BillingAddress != null && (
                    (!string.IsNullOrEmpty(x.BillingAddress.LastName) && x.BillingAddress.LastName.Contains(billingName)) ||
                    (!string.IsNullOrEmpty(x.BillingAddress.FirstName) && x.BillingAddress.FirstName.Contains(billingName))
                ));
            }

            return query;
        }
    }
}

using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Payment
{
    public static partial class RecurringPaymentQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter and sorts by <see cref="RecurringPayment.StartDateUtc"/>, then by <see cref="BaseEntity.Id"/>./>
        /// </summary>
        /// <param name="query">Recurring payment query.</param>
        /// <param name="initialOrderId">Initial order identifier.</param>
        /// <param name="customerId">Customer identifier.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden records.</param>
        /// <returns>Recurring payment query.</returns>
        public static IOrderedQueryable<RecurringPayment> ApplyStandardFilter(
            this IQueryable<RecurringPayment> query,
            int? initialOrderId = null,
            int? customerId = null,
            int? storeId = null,
            bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));

            query = query.Include(x => x.InitialOrder);

            if (initialOrderId > 0)
            {
                query = query.Where(x => x.InitialOrderId == initialOrderId.Value);
            }

            if (customerId > 0)
            {
                query = query.Where(x => x.InitialOrder.CustomerId == customerId.Value);
            }

            if (storeId > 0)
            {
                query = query.Where(x => x.InitialOrder.StoreId == storeId.Value);
            }

            if (!includeHidden)
            {
                var db = query.GetDbContext<SmartDbContext>();

                // Join to exclude deleted customers.
                query =
                    from rp in query
                    join c in db.Customers.AsNoTracking() on rp.InitialOrder.CustomerId equals c.Id
                    select rp;

                query = query.Where(x => x.IsActive && !x.InitialOrder.Deleted);
            }

            return query.OrderBy(x => x.StartDateUtc).ThenBy(x => x.Id);
        }

        public static IQueryable<RecurringPayment> IncludeAddresses(this IQueryable<RecurringPayment> query)
        {
            Guard.NotNull(query, nameof(query));

            query = query
                .AsSplitQuery()
                .Include(x => x.InitialOrder).ThenInclude(x => x.Customer).ThenInclude(x => x.BillingAddress)
                .Include(x => x.InitialOrder).ThenInclude(x => x.Customer).ThenInclude(x => x.ShippingAddress)
                .Include(x => x.RecurringPaymentHistory);

            return query;
        }
    }
}

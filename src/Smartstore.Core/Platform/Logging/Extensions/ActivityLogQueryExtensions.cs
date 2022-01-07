using Smartstore.Core.Data;
using Smartstore.Core.Rules.Filters;

namespace Smartstore.Core.Logging
{
    public static class ActivityLogQueryExtensions
    {
        public static IQueryable<ActivityLog> ApplyDateFilter(this IQueryable<ActivityLog> query, DateTime? fromUtc, DateTime? toUtc)
        {
            Guard.NotNull(query, nameof(query));

            if (fromUtc.HasValue)
                query = query.Where(x => fromUtc.Value <= x.CreatedOnUtc);

            if (toUtc.HasValue)
                query = query.Where(x => toUtc.Value >= x.CreatedOnUtc);

            return query;
        }

        public static IQueryable<ActivityLog> ApplyCustomerFilter(this IQueryable<ActivityLog> query, string email = null, bool? customerSystemAccount = null)
        {
            Guard.NotNull(query, nameof(query));

            if (email.HasValue() || customerSystemAccount.HasValue)
            {
                var queryCustomers = query.GetDbContext<SmartDbContext>().Customers.AsQueryable();

                if (email.HasValue())
                    queryCustomers = queryCustomers.ApplySearchFilterFor(x => x.Email, email);

                if (customerSystemAccount.HasValue)
                    queryCustomers = queryCustomers.Where(x => x.IsSystemAccount == customerSystemAccount.Value);

                query =
                    from al in query.GetDbContext<SmartDbContext>().ActivityLogs
                    join c in queryCustomers on al.CustomerId equals c.Id
                    select al;
            }

            return query;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Core.Data;
using Smartstore.Core.Logging;

namespace Smartstore
{
    public static class ActivityLogQueryExtensions
    {
        public static IQueryable<ActivityLog> ApplyDateFilter(this IQueryable<ActivityLog> query, DateTime? from, DateTime? to)
        {
            Guard.NotNull(query, nameof(query));

            if (from.HasValue)
                query = query.Where(x => from.Value <= x.CreatedOnUtc);

            if (to.HasValue)
                query = query.Where(x => to.Value >= x.CreatedOnUtc);

            return query;
        }

        public static IQueryable<ActivityLog> ApplyCustomerFilter(this IQueryable<ActivityLog> query, string email = null, bool? customerSystemAccount = null)
        {
            Guard.NotNull(query, nameof(query));

            if (email.HasValue() || customerSystemAccount.HasValue)
            {
                var queryCustomers = query.GetDbContext<SmartDbContext>().Customers.AsQueryable();

                if (email.HasValue())
                    queryCustomers = queryCustomers.Where(x => x.Email.Contains(email));

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
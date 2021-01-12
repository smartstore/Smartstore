using System.Linq;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;

namespace Smartstore.Core.Messages
{
    public static class NewsletterSubscriptionQueryExtensions
    {
        /// <summary>
        /// Applies filter by <see cref="NewsletterSubscription.Email"/> and <see cref="NewsletterSubscription.StoreId"/> and validates mail address.
        /// </summary>
        public static IQueryable<NewsletterSubscription> ApplyMailAddressFilter(this IQueryable<NewsletterSubscription> query, string mail, int storeId = 0)
        {
            Guard.NotNull(query, nameof(query));

            if (mail.HasValue())
            {
                mail = mail.Trim();

                if (!mail.IsEmail())
                    return null;

                query = query.Where(x => x.Email.Contains(mail));

                if (storeId > 0)
                {
                    query = query.Where(x => x.StoreId == storeId);
                }
            }

            return query;
        }

        /// <summary>
        /// Applies standard filter and sorts by <see cref="NewsletterSubscription.Email"/>, then by <see cref="NewsletterSubscription.StoreId"/>.
        /// </summary>
        /// <param name="query">Newsletter subscription query.</param>
        /// <param name="includeHidden">Applies filter by <see cref="NewsletterSubscription.Active"/>.</param>
        /// <param name="customerRolesIds">Customer roles identifiers to apply filter by customer role restrictions.</param>
        /// <param name="storeId">Store identifier to apply filter by store restriction.</param>
        /// <returns>NewsletterSubscriber query.</returns>
        public static IOrderedQueryable<NewsletterSubscriber> ApplyStandardFilter(this IQueryable<NewsletterSubscription> query,
            string mail,
            bool includeHidden = false, 
            int[] storeIds = null,
            int[] customerRolesIds = null)
        {
            Guard.NotNull(query, nameof(query));

            var db = query.GetDbContext<SmartDbContext>();

            var joinedQuery =
                from ns in query
                join c in db.Customers.AsNoTracking() on ns.Email equals c.Email into customers
                from c in customers.DefaultIfEmpty()
                select new NewsletterSubscriber
                {
                    Subscription = ns,
                    Customer = c
                };

            if (mail.HasValue())
            {
                joinedQuery = joinedQuery.Where(x => x.Subscription.Email.Contains(mail));
            }

            if (!includeHidden)
            {
                joinedQuery = joinedQuery.Where(x => x.Subscription.Active);
            }

            // TODO: (mh) (core) > We can't use ApplyStoreFilter here because NewsletterSubscriber aint't IStoreRestricted | REMOVE THIS COMMENT.
            if (storeIds?.Any() ?? false)
            {
                joinedQuery = joinedQuery.Where(x => storeIds.Contains(x.Subscription.StoreId));
            }

            // TODO: (mh) (core) > We can't use ApplyAclFilter here because NewsletterSubscriber aint't IAclRestricted | REMOVE THIS COMMENT.
            if (customerRolesIds?.Any() ?? false)
            {
                joinedQuery = joinedQuery.Where(x => x.Customer.CustomerRoleMappings
                    .Where(rm => rm.CustomerRole.Active)
                    .Select(rm => rm.CustomerRoleId)
                    .Intersect(customerRolesIds).Any());
            }

            return joinedQuery
                .OrderBy(x => x.Subscription.Email)
                .ThenBy(x => x.Subscription.StoreId);
        }
    }
}

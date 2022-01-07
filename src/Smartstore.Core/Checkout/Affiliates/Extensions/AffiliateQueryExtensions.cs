using Smartstore.Core.Checkout.Affiliates;

namespace Smartstore
{
    public static class AffiliateQueryExtensions
    {
        /// <summary>
        /// Applies standard filter to affliate query. Orders query by <see cref="Affiliate.AddressId"/>
        /// </summary>
        public static IOrderedQueryable<Affiliate> ApplyStandardFilter(this IQueryable<Affiliate> query, bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));

            if (!includeHidden)
            {
                query = query.Where(x => x.Active);
            }

            return query.OrderBy(x => x.AddressId);
        }
    }
}

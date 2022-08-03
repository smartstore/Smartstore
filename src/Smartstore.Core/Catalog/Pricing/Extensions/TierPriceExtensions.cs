using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Pricing
{
    public static partial class TierPriceExtensions
    {
        /// <summary>
        /// Filter tier prices by a store.
        /// </summary>
        /// <param name="source">Tier prices.</param>
        /// <param name="storeId">Store identifier.</param>
        /// <returns>Filtered tier prices.</returns>
        public static IEnumerable<TierPrice> FilterByStore(this IEnumerable<TierPrice> source, int storeId)
        {
            Guard.NotNull(source, nameof(source));

            if (storeId == 0)
            {
                return source.Where(x => x.StoreId == 0);
            }
            else
            {
                return source.Where(x => x.StoreId == 0 || x.StoreId == storeId);
            }
        }

        /// <summary>
        /// Filter tier prices by customer.
        /// </summary>
        /// <param name="source">Tier prices.</param>
        /// <param name="customer">Customer entity.</param>
        /// <returns>Filtered tier prices.</returns>
        public static IEnumerable<TierPrice> FilterForCustomer(this IEnumerable<TierPrice> source, Customer customer)
        {
            Guard.NotNull(source, nameof(source));

            foreach (var tierPrice in source)
            {
                // Check customer role requirement.
                if (tierPrice.CustomerRole != null)
                {
                    if (customer == null)
                        continue;

                    var customerRoles = customer.CustomerRoleMappings
                        .Select(x => x.CustomerRole)
                        .Where(cr => cr.Active);

                    if (!customerRoles.Any())
                        continue;

                    var roleIsFound = false;

                    foreach (var customerRole in customerRoles)
                    {
                        if (customerRole == tierPrice.CustomerRole)
                            roleIsFound = true;
                    }

                    if (!roleIsFound)
                        continue;
                }

                yield return tierPrice;
            }
        }

        /// <summary>
        /// Remove duplicated quantities (leave only a tier price with minimum price).
        /// </summary>
        /// <param name="source">Tier prices.</param>
        /// <returns>Filtered tier prices.</returns>
        public static ICollection<TierPrice> RemoveDuplicatedQuantities(this ICollection<TierPrice> source)
        {
            Guard.NotNull(source, nameof(source));

            // Find duplicates.
            var items =
                from tierPrice in source
                group tierPrice by tierPrice.Quantity into g
                where g.Count() > 1
                select new { Quantity = g.Key, TierPrices = g.ToList() };

            foreach (var item in items)
            {
                // Find a tier price with minimum price (we'll not remove it).
                var minTierPrice = item.TierPrices.Aggregate((tp1, tp2) => tp1.Price < tp2.Price ? tp1 : tp2);

                // Remove all other records.
                item.TierPrices.Remove(minTierPrice);
                item.TierPrices.ForEach(x => source.Remove(x));
            }

            return source;
        }
    }
}

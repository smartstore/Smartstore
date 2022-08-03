using Smartstore.Core.Stores;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Checkout attribute query extensions.
    /// </summary>
    public static partial class CheckoutAttributeQueryExtensions
    {
        /// <summary>
        /// Standard filter for checkout attributes.
        /// Applies store filter, may include hidden (<see cref="CheckoutAttribute.IsActive"/>) attributes 
        /// and sorts by <see cref="CheckoutAttribute.DisplayOrder"/>.
        /// </summary>
        public static IOrderedQueryable<CheckoutAttribute> ApplyStandardFilter(
            this IQueryable<CheckoutAttribute> query,
            bool includeHidden = false,
            int storeId = 0)
        {
            Guard.NotNull(query, nameof(query));

            if (!includeHidden)
            {
                query = query.Where(x => x.IsActive);
            }

            if (storeId > 0)
            {
                query = query.ApplyStoreFilter(storeId);
            }

            return query.OrderBy(x => x.DisplayOrder);
        }
    }
}
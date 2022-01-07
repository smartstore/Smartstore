using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Core.Checkout.Attributes
{
    public static partial class CheckoutAttributeValueQueryExtensions
    {
        /// <summary>
        /// Applies a filter for list control types and sorts by <see cref="CheckoutAttribute.DisplayOrder"/>, 
        /// then by <see cref="CheckoutAttributeValue.DisplayOrder"/>.
        /// Only checkout attributes of list types (e.g. dropdown list) can have assigned <see cref="CheckoutAttributeValue"/> entities.
        /// </summary>
        /// <param name="query">Checkout attribute value query.</param>
        /// <returns>Checkout attribute value query.</returns>
        public static IOrderedQueryable<CheckoutAttributeValue> ApplyListTypeFilter(this IQueryable<CheckoutAttributeValue> query)
        {
            Guard.NotNull(query, nameof(query));

            query = query.Where(x => ProductAttributeMaterializer.AttributeListControlTypeIds.Contains(x.CheckoutAttribute.AttributeControlTypeId));

            return query
                .OrderBy(x => x.CheckoutAttribute.DisplayOrder)
                .ThenBy(x => x.DisplayOrder);
        }
    }
}

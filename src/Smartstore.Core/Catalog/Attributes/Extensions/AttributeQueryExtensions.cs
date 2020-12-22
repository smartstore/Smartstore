using System.Linq;

namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class AttributeQueryExtensions
    {
        /// <summary>
        /// Apply standard filter for a product variant combinations query.
        /// </summary>
        /// <param name="query">Product variant combinations query.</param>
        /// <param name="includeHidden">Applies filter by <see cref="ProductVariantAttributeCombination.Product.Published"/> and <see cref="ProductVariantAttributeCombination.IsActive"/>.</param>
        /// <returns>Product variant combinations query.</returns>
        public static IQueryable<ProductVariantAttributeCombination> ApplyStandardFilter(this IQueryable<ProductVariantAttributeCombination> query, bool includeHidden = false)
        {
            Guard.NotNull(query, nameof(query));

            if (!includeHidden)
            {
                query = query.Where(x => x.Product.Published && x.IsActive);
            }

            return query;
        }
    }
}

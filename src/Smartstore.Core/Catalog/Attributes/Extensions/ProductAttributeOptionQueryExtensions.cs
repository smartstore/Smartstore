namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class ProductAttributeOptionQueryExtensions
    {
        /// <summary>
        /// Applies a standard filter and sorts by <see cref="ProductAttributeOption.DisplayOrder"/>, then by <see cref="ProductAttributeOption.Name"/>.
        /// </summary>
        /// <param name="query">Product attribute option query.</param>
        /// <param name="optionsSetId">Options set identifier.</param>
        /// <returns>Product attribute option query.</returns>
        public static IOrderedQueryable<ProductAttributeOption> ApplyStandardFilter(this IQueryable<ProductAttributeOption> query, int? optionsSetId = null)
        {
            Guard.NotNull(query, nameof(query));

            if (optionsSetId > 0)
            {
                query = query.Where(x => x.ProductAttributeOptionsSetId == optionsSetId.Value);
            }

            return query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);
        }
    }
}

using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Attributes
{
    public static partial class ProductSpecificationAttributeQueryExtensions
    {
        /// <summary>
        /// Applies a filter for products and sorts by <see cref="ProductSpecificationAttribute.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product specification attribute query.</param>
        /// <param name="productIds">Identifiers of products to be filtered.</param>
        /// <param name="allowFiltering">A value indicating whether to filter by <see cref="ProductSpecificationAttribute.AllowFiltering"/>.</param>
        /// <param name="showOnProductPage">A value indicating whether to filter by <see cref="ProductSpecificationAttribute.ShowOnProductPage"/>.</param>
        /// <returns>Product specification attribute query.</returns>
        public static IOrderedQueryable<ProductSpecificationAttribute> ApplyProductsFilter(
            this IQueryable<ProductSpecificationAttribute> query,
            int[] productIds,
            bool? allowFiltering = null,
            bool? showOnProductPage = null)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(productIds, nameof(productIds));

            if (allowFiltering.HasValue || showOnProductPage.HasValue)
            {
                var db = query.GetDbContext<SmartDbContext>();

                var joinedQuery =
                    from psa in query
                    join sao in db.SpecificationAttributeOptions.AsNoTracking() on psa.SpecificationAttributeOptionId equals sao.Id
                    where productIds.Contains(psa.ProductId)
                    select new
                    {
                        ProductAttribute = psa,
                        Attribute = sao.SpecificationAttribute
                    };

                if (allowFiltering.HasValue)
                {
                    joinedQuery = joinedQuery.Where(x =>
                        (x.ProductAttribute.AllowFiltering == null && x.Attribute.AllowFiltering == allowFiltering.Value) ||
                        (x.ProductAttribute.AllowFiltering != null && x.ProductAttribute.AllowFiltering == allowFiltering.Value)
                    );
                }

                if (showOnProductPage.HasValue)
                {
                    joinedQuery = joinedQuery.Where(x =>
                        (x.ProductAttribute.ShowOnProductPage == null && x.Attribute.ShowOnProductPage == showOnProductPage.Value) ||
                        (x.ProductAttribute.ShowOnProductPage != null && x.ProductAttribute.ShowOnProductPage == showOnProductPage.Value)
                    );
                }

                return joinedQuery
                    .Select(x => x.ProductAttribute)
                    .OrderBy(x => x.DisplayOrder);
            }

            return query
                .Where(x => productIds.Contains(x.ProductId))
                .OrderBy(x => x.DisplayOrder);
        }
    }
}

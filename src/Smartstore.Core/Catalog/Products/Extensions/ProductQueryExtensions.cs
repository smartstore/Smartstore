using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class ProductQueryExtensions
    {
        #region Apply*

        /// <summary>
        /// Applies standard filter for a product query.
        /// Filters out <see cref="Product.IsSystemProduct"/>.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="includeHidden">Applies filter by <see cref="Product.Published"/>.</param>
        /// <returns>Product query.</returns>
        public static IQueryable<Product> ApplyStandardFilter(this IQueryable<Product> query, bool includeHidden = false)
        {
            Guard.NotNull(query);

            if (!includeHidden)
            {
                query = query.Where(x => x.Published);
            }

            query = query.Where(x => !x.IsSystemProduct);

            return query;
        }

        /// <summary>
        /// Applies a filter for system names.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="systemName">Product system name.</param>
        /// <returns>Product query.</returns>
        public static IQueryable<Product> ApplySystemNameFilter(this IQueryable<Product> query, string systemName)
        {
            Guard.NotNull(query);

            return query.Where(x => x.SystemName == systemName && x.IsSystemProduct);
        }

        /// <summary>
        /// Applies a filter for SKU and sorts by <see cref="Product.DisplayOrder"/>, then by <see cref="BaseEntity.Id"/>.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="sku">Stock keeping unit (SKU).</param>
        /// <returns>Ordered product query.</returns>
        public static IOrderedQueryable<Product> ApplySkuFilter(this IQueryable<Product> query, string sku)
        {
            Guard.NotNull(query);

            sku = sku.TrimSafe();

            query = query.Where(x => x.Sku == sku);

            return query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id);
        }

        /// <summary>
        /// Applies a filter for GTIN and sorts by <see cref="Product.DisplayOrder"/>, then by <see cref="BaseEntity.Id"/>.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="gtin">Global Trade Item Number (GTIN).</param>
        /// <returns>Ordered product query.</returns>
        public static IOrderedQueryable<Product> ApplyGtinFilter(this IQueryable<Product> query, string gtin)
        {
            Guard.NotNull(query);

            gtin = gtin.TrimSafe();

            query = query.Where(x => x.Gtin == gtin);

            return query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id);
        }

        /// <summary>
        /// Applies a filter for MPN and sorts by <see cref="Product.DisplayOrder"/>, then by <see cref="BaseEntity.Id"/>.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="manufacturerPartNumber">Manufacturer Part Number (MPN).</param>
        /// <returns>Ordered product query.</returns>
        public static IOrderedQueryable<Product> ApplyMpnFilter(this IQueryable<Product> query, string manufacturerPartNumber)
        {
            Guard.NotNull(query);

            manufacturerPartNumber = manufacturerPartNumber.TrimSafe();

            query = query.Where(x => x.ManufacturerPartNumber == manufacturerPartNumber);

            return query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id);
        }

        /// <summary>
        /// Applies a filter to find a product by its SKU, MPN or GTIN and sorts 
        /// by <see cref="Product.DisplayOrder"/>, then by <see cref="BaseEntity.Id"/>.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="code">A product code like SKU, MPN or GTIN.</param>
        /// <returns>Ordered product query.</returns>
        public static IOrderedQueryable<Product> ApplyProductCodeFilter(this IQueryable<Product> query, string code)
        {
            Guard.NotNull(query);
            Guard.NotEmpty(code);

            code = code.Trim();

            query = query.Where(x => x.Sku == code || x.ManufacturerPartNumber == code || x.Gtin == code);

            return query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id);
        }

        /// <summary>
        /// Applies a filter for associated products and sorts by <see cref="Product.ParentGroupedProductId"/>, then by <see cref="Product.DisplayOrder"/>.
        /// </summary>
        /// <param name="query">Product query.</param>
        /// <param name="groupedProductIds">Product identifiers of grouped products.</param>
        /// <param name="includeHidden">A value indicating whether to include hidden products.</param>
        /// <returns>Product query.</returns>
        public static IOrderedQueryable<Product> ApplyAssociatedProductsFilter(this IQueryable<Product> query, int[] groupedProductIds, bool includeHidden = false)
        {
            Guard.NotNull(query);
            Guard.NotNull(groupedProductIds);

            // Ignore multistore. Expect multistore setting for associated products is the same as for parent grouped product.
            query = query
                .Where(x => groupedProductIds.Contains(x.ParentGroupedProductId))
                .ApplyStandardFilter(includeHidden);

            return query
                .OrderBy(x => x.ParentGroupedProductId)
                .ThenBy(x => x.DisplayOrder);
        }

        /// <summary>
        /// Applies a filter that reads all products that are descendants of the category with the given <paramref name="treePath"/>.
        /// </summary>
        /// <param name="treePath">The parent's tree path to get descendant products from.</param>
        /// <param name="includeSelf"><c>true</c> = add the products of the parent category to the result list, <c>false</c> = ignore the parent category.</param>
        public static IQueryable<Product> ApplyDescendantsFilter(
            this IQueryable<Product> query,
            string treePath,
            bool includeSelf = false)
        {
            Guard.NotNull(query);
            Guard.NotEmpty(treePath);

            if (treePath.Length < 3 || (treePath[0] != '/' && treePath[^1] != '/'))
            {
                throw new ArgumentException("Invalid treePath format.", nameof(treePath));
            }

            query = 
                from p in query
                from pc in p.ProductCategories
                let c = pc.Category
                where c.TreePath.StartsWith(treePath) && (includeSelf || c.TreePath.Length > treePath.Length)
                select p;

            return query;
        }

        #endregion

        #region Include*

        /// <summary>
        /// Includes media for eager loading:
        /// <see cref="Product.ProductMediaFiles"/> (sorted by <see cref="ProductMediaFile.DisplayOrder"/>), then <see cref="ProductMediaFile.MediaFile"/>
        /// </summary>
        public static IIncludableQueryable<Product, MediaFile> IncludeMedia(this IQueryable<Product> query)
        {
            Guard.NotNull(query);

            return query.Include(x => x.ProductMediaFiles.OrderBy(y => y.DisplayOrder))
                .ThenInclude(x => x.MediaFile);
        }

        /// <summary>
        /// Includes manufacturers for eager loading:
        /// Published <see cref="Product.ProductManufacturers"/> (sorted by <see cref="Manufacturer.DisplayOrder"/> then by <see cref="Manufacturer.Name"/>), 
        /// then <see cref="ProductManufacturer.Manufacturer"/>, then <see cref="Manufacturer.MediaFile"/>
        /// </summary>
        public static IIncludableQueryable<Product, MediaFile> IncludeManufacturers(this IQueryable<Product> query)
        {
            Guard.NotNull(query);

            return query.Include(x => x.ProductManufacturers.OrderBy(y => y.DisplayOrder).ThenBy(x => x.Manufacturer.Name))
                .ThenInclude(x => x.Manufacturer)
                .ThenInclude(x => x.MediaFile);
        }

        /// <summary>
        /// Includes bundle items for eager loading:
        /// Published <see cref="Product.ProductBundleItems"/> (sorted by <see cref="ProductBundleItem.DisplayOrder"/>), 
        /// then <see cref="ProductBundleItem.BundleProduct"/>.
        /// </summary>
        public static IIncludableQueryable<Product, Product> IncludeBundleItems(this IQueryable<Product> query)
        {
            Guard.NotNull(query);

            return query.Include(x => x.ProductBundleItems
                .Where(y => y.Published)
                .OrderBy(y => y.DisplayOrder))
                .ThenInclude(x => x.BundleProduct);
        }

        /// <summary>
        /// Includes product reviews for eager loading:
        /// Approved <see cref="Product.ProductReviews"/> (sorted by <see cref="ProductBundleItem.CreatedOnUtc"/> desc), 
        /// then <see cref="CustomerContent.Customer"/>.
        /// </summary>
        public static IIncludableQueryable<Product, Customer> IncludeReviews(this IQueryable<Product> query)
        {
            Guard.NotNull(query);

            // INFO: using .Take(x) will fail due to TPH inheritance. EF bug?

            return query.Include(x => x.ProductReviews
                .Where(y => y.IsApproved)
                .OrderByDescending(y => y.CreatedOnUtc))
                .ThenInclude(x => x.Customer);
        }

        ///// <summary>
        ///// Includes a bunch of navigation properties for eager loading:
        ///// <list type="bullet">
        /////     <item><see cref="Product.ProductMediaFiles"/> (sorted by <see cref="ProductMediaFile.DisplayOrder"/>), then <see cref="ProductMediaFile.MediaFile"/></item>
        /////     <item>
        /////         Published <see cref="Product.ProductManufacturers"/> (sorted by <see cref="Manufacturer.DisplayOrder"/> then by <see cref="Manufacturer.Name"/>), 
        /////         then <see cref="ProductManufacturer.Manufacturer"/>, then <see cref="Manufacturer.MediaFile"/>
        /////     </item>
        /////     <item><see cref="Product.ProductSpecificationAttributes"/>, then <see cref="ProductSpecificationAttribute.SpecificationAttributeOption"/>, then <see cref="SpecificationAttributeOption.SpecificationAttribute"/></item>
        /////     <item><see cref="Product.ProductVariantAttributes"/>, then <see cref="ProductVariantAttribute.ProductAttribute"/> and <see cref="ProductVariantAttribute.ProductVariantAttributeValues"/></item>
        /////     <item><see cref="Product.ProductVariantAttributeCombinations"/>, then <see cref="ProductVariantAttributeCombination.DeliveryTime"/></item>
        /////     <item><see cref="Product.TierPrices"/> (sorted by <see cref="TierPrice.Quantity"/>)</item>
        /////     <item>Published <see cref="Product.ProductBundleItems"/> (sorted by <see cref="ProductBundleItem.DisplayOrder"/>), then <see cref="ProductBundleItem.BundleProduct"/></item>
        ///// </list>
        ///// </summary>
        //public static IQueryable<Product> IncludeMega(this IQueryable<Product> query)
        //{
        //    Guard.NotNull(query, nameof(query));

        //    var megaInclude = query
        //        .Include(x => x.ProductMediaFiles
        //            .OrderBy(y => y.DisplayOrder))
        //            .ThenInclude(x => x.MediaFile)
        //        //.Include(x => x.DeliveryTime) // Is DB cached anyway
        //        //.Include(x => x.QuantityUnit)  // Is DB cached anyway
        //        .Include(x => x.ProductManufacturers
        //            .Where(y => y.Manufacturer.Published)
        //            .OrderBy(y => y.DisplayOrder).ThenBy(y => y.Manufacturer.Name))
        //            .ThenInclude(x => x.Manufacturer)
        //            .ThenInclude(x => x.MediaFile)
        //        .Include(x => x.ProductSpecificationAttributes)
        //            .ThenInclude(x => x.SpecificationAttributeOption)
        //            .ThenInclude(x => x.SpecificationAttribute)
        //        .Include(x => x.ProductTags)
        //        .Include(x => x.ProductVariantAttributes)
        //            .ThenInclude(x => x.ProductAttribute)
        //        .Include(x => x.ProductVariantAttributes)
        //            .ThenInclude(x => x.ProductVariantAttributeValues)
        //        .Include(x => x.ProductVariantAttributeCombinations)
        //            .ThenInclude(x => x.QuantityUnit)
        //        .Include(x => x.ProductVariantAttributeCombinations)
        //            .ThenInclude(x => x.DeliveryTime)
        //        .Include(x => x.TierPrices
        //            .OrderBy(x => x.Quantity))
        //        .Include(x => x.ProductBundleItems
        //            .Where(y => y.Published)
        //            .OrderBy(y => y.DisplayOrder));

        //    return megaInclude;
        //}

        #endregion
    }
}

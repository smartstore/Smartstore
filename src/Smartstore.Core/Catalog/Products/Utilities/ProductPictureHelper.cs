using System.Runtime.CompilerServices;
using Smartstore.Collections;
using Smartstore.Core.Data;

namespace Smartstore.Core.Catalog.Products.Utilities
{
    public static class ProductPictureHelper
    {
        /// <summary>
        /// Fixes 'MainPictureId' property of a single product entity.
        /// </summary>
        /// <param name="db">Database context.</param>
        /// <param name="product">Product to fix.</param>
        /// <param name="entities">When <c>null</c>, <see cref="Product.ProductMediaFiles"/> gets called.</param>
        /// <returns><c>true</c> when value was fixed.</returns>
        public static bool FixProductMainPictureId(SmartDbContext db, Product product, IEnumerable<ProductMediaFile> entities = null)
        {
            Guard.NotNull(product, nameof(product));

            // INFO: this method must be able to handle pre-save state also.

            entities ??= product.ProductMediaFiles;
            if (entities == null)
            {
                return false;
            }

            var transientEntities = entities.Where(x => x.Id == 0);

            var sortedEntities = entities
                // Remove transient entities.
                .Except(transientEntities)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .Select(x => db.Entry(x))
                .Where(x => x.State != EfState.Deleted && x.State != EfState.Detached)
                .Select(x => x.Entity)
                // Added/transient entities must be appended.
                .Concat(transientEntities.OrderBy(x => x.DisplayOrder));

            var newMainPictureId = sortedEntities.FirstOrDefault()?.MediaFileId;

            if (newMainPictureId != product.MainPictureId)
            {
                product.MainPictureId = newMainPictureId;
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<int> FixProductMainPictureIds(SmartDbContext db, DateTime? ifModifiedSinceUtc = null)
         => FixProductMainPictureIds(db, false, ifModifiedSinceUtc);

        /// <summary>
        /// Called from migration seeder and only processes product entities without MainPictureId value.
        /// </summary>
        /// <param name="db">Database context.</param>
        /// <returns>The total count of fixed and updated product entities.</returns>
        internal static async Task<int> FixProductMainPictureIds(SmartDbContext db, bool initial, DateTime? ifModifiedSinceUtc = null)
        {
            var query =
                from p in db.Products.AsNoTracking()
                where (!initial || p.MainPictureId == null) && (ifModifiedSinceUtc == null || p.UpdatedOnUtc >= ifModifiedSinceUtc.Value)
                orderby p.Id
                select new { p.Id, p.MainPictureId };

            // Key = ProductId, Value = MainPictureId
            var toUpdate = new Dictionary<int, int?>();

            // 1st pass
            var pageIndex = -1;
            while (true)
            {
                var products = await query.ToPagedList(++pageIndex, 500).LoadAsync();
                var map = await GetPoductPictureMap(db, products.Select(x => x.Id).ToArray());

                foreach (var p in products)
                {
                    int? fixedPictureId = null;
                    if (map.ContainsKey(p.Id))
                    {
                        // Product has still a pic.
                        fixedPictureId = map[p.Id].FirstOrDefault();
                    }

                    // Update only if fixed PictureId differs from current
                    if (fixedPictureId != p.MainPictureId)
                    {
                        toUpdate.Add(p.Id, fixedPictureId);
                    }
                }

                if (!products.HasNextPage)
                {
                    break;
                }
            }

            // 2nd pass.
            foreach (var chunk in toUpdate.Chunk(1000))
            {
                using var transaction = await db.Database.BeginTransactionAsync();

                foreach (var kvp in chunk)
                {
                    db.Database.ExecuteSqlRaw("Update Product Set MainPictureId = {0} WHERE Id = {1}", kvp.Value, kvp.Key);
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }

            return toUpdate.Count;
        }

        private static async Task<Multimap<int, int>> GetPoductPictureMap(SmartDbContext db, IEnumerable<int> productIds)
        {
            var query =
                from x in db.ProductMediaFiles.AsNoTracking()
                where productIds.Contains(x.ProductId)
                orderby x.ProductId, x.DisplayOrder
                select new { x.ProductId, x.MediaFileId };

            var files = await query.ToListAsync();
            var map = files.ToMultimap(x => x.ProductId, x => x.MediaFileId);
            return map;
        }
    }
}

using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Smartstore.Collections;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Core.Catalog.Products.Utilities
{
    public static partial class ProductPictureHelper
    {
        #region MainPictureId

        /// <summary>
        /// Fixes 'MainPictureId' property of a single product entity.
        /// </summary>
        /// <param name="db">Database context.</param>
        /// <param name="product">Product to fix.</param>
        /// <param name="entities">When <c>null</c>, <see cref="Product.ProductMediaFiles"/> gets called.</param>
        /// <returns><c>true</c> when value was fixed.</returns>
        public static bool FixProductMainPictureId(SmartDbContext db, Product product, IEnumerable<ProductMediaFile> entities = null)
        {
            Guard.NotNull(product);

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
                    //db.Database.ExecuteSqlRaw("Update Product Set MainPictureId = {0} WHERE Id = {1}", kvp.Value, kvp.Key);
                    await db.Products
                        .Where(x => x.Id == kvp.Key)
                        .ExecuteUpdateAsync(
                            x => x.SetProperty(p => p.MainPictureId, p => kvp.Value));
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

        #endregion

        #region Outsourcing

        //[GeneratedRegex(@"(?<=<img[^>]*src\s*=\s*['""])data:image\/(?<format>[a-z]+);base64,(?<data>[^'""]+)(?=['""][^>]*>)")]
        [GeneratedRegex(@"src\s*=\s*['""](data:image\/(?<format>[a-z]+);base64,(?<data>[^'""]+))['""]")]
        private static partial Regex EmbeddedImagesRegex();
        private static readonly Regex _rgEmbeddedImages = EmbeddedImagesRegex();

        /// <summary>
        /// Loads all products whose long description contains at least one embedded base64 image and 
        /// replaces the base64 images with paths after moving them to the media storage.
        /// This is a long running process. Please create a database backup before calling this function.
        /// </summary>
        /// <param name="take">Max products to process.</param>
        /// <returns>The processing result</returns>
        public static async Task<OffloadImageResult> OffloadEmbeddedImages(SmartDbContext db, IMediaService mediaService, int take = 200)
        {
            Guard.IsPositive(take);
            
            var folderPath = "file/outsourced";
            var dbSet = db.Products.IgnoreQueryFilters();
            var allIds = dbSet
                .Where(x => x.FullDescription.Contains("src=\"data:image/"))
                .OrderBy(x => x.Id)
                .Select(x => x.Id)
                .ToPagedList(0, take);

            await allIds.LoadAsync();

            if (allIds.Any())
            {
                mediaService.ImagePostProcessingEnabled = false;
                
                if (!mediaService.FolderExists(folderPath))
                {
                    await mediaService.CreateFolderAsync(folderPath);
                }
            }

            var numAffectedProducts = await allIds.GetTotalCountAsync();
            var numProcessedProducts = 0;
            var numAttempted = 0;
            var numFailed = 0;
            var numSucceeded = 0;

            foreach (var chunk in allIds.Chunk(10))
            {
                // MUST be sync. Keep small for less RAM usage.
                var products = dbSet
                    .Where(x => chunk.Contains(x.Id))
                    .Select(x => new { x.Id, x.FullDescription })
                    .ToList();

                foreach (var p in products)
                {
                    var dirty = false;
                    numProcessedProducts++;
                    
                    var replaced = await _rgEmbeddedImages.ReplaceAsync(p.FullDescription, async match =>
                    {
                        numAttempted++;
                        
                        var format = match.Groups["format"].Value;
                        var data = match.Groups["data"].Value;
                        if (format == "jpeg")
                        {
                            format = "jpg";
                        }

                        string result = null;

                        try
                        {
                            using var stream = new MemoryStream(Convert.FromBase64String(data));

                            var fileName = $"p{p.Id.ToStringInvariant()}-{CommonHelper.GenerateRandomDigitCode(8)}.{format}";
                            var filePath = PathUtility.Join("file/outsourced", fileName);
                            var fileInfo = await mediaService.SaveFileAsync(filePath, stream, false);

                            result = fileInfo.Url;
                            dirty = true;
                            numSucceeded++;
                        }
                        catch
                        {
                            result = match.Value;
                            numFailed++;
                        }

                        return $"src=\"{result}\"";
                    });

                    if (dirty)
                    {
                        var numAffected = dbSet
                            .Where(x => x.Id == p.Id)
                            .ExecuteUpdate(
                                x => x.SetProperty(p => p.FullDescription, p => replaced));
                    }
                }
            }

            return new OffloadImageResult
            {
                NumAffectedProducts = numAffectedProducts,
                NumProcessedProducts = numProcessedProducts,
                NumAttempted = numAttempted,
                NumFailed = numFailed,
                NumSucceded = numSucceeded
            };
        }

        public class OffloadImageResult
        {
            public int NumAffectedProducts { get; set; }
            public int NumProcessedProducts { get; set; }
            public int NumAttempted { get; set; }
            public int NumFailed { get; set; }
            public int NumSucceded { get; set; }

            public override string ToString()
            {
                var sb = new StringBuilder();

                sb.AppendLine($"NumAffectedProducts: {NumAffectedProducts}");
                sb.AppendLine($"NumProcessedProducts: {NumProcessedProducts}");
                sb.AppendLine($"NumAttempted: {NumAttempted}");
                sb.AppendLine($"NumSucceded: {NumSucceded}");
                sb.AppendLine($"NumFailed: {NumFailed}");

                return sb.ToString();
            }
        }

        #endregion
    }
}

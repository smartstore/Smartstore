using Smartstore.Core.Catalog.Attributes;
using Smartstore.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Data.Migrations
{
    public class AttributesMigrator
    {
        private readonly SmartDbContext _db;
        private readonly ILogger _logger;

        public AttributesMigrator(SmartDbContext db, ILogger logger)
        {
            _db = Guard.NotNull(db);
            _logger = Guard.NotNull(logger);
        }

        /// <summary>
        /// Sets <see cref="ProductVariantAttributeCombination.HashCode"/> if it is 0.
        /// </summary>
        /// <returns>Number of updated <see cref="ProductVariantAttributeCombination"/>.</returns>
        public async Task<int> CreateAttributeCombinationHashCodesAsync(CancellationToken cancelToken = default)
        {
            const int take = 4000;
            // Avoid an infinite loop here under all circumstances. Process a maximum of 500,000,000 records.
            const int maxBatches = 500000000 / take;
            var numBatches = 0;
            var numWarnings = 0;
            var numSuccess = 0;
            var now = DateTime.UtcNow;

            var query = _db.ProductVariantAttributeCombinations
                .Where(x => x.HashCode == 0)
                .OrderBy(x => x.Id);

            // TODO: (mg) FastPager will definitely increase perf for large datasets.
            // TODO: (mg) HookImportance.Essential could slightly increase write performance. Check if possible.
            using (var scope = new DbContextScope(_db, autoDetectChanges: false, deferCommit: true, minHookImportance: HookImportance.Important))
            {
                do
                {
                    var combinations = await query.Take(take).ToListAsync(cancelToken);
                    if (combinations.Count == 0)
                    {
                        break;
                    }

                    foreach (var combination in combinations)
                    {
                        // INFO: by accidents the selection can be empty. In that case the hash code has the value 5381, not 0.
                        var hashCode = new ProductVariantAttributeSelection(combination.RawAttributes).GetHashCode();
                        if (hashCode == 0 && ++numWarnings < 10)
                        {
                            _logger.Warn($"The generated hash code for attribute combination {combination.Id} is 0.");
                        }

                        combination.HashCode = hashCode;
                    }

                    await scope.CommitAsync(cancelToken);
                    numSuccess += combinations.Count;

                    try
                    {
                        scope.DbContext.DetachEntities<ProductVariantAttributeCombination>();
                    }
                    catch
                    {
                    }
                }
                while (++numBatches < maxBatches && !cancelToken.IsCancellationRequested);
            }

            if (numSuccess > 0)
            {
                var elapsed = Math.Floor((DateTime.UtcNow - now).TotalSeconds);
                _logger.Info($"Added {numSuccess:N0} hash codes for attribute combinations. Elapsed: {elapsed:N0} sec.");
            }

            return numSuccess;
        }
    }
}

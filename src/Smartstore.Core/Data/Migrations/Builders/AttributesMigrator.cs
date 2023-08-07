using Smartstore.Core.Catalog.Attributes;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Utilities;

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
            const int pageSize = 4000;
            // Avoid an infinite loop here under all circumstances. Process a maximum of 500,000,000 records.
            const int maxBatches = 500000000 / pageSize;
            var numBatches = 0;
            var numWarnings = 0;
            var numSuccess = 0;
            var startDate = DateTime.UtcNow;

            var pager = _db.ProductVariantAttributeCombinations
                .Where(x => x.HashCode == 0)
                .ToFastPager(pageSize);

            using (var scope = new DbContextScope(_db, autoDetectChanges: false, deferCommit: true, minHookImportance: HookImportance.Essential))
            {
                while (++numBatches < maxBatches &&
                    !cancelToken.IsCancellationRequested &&
                    (await pager.ReadNextPageAsync<ProductVariantAttributeCombination>(cancelToken)).Out(out var combinations))
                {
                    foreach (var combination in combinations)
                    {
                        // INFO: by accidents the selection can be empty. In that case the hash code has the value 5381, not 0.
                        combination.HashCode = GetAttributesHashCodeSafe(combination);

                        if (combination.HashCode == 0 && ++numWarnings < 10)
                        {
                            _logger.Warn($"The generated hash code for attribute combination {combination.Id} is 0.");
                        }
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
            }

            if (numSuccess > 0)
            {
                _logger.Info($"Added {numSuccess:N0} hash codes for attribute combinations. Elapsed: {Prettifier.HumanizeTimeSpan(DateTime.UtcNow - startDate)}.");
            }

            return numSuccess;

            static int GetAttributesHashCodeSafe(ProductVariantAttributeCombination entity)
            {
                try
                {
                    return new ProductVariantAttributeSelection(entity.RawAttributes).GetHashCode();
                }
                catch
                {
                    return 0;
                }
            }
        }
    }
}

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
        /// Generates attribute combination hash codes and applies them to <see cref="ProductVariantAttributeCombination.HashCode"/>.
        /// These are required to find <see cref="ProductVariantAttributeCombination"/> entities.
        /// </summary>
        /// <param name="force">
        /// <c>true</c> to regenerate all hash codes.
        /// <c>false</c> to generate only missing hash codes (those whose value is 0).
        /// </param>
        /// <returns>Number of updated <see cref="ProductVariantAttributeCombination"/> entities.</returns>
        public async Task<int> CreateAttributeCombinationHashCodesAsync(bool force = false, CancellationToken cancelToken = default)
        {
            const int pageSize = 4000;
            // Avoid an infinite loop here under all circumstances. Process a maximum of 500,000,000 records.
            const int maxBatches = 500000000 / pageSize;
            var numBatches = 0;
            var numWarnings = 0;
            var numSuccess = 0;
            var startDate = DateTime.UtcNow;

            if (force)
            {
                // INFO: "Hard reset" hash codes, rather than just modifying pager query below.
                // This allows us to call this method several times if problems occur during execution.
                _ = await _db.ProductVariantAttributeCombinations
                    .Where(x => x.HashCode != 0)
                    .ExecuteUpdateAsync(x => x.SetProperty(pvac => pvac.HashCode, pvac => 0), cancelToken);
            }

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
                        combination.HashCode = CommonHelper.TryAction(() => new ProductVariantAttributeSelection(combination.RawAttributes).GetHashCode());

                        if (combination.HashCode == 0 && ++numWarnings < 10)
                        {
                            _logger.Warn($"The generated hash code for attribute combination {combination.Id} is 0.");
                        }
                    }

                    await scope.CommitAsync(cancelToken);
                    numSuccess += combinations.Count;

                    CommonHelper.TryAction(() => scope.DbContext.DetachEntities<ProductVariantAttributeCombination>());
                }
            }

            _logger.Info($"Generated {numSuccess:N0} hash codes for attribute combinations. Elapsed: {Prettifier.HumanizeTimeSpan(DateTime.UtcNow - startDate)}.");

            return numSuccess;
        }
    }
}

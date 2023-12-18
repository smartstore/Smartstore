using Smartstore.Core.Data;

namespace Smartstore.Core.Seo
{
    internal class UrlServiceBatchScope : Disposable, IUrlServiceBatchScope
    {
        private UrlService _urlService;
        private readonly SmartDbContext _db;
        private DbSet<UrlRecord> _dbSet;
        private readonly List<ValidateSlugResult> _batch = new();

        public UrlServiceBatchScope(UrlService urlService, SmartDbContext db = null)
        {
            _urlService = urlService.GetInstanceForBatching(db);
            _db = urlService._db;
            _dbSet = _db.UrlRecords;
        }

        public virtual async Task<string> GetActiveSlugAsync(int entityId, string entityName, int languageId)
        {
            Guard.NotEmpty(entityName);

            if (_urlService.TryGetPrefetchedActiveSlug(entityId, entityName, languageId, out var slug))
            {
                return slug;
            }

            // Don't involve cache as it could put high memory pressure on app.
            var query = _dbSet
                .ApplyEntityFilter(entityName, entityId, languageId, true)
                .Select(x => x.Slug);

            return (await query.FirstOrDefaultAsync()).EmptyNull();
        }

        public virtual void ApplySlugs(params ValidateSlugResult[] slugs)
        {
            _batch.AddRange(slugs);
        }

        public virtual async Task<int> CommitAsync(CancellationToken cancelToken = default)
        {
            if (_batch.Count == 0)
                return 0;

            var batch = await ValidateBatchAsync(_batch, cancelToken);

            var batchByEntityName = batch.ToMultimap(x => x.EntityName, x => x);

            foreach (var kvp in batchByEntityName)
            {
                var entityName = kvp.Key;
                var slugs = kvp.Value;
                var languageIds = slugs.Select(x => x.LanguageId ?? 0).Distinct().ToArray();
                var entityIds = slugs.Select(x => x.Source.Id).Where(x => x > 0).Distinct().ToArray();

                var collection = await _urlService.GetUrlRecordCollectionAsync(entityName, languageIds, entityIds, tracked: true);

                foreach (var slug in slugs)
                {
                    await _urlService.ApplySlugAsync(slug, collection, false);
                }
            }

            var numAffected = await _urlService._db.SaveChangesAsync(cancelToken);
            _batch.Clear();

            return numAffected;
        }

        private async Task<List<ValidateSlugResult>> ValidateBatchAsync(IList<ValidateSlugResult> batch, CancellationToken cancelToken = default)
        {
            var batch2 = batch.Where(x => x.Source != null && x.Slug.HasValue());

            var unvalidatedSlugsMap = batch2
                .Where(x => !x.WasValidated)
                .DistinctBy(x => x.Slug)
                .ToDictionary(x => x.Slug, x => x.Found, StringComparer.OrdinalIgnoreCase);

            var unvalidatedSlugs = unvalidatedSlugsMap.Keys;
            var foundRecords = await _dbSet.Where(x => unvalidatedSlugs.Contains(x.Slug)).ToListAsync(cancelToken);

            foundRecords.Each(x => unvalidatedSlugsMap[x.Slug] = x);
            _urlService._extraSlugLookup.Each(x => unvalidatedSlugsMap[x.Key] = x.Value);

            var validatedBatch = batch2
                .SelectAwait(async (slug) =>
                {
                    if (slug.WasValidated)
                    {
                        return slug;
                    }

                    ValidateSlugResult result;
                    var found = unvalidatedSlugsMap.Get(slug.Slug);
                    var foundIsSelf = UrlService.FoundRecordIsSelf(slug.Source, found, slug.LanguageId);
                    var isReserved = _urlService._routeHelper.IsReservedPath(slug.Slug);
                    var alreadyProcessed = _urlService._extraSlugLookup.ContainsKey(slug.Slug);

                    if (!foundIsSelf && (found != null || isReserved || alreadyProcessed))
                    {
                        // The existence of a UrlRecord instance for a given slug implies that the slug
                        // needs to run through the default validation process to ensure uniqueness.
                        result = await _urlService.ValidateSlugAsync(
                            slug.Source,
                            slug.Slug,
                            null,
                            slug.LanguageId.GetValueOrDefault() == 0,
                            slug.LanguageId).AsTask();
                    }
                    else
                    {
                        result = new ValidateSlugResult(slug)
                        {
                            WasValidated = true,
                            Found = found,
                            FoundIsSelf = true
                        };
                    }

                    //$"{slug.Source.Id} {slug.Slug} -> {result.Slug}: {foundIsSelf == false} {found != null} {isReserved} {alreadyProcessed}".Dump();

                    _urlService._extraSlugLookup[result.Slug] = new UrlRecord
                    {
                        EntityId = result.Source.Id,
                        EntityName = result.EntityName,
                        Slug = result.Slug,
                        LanguageId = result.LanguageId ?? 0,
                        IsActive = true
                    };

                    return result;
                });

            return await validatedBatch.AsyncToList(cancelToken);
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _urlService = null;
                _dbSet = null;
            }
        }
    }
}
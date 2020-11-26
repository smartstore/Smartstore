using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Core.Seo
{
    internal class UrlServiceBatchScope : Disposable, IUrlServiceBatchScope
    {
        private UrlService _urlService;
        private DbSet<UrlRecord> _dbSet;
        private readonly List<ValidateSlugResult> _batch = new();

        public UrlServiceBatchScope(UrlService urlService)
        {
            _urlService = urlService;
            _dbSet = urlService._db.UrlRecords;
        }

        public virtual async Task<string> GetActiveSlugAsync(int entityId, string entityName, int languageId)
        {
            Guard.NotEmpty(entityName, nameof(entityName));

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
            return numAffected;

            //return 0;
        }

        private async Task<List<ValidateSlugResult>> ValidateBatchAsync(IList<ValidateSlugResult> batch, CancellationToken cancelToken = default)
        {
            var batch2 = batch.Where(x => x.Source != null && x.Slug.HasValue());

            var unvalidatedSlugsMap = batch2
                .Where(x => !x.WasValidated)
                .Select(x => x.Slug)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x, x => (UrlRecord)null, StringComparer.OrdinalIgnoreCase);

            var unvalidatedSlugs = unvalidatedSlugsMap.Keys;

            var foundRecords = await _dbSet.Where(x => unvalidatedSlugs.Contains(x.Slug)).ToListAsync();
            foreach (var record in foundRecords)
            {
                unvalidatedSlugsMap[record.Slug] = record;
            }

            var validatedBatch = batch2
                .SelectAsync(async (slug) =>
                {
                    if (slug.WasValidated)
                    {
                        return slug;
                    }
                    
                    var mustValidate = _urlService._seoSettings.ReservedUrlRecordSlugs.Contains(slug.Slug);
                    if (!mustValidate)
                    {
                        mustValidate = unvalidatedSlugsMap.TryGetValue(slug.Slug, out var urlRecord)
                            && urlRecord != null
                            && !_urlService.FoundRecordIsSelf(slug.Source, urlRecord, slug.LanguageId);
                    }
                    
                    if (mustValidate)
                    {
                        // The existence of a UrlRecord instance for a given slug implies that the slug
                        // needs to run through the default validation process
                        // to ensure uniqueness.
                        return await _urlService.ValidateSlugAsync(slug.Source, slug.Slug, true, slug.LanguageId).AsTask();
                    }
                    else
                    {
                        return slug;
                    }
                });

            // Namespace conflict with EF otherwise
            return await Dasync.Collections.IAsyncEnumerableExtensions.ToListAsync(validatedBatch, cancelToken);
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
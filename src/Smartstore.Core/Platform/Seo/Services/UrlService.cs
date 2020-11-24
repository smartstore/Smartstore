using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Caching;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore.Core.Seo
{
    public partial class UrlService : AsyncDbSaveHook<UrlRecord>, IUrlService
    {
        /// <summary>
        /// 0 = segment (EntityName.IdRange), 1 = language id
        /// </summary>
        const string URLRECORD_SEGMENT_KEY = "urlrecord:segment:{0}-lang-{1}";
        const string URLRECORD_SEGMENT_PATTERN = "urlrecord:segment:{0}*";
        const string URLRECORD_ALL_ACTIVESLUGS_KEY = "urlrecord:all-active-slugs";

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cacheManager;
        private readonly SeoSettings _seoSettings;
        private readonly PerformanceSettings _performanceSettings;

        private readonly IDictionary<string, UrlRecord> _extraSlugLookup;
        private readonly IDictionary<string, UrlRecordCollection> _prefetchedCollections;
        private static int _lastCacheSegmentSize = -1;

        public UrlService(
            SmartDbContext db,
            ICacheManager cacheManager,
            SeoSettings seoSettings,
            PerformanceSettings performanceSettings)
        {
            _db = db;
            _cacheManager = cacheManager;
            _seoSettings = seoSettings;
            _performanceSettings = performanceSettings;

            _prefetchedCollections = new Dictionary<string, UrlRecordCollection>(StringComparer.OrdinalIgnoreCase);
            _extraSlugLookup = new Dictionary<string, UrlRecord>();

            ValidateCacheState();
        }

        #region Hook

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            return Task.FromResult(HookResult.Ok);
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var distinctEntries = entries
                .Select(x => x.Entity)
                .OfType<UrlRecord>()
                .Select(x => (x.EntityName, x.EntityId, x.LanguageId))
                .Distinct()
                .ToArray();

            foreach (var entry in distinctEntries)
            {
                await ClearCacheSegmentAsync(entry.EntityName, entry.EntityId, entry.LanguageId);
            }

            _extraSlugLookup.Clear();
        }

        #endregion

        #region Cache

        /// <summary>
        /// Gets the cache segment for a entity name, entity id and language id combination.
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="entityId"></param>
        /// <param name="languageId"></param>
        /// <returns></returns>
        protected virtual async Task<Dictionary<int, string>> GetCacheSegmentAsync(string entityName, int entityId, int languageId)
        {
            Guard.NotEmpty(entityName, nameof(entityName));

            var segmentKey = GetSegmentKeyPart(entityName, entityId, out var minEntityId, out var maxEntityId);
            var cacheKey = BuildCacheSegmentKey(segmentKey, languageId);

            return await _cacheManager.GetAsync(cacheKey, async (o) =>
            {
                o.ExpiresIn(TimeSpan.FromHours(8));

                var query = from ur in _db.UrlRecords.AsNoTracking()
                            where
                                ur.EntityId >= minEntityId &&
                                ur.EntityId <= maxEntityId &&
                                ur.EntityName == entityName &&
                                ur.LanguageId == languageId &&
                                ur.IsActive
                            orderby ur.Id descending
                            select ur;

                var urlRecords = await query.ToListAsync();

                var dict = new Dictionary<int, string>(urlRecords.Count);

                foreach (var ur in urlRecords)
                {
                    dict[ur.EntityId] = ur.Slug.EmptyNull();
                }

                return dict;
            }, independent: true);
        }

        /// <summary>
        /// Clears the cached segment from the cache
        /// </summary>
        protected virtual async Task ClearCacheSegmentAsync(string entityName, int entityId, int? languageId = null)
        {
            var segmentKey = GetSegmentKeyPart(entityName, entityId);

            if (languageId > 0)
            {
                await _cacheManager.RemoveAsync(BuildCacheSegmentKey(segmentKey, languageId.Value));
            }
            else
            {
                await _cacheManager.RemoveByPatternAsync(URLRECORD_SEGMENT_PATTERN.FormatInvariant(segmentKey));
            }

            // Always delete this (in case when LoadAllOnStartup is true)
            await _cacheManager.RemoveAsync(URLRECORD_ALL_ACTIVESLUGS_KEY);
        }

        private void ValidateCacheState()
        {
            // Ensure that after a segment size change the cache segments are invalidated.
            var size = _performanceSettings.CacheSegmentSize;
            var changed = _lastCacheSegmentSize == -1;

            if (size <= 0)
            {
                _performanceSettings.CacheSegmentSize = size = 1;
            }

            if (_lastCacheSegmentSize > 0 && _lastCacheSegmentSize != size)
            {
                _cacheManager.RemoveByPattern(URLRECORD_SEGMENT_PATTERN);
                changed = true;
            }

            if (changed)
            {
                Interlocked.Exchange(ref _lastCacheSegmentSize, size);
            }
        }

        private static string BuildCacheSegmentKey(string segment, int languageId)
        {
            return string.Format(URLRECORD_SEGMENT_KEY, segment, languageId);
        }

        private string GetSegmentKeyPart(string entityName, int entityId)
        {
            return GetSegmentKeyPart(entityName, entityId, out _, out _);
        }

        private string GetSegmentKeyPart(string entityName, int entityId, out int minId, out int maxId)
        {
            (minId, maxId) = entityId.GetRange(_performanceSettings.CacheSegmentSize);
            return (entityName + "." + minId.ToString()).ToLowerInvariant();
        }

        private static string GenerateKey(int entityId, string entityName, int languageId)
        {
            return entityId.ToStringInvariant() + '.' + entityName + '.' + languageId.ToStringInvariant();
        }

        #endregion

        #region IUrlService

        public virtual async Task<string> GetActiveSlugAsync(int entityId, string entityName, int languageId)
        {
            Guard.NotEmpty(entityName, nameof(entityName));
            
            if (_prefetchedCollections.TryGetValue(entityName, out var collection))
            {
                var cachedItem = collection.Find(languageId, entityId);
                if (cachedItem != null)
                {
                    return cachedItem.Slug.EmptyNull();
                }
            }

            string slug = null;

            if (_seoSettings.LoadAllUrlAliasesOnStartup)
            {
                var allActiveSlugs = await _cacheManager.GetAsync(URLRECORD_ALL_ACTIVESLUGS_KEY, async (o) =>
                {
                    o.ExpiresIn(TimeSpan.FromHours(8));
                    
                    var query = from x in _db.UrlRecords.AsNoTracking()
                                where x.IsActive
                                orderby x.Id descending
                                select x;

                    var items = await query.ToListAsync();
                    var result = items.ToDictionarySafe(
                        x => GenerateKey(x.EntityId, x.EntityName, x.LanguageId),
                        x => x.Slug,
                        StringComparer.OrdinalIgnoreCase);

                    return result;
                }, independent: true);

                var key = GenerateKey(entityId, entityName, languageId);
                if (!allActiveSlugs.TryGetValue(key, out slug))
                {
                    return string.Empty;
                }
            }
            else
            {
                var slugs = await GetCacheSegmentAsync(entityName, entityId, languageId);

                if (!slugs.TryGetValue(entityId, out slug))
                {
                    return string.Empty;
                }
            }

            return slug;
        }

        public virtual async Task PrefetchUrlRecordsAsync(string entityName, int[] languageIds, int[] entityIds, bool isRange = false, bool isSorted = false, bool tracked = false)
        {
            var collection = await GetUrlRecordCollectionInternalAsync(entityName, languageIds, entityIds, isRange, isSorted, tracked);

            if (_prefetchedCollections.TryGetValue(entityName, out var existing))
            {
                collection.MergeWith(existing);
            }
            else
            {
                _prefetchedCollections[entityName] = collection;
            }
        }

        public Task<UrlRecordCollection> GetUrlRecordCollectionAsync(string entityName, int[] languageIds, int[] entityIds, bool isRange = false, bool isSorted = false, bool tracked = false)
        {
            return GetUrlRecordCollectionInternalAsync(entityName, languageIds, entityIds, isRange, isSorted, tracked);
        }

        protected virtual async Task<UrlRecordCollection> GetUrlRecordCollectionInternalAsync(string entityName, int[] languageIds, int[] entityIds, bool isRange, bool isSorted, bool tracked)
        {
            Guard.NotEmpty(entityName, nameof(entityName));

            var query = from x in _db.UrlRecords.ApplyTracking(tracked)
                        where x.EntityName == entityName && x.IsActive
                        select x;

            var requestedSet = entityIds;

            if (entityIds != null && entityIds.Length > 0)
            {
                if (isRange)
                {
                    if (!isSorted)
                    {
                        Array.Sort(entityIds);
                    }

                    var min = entityIds[0];
                    var max = entityIds[entityIds.Length - 1];

                    if (entityIds.Length == 2 && max > min + 1)
                    {
                        // Only min & max were passed, create the range sequence.
                        requestedSet = Enumerable.Range(min, max - min + 1).ToArray();
                    }

                    query = query.Where(x => x.EntityId >= min && x.EntityId <= max);
                }
                else
                {
                    requestedSet = entityIds;
                    query = query.Where(x => entityIds.Contains(x.EntityId));
                }
            }

            if (languageIds != null && languageIds.Length > 0)
            {
                if (languageIds.Length == 1)
                {
                    // Avoid "The LINQ expression node type 'ArrayIndex' is not supported in LINQ to Entities".
                    var languageId = languageIds[0];
                    query = query.Where(x => x.LanguageId == languageId);
                }
                else
                {
                    query = query.Where(x => languageIds.Contains(x.LanguageId));
                }
            }

            // Don't sort DESC, because latter items overwrite exisiting ones (it's the same as sorting DESC and taking the first)
            var items = await query.OrderBy(x => x.Id).ToListAsync();
            return new UrlRecordCollection(entityName, requestedSet, items);
        }

        public virtual async Task<UrlRecord> ApplySlugAsync<T>(T entity, string slug, int languageId, bool save = false) 
            where T : BaseEntity, ISlugSupported
        {
            Guard.NotNull(entity, nameof(entity));

            int entityId = entity.Id;
            string entityName = entity.GetEntityName();
            UrlRecord result = null;
            var dirty = false;

            var allUrlRecords = await _db.UrlRecords
                .ApplyEntityFilter(entity)
                .Where(x => x.LanguageId == languageId)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            var activeEntry = allUrlRecords.FirstOrDefault(x => x.IsActive);
            if (activeEntry == null && slug.HasValue())
            {
                // Find in non-active records with the specified slug
                var inactiveEntryWithSpecifiedSlug = allUrlRecords.FirstOrDefault(x => x.Slug.EqualsNoCase(slug) && !x.IsActive);
                if (inactiveEntryWithSpecifiedSlug != null)
                {
                    // Mark non-active record as active
                    inactiveEntryWithSpecifiedSlug.IsActive = true;
                    result = inactiveEntryWithSpecifiedSlug;
                    dirty = true;
                }
                else
                {
                    // New record
                    var urlRecord = new UrlRecord
                    {
                        EntityId = entity.Id,
                        EntityName = entityName,
                        Slug = slug,
                        LanguageId = languageId,
                        IsActive = true,
                    };
                    _db.UrlRecords.Add(urlRecord);
                    _extraSlugLookup[urlRecord.Slug] = urlRecord;
                    dirty = true;
                    result = urlRecord;
                }
            }

            if (activeEntry != null && string.IsNullOrWhiteSpace(slug))
            {
                // Disable the previous active URL record
                activeEntry.IsActive = false;
                dirty = true;
            }

            if (activeEntry != null && !string.IsNullOrWhiteSpace(slug))
            {
                // Is it the same slug as in active URL record?
                if (activeEntry.Slug.EqualsNoCase(slug))
                {
                    // yes. do nothing
                    // P.S. wrote this way for more source code readability
                }
                else
                {
                    // find in non-active records with the specified slug
                    var inactiveEntryWithSpecifiedSlug = allUrlRecords
                        .FirstOrDefault(x => x.Slug.EqualsNoCase(slug) && !x.IsActive);
                    if (inactiveEntryWithSpecifiedSlug != null)
                    {
                        // mark non-active record as active
                        inactiveEntryWithSpecifiedSlug.IsActive = true;

                        //disable the previous active URL record
                        activeEntry.IsActive = false;

                        dirty = true;
                    }
                    else
                    {
                        // MC: Absolutely ensure that we have no duplicate active record for this entity.
                        // In such case a record other than "activeUrlRecord" could have same seName
                        // and the DB would report an Index error.
                        var alreadyActiveDuplicate = allUrlRecords.FirstOrDefault(x => x.Slug.EqualsNoCase(slug) && x.IsActive);
                        if (alreadyActiveDuplicate != null)
                        {
                            // deactivate all
                            allUrlRecords.Each(x => x.IsActive = false);
                            // set the existing one to active again
                            alreadyActiveDuplicate.IsActive = true;
                            dirty = true;
                        }
                        else
                        {
                            // Insert new record
                            // we do not update the existing record because we should track all previously entered slugs
                            // to ensure that URLs will work fine
                            var urlRecord = new UrlRecord
                            {
                                EntityId = entity.Id,
                                EntityName = entityName,
                                Slug = slug,
                                LanguageId = languageId,
                                IsActive = true,
                            };
                            _db.UrlRecords.Add(urlRecord);
                            _extraSlugLookup[urlRecord.Slug] = urlRecord;
                            result = urlRecord;

                            // disable the previous active URL record
                            activeEntry.IsActive = false;

                            dirty = true;
                        }
                    }
                }
            }

            if (dirty && save)
            {
                await _db.SaveChangesAsync();
            }

            return result;
        }

        public virtual async Task<string> ValidateSlugAsync<T>(T entity,
            string slug,
            bool ensureNotEmpty,
            int? languageId = null)
            where T : BaseEntity, ISlugSupported
        {
            Guard.NotNull(entity, nameof(entity));

            // Use name if slug is not specified
            if (string.IsNullOrWhiteSpace(slug) && !string.IsNullOrWhiteSpace(entity.GetDisplayName()))
                slug = entity.GetDisplayName();

            // Validation
            slug = SeoHelper.BuildSlug(slug,
                _seoSettings.ConvertNonWesternChars,
                _seoSettings.AllowUnicodeCharsInUrls, 
                true, 
                _seoSettings.SeoNameCharConversion);

            if (string.IsNullOrWhiteSpace(slug))
            {
                if (ensureNotEmpty)
                {
                    // Use entity identifier as slug if empty
                    slug = entity.GetEntityName() + entity.Id.ToStringInvariant();
                }
                else
                {
                    // Return. no need for further processing
                    return slug;
                }
            }

            // Validate and alter slug if it could be interpreted as SEO code
            if (CultureHelper.IsValidCultureCode(slug))
            {
                if (slug.Length == 2)
                {
                    slug += "-0";
                }
            }

            // Ensure this slug is not reserved
            string entityName = entity.GetEntityName();
            int i = 2;
            var tempSlug = slug;

            while (true)
            {
                // Check whether such slug already exists (and that it's not the current entity)
                var urlRecord = _extraSlugLookup.Get(tempSlug) ?? (await _db.UrlRecords.FirstOrDefaultAsync(x => x.Slug == tempSlug));
                var reserved1 = urlRecord != null && !(urlRecord.EntityId == entity.Id && urlRecord.EntityName.EqualsNoCase(entityName));

                if (!reserved1 && urlRecord != null && languageId.HasValue)
                    reserved1 = (urlRecord.LanguageId != languageId.Value);

                // ...and it's not in the list of reserved slugs
                var reserved2 = _seoSettings.ReservedUrlRecordSlugs.Contains(tempSlug);
                if (!reserved1 && !reserved2)
                    break;

                var suffixLen = Math.Floor(Math.Log10(i) + 1).Convert<int>() + 1;
                tempSlug = string.Format("{0}-{1}", slug.Truncate(400 - suffixLen), i);
                i++;
            }
            slug = tempSlug;

            return slug;
        }

        public virtual Task<Dictionary<int, int>> CountSlugsPerEntityAsync(params int[] urlRecordIds)
        {
            if (urlRecordIds.Length == 0)
                return Task.FromResult(new Dictionary<int, int>());

            var query =
                from x in _db.UrlRecords
                where urlRecordIds.Contains(x.Id)
                select new
                {
                    x.Id,
                    Count = _db.UrlRecords.Where(y => y.EntityName == x.EntityName && y.EntityId == x.EntityId).Count()
                };

            var result = query
                .ToDictionaryAsync(x => x.Id, x => x.Count);

            return result;
        }

        #endregion
    }
}

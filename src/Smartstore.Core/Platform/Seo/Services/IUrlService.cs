using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Data;
using Smartstore.Core.Seo.Routing;
using Smartstore.Domain;

namespace Smartstore.Core.Seo
{
    public readonly struct ValidateSlugResult
    {
        private readonly ISlugSupported _source;

        public ValidateSlugResult(ValidateSlugResult copyFrom)
        {
            _source = copyFrom.Source;
            EntityName = _source?.GetEntityName();
            Slug = copyFrom.Slug;
            Found = copyFrom.Found;
            FoundIsSelf = copyFrom.FoundIsSelf;
            LanguageId = copyFrom.LanguageId;
            WasValidated = copyFrom.WasValidated;
        }

        public ISlugSupported Source 
        {
            get => _source;
            init
            {
                _source = value;
                EntityName = value?.GetEntityName();
            }
        }

        public string EntityName { get; private init; }
        public string Slug { get; init; }
        public UrlRecord Found { get; init; }
        public bool FoundIsSelf { get; init; }
        public int? LanguageId { get; init; }
        public bool WasValidated { get; init; }
    }
    
    /// <summary>
    /// Seo slugs service interface
    /// </summary>
    public partial interface IUrlService
    {
        /// <summary>
        /// Gets the <see cref="UrlPolicy"/> instance for the current request. The url policy
        /// can be used to modify specific segments of the current request URL (scheme, host, culture code,
        /// path and querystring). A middleware then analyzes the changes and performs a HTTP
        /// redirection to the new location if necessary.
        /// </summary>
        UrlPolicy GetUrlPolicy();

        /// <summary>
        /// Applies all configured rules for canonical URLs.
        /// </summary>
        UrlPolicy ApplyCanonicalUrlRulesPolicy();

        /// <summary>
        /// Applies all configured rules for seo friendly URLs.
        /// </summary>
        UrlPolicy ApplyCultureUrlPolicy(Endpoint endpoint);

        /// <summary>
        /// Gets the active slug for an entity.
        /// </summary>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="entityName">Entity name</param>
        /// <param name="languageId">Language identifier</param>
        /// <returns>Found slug or empty string</returns>
        Task<string> GetActiveSlugAsync(int entityId, string entityName, int languageId);

        /// <summary>
        /// Prefetches a collection of url record properties for a range of entities in one go
        /// and caches them for the duration of the current request.
        /// </summary>
        /// <param name="entityName">Entity name</param>
        /// <param name="entityIds">
        /// The entity ids to prefetch url records for. Can be null,
        /// in which case all records for the requested entity name are loaded.
        /// </param>
        /// <param name="isRange">Whether <paramref name="entityIds"/> represents a range of ids (perf).</param>
        /// <param name="isSorted">Whether <paramref name="entityIds"/> is already sorted (perf).</param>
        /// <param name="tracked">Whether to put prefetched entities to EF change tracker.</param>
        /// <returns>Url record collection</returns>
        /// <remarks>
        /// Be careful not to load large amounts of data at once (e.g. for "Product" scope with large range).
        /// </remarks>
        Task PrefetchUrlRecordsAsync(string entityName, int[] languageIds, int[] entityIds, bool isRange = false, bool isSorted = false, bool tracked = false);

        /// <summary>
        /// Prefetches a collection of url record properties for a range of entities in one go.
        /// </summary>
        /// <param name="entityName">Entity name</param>
        /// <param name="entityIds">
        /// The entity ids to prefetch url records for. Can be null,
        /// in which case all records for the requested entity name are loaded.
        /// </param>
        /// <param name="isRange">Whether <paramref name="entityIds"/> represents a range of ids (perf).</param>
        /// <param name="isSorted">Whether <paramref name="entityIds"/> is already sorted (perf).</param>
        /// <param name="tracked">Whether to put prefetched entities to EF change tracker.</param>
        /// <returns>Url record collection</returns>
        /// <remarks>
        /// Be careful not to load large amounts of data at once (e.g. for "Product" scope with large range).
        /// </remarks>
        Task<UrlRecordCollection> GetUrlRecordCollectionAsync(string entityName, int[] languageIds, int[] entityIds, bool isRange = false, bool isSorted = false, bool tracked = false);

        /// <summary>
        /// Slugifies and checks uniqueness of a given search engine name. If not unique, a number will be appended to the result slug.
        /// </summary>
        /// <typeparam name="T">Type of slug supporting entity</typeparam>
        /// <param name="entity">Entity instance</param>
        /// <param name="seName">Search engine display name to validate. If null or empty, name will be resolved from <see cref="IDisplayedEntity.GetDisplayName()"/>.</param>
        /// <param name="ensureNotEmpty">Ensure that slug is not empty</param>
        /// <returns>A system unique slug</returns>
        ValueTask<ValidateSlugResult> ValidateSlugAsync<T>(T entity, string seName, bool ensureNotEmpty, int? languageId = null)
            where T : ISlugSupported;

        /// <summary>
        /// Applies a slug.
        /// </summary>
        /// <param name="result">Result data from <see cref="ValidateSlugAsync{T}(T, string, bool, int?)"/> method call.</param>
        /// <param name="save"><c>true</c> will commit result to database.</param>
        /// <returns>
        /// The affected <see cref="UrlRecord"/> instance, either new or existing as tracked entity.
        /// </returns>
        Task<UrlRecord> ApplySlugAsync(ValidateSlugResult result, bool save = false);

        /// <summary>
        /// Gets the number of existing slugs per entity.
        /// </summary>
        /// <param name="urlRecordIds">URL record identifiers</param>
        /// <returns>Dictionary of slugs per entity count</returns>
        Task<Dictionary<int, int>> CountSlugsPerEntityAsync(params int[] urlRecordIds);

        /// <summary>
        /// Creates a variation of this service that is optimized
        /// for batching scenarios like long running imports or exports.
        /// Cache segmenting is turned off to avoid high memory pressure
        /// and applied slugs are queued until <see cref="IUrlServiceBatchScope.CommitAsync()"/> is called.
        /// </summary>
        /// <param name="db">
        /// The scope will internally use the passed instance or - if null - the request scoped instance from <see cref="IUrlService"/>.
        /// </param>
        /// <returns>The batch scope instance.</returns>
        IUrlServiceBatchScope CreateBatchScope(SmartDbContext db = null);
    }
}
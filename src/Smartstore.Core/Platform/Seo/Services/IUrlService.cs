#nullable enable

using System.Diagnostics.CodeAnalysis;
using Smartstore.Core.Data;
using Smartstore.Core.Seo.Routing;
using Smartstore.Threading;

namespace Smartstore.Core.Seo
{
    /// <summary>
    /// Seo slugs service interface
    /// </summary>
    public partial interface IUrlService
    {
        /// <summary>
        /// Gets the <see cref="UrlPolicy"/> instance for the current request. The url policy
        /// can be used to modify specific segments of the current request URL (scheme, host, culture code,
        /// path and querystring). A middleware then analyzes the changes and performs an HTTP
        /// redirection to the new location if necessary.
        /// </summary>
        UrlPolicy GetUrlPolicy();

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
        Task PrefetchUrlRecordsAsync(string entityName, int[]? languageIds, int[]? entityIds, bool isRange = false, bool isSorted = false, bool tracked = false);

        /// <summary>
        /// Clears the prefetch cache that was populated by calls to <see cref="PrefetchUrlRecordsAsync(string, int[], int[], bool, bool, bool)"/>
        /// </summary>
        void ClearPrefetchCache();

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
        Task<UrlRecordCollection> GetUrlRecordCollectionAsync(string entityName, int[]? languageIds, int[]? entityIds, bool isRange = false, bool isSorted = false, bool tracked = false);

        /// <summary>
        /// Slugifies and checks uniqueness of a given search engine name. If not unique, a number will be appended to the result slug.
        /// </summary>
        /// <typeparam name="T">Type of slug supporting entity</typeparam>
        /// <param name="entity">Entity instance</param>
        /// <param name="seName">Search engine name to validate. If <c>null</c> or empty, the slug will be resolved from <paramref name="displayName"/>.</param>
        /// <param name="displayName">Display name used to resolve the slug if <paramref name="seName"/> is empty.</param>
        /// <param name="ensureNotEmpty">Ensure that slug is not empty</param>
        /// <param name="force">
        /// <c>true</c> to check slug uniqueness directly against the database.
        /// <c>false</c> for performance reason, also check internal dictionary with already processed slugs.
        /// </param>
        /// <returns>A system unique slug</returns>
        ValueTask<ValidateSlugResult> ValidateSlugAsync<T>(T entity,
            string? seName,
            string? displayName,
            bool ensureNotEmpty,
            int? languageId = null,
            bool force = false)
            where T : ISlugSupported;

        /// <summary>
        /// Applies a slug.
        /// </summary>
        /// <param name="result">Result data from <see cref="ValidateSlugAsync{T}(T, string, string, bool, int?, bool)"/> method call.</param>
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
        /// and applied slugs are queued until <see cref="IUrlServiceBatchScope.CommitAsync(CancellationToken)"/> is called.
        /// </summary>
        /// <param name="db">
        /// The scope will internally use the passed instance or - if null - the request scoped instance from <see cref="IUrlService"/>.
        /// </param>
        /// <returns>The batch scope instance.</returns>
        IUrlServiceBatchScope CreateBatchScope(SmartDbContext? db = null);

        /// <summary>
        /// Gets a <see cref="IDistributedLock"/> instance for the given <paramref name="entity"/>
        /// used to synchronize access to the underlying slug storage.
        /// </summary>
        /// <typeparam name="T">Type of slug supporting entity</typeparam>
        /// <param name="entity">Entity instance</param>
        /// <param name="seName">Search engine name to acquire a lock for. If <c>null</c> or empty, the slug will be resolved from <paramref name="displayName"/>.</param>
        /// <param name="displayName">Display name used to to acquire a lock for if <paramref name="seName"/> is empty.</param>
        /// <param name="ensureNotEmpty">Ensure that slug is not empty</param>
        /// <returns>
        /// An <see cref="IDistributedLock"/> instance or <c>null</c> if no <paramref name="lockKey"/> could be generated.
        /// </returns>
        IDistributedLock? GetLock<T>(T entity, string? seName, string? displayName, bool ensureNotEmpty, out string? lockKey) where T : ISlugSupported;
    }
}
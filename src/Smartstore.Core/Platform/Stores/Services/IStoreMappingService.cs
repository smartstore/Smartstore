namespace Smartstore.Core.Stores
{
    /// <summary>
    /// Store mapping service interface
    /// </summary>
    public interface IStoreMappingService
    {
        /// <summary>
        /// Creates store mapping entities for a mappable entity and begins change tracking.
        /// This method does NOT commit to database.
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="entity">The entity</param>
        /// <param name="selectedStoreIds">Array of selected store ids</param>
        Task ApplyStoreMappingsAsync<T>(T entity, int[] selectedStoreIds) where T : BaseEntity, IStoreRestricted;

        /// <summary>
        /// Creates and adds a <see cref="StoreMapping"/> entity to the change tracker.
        /// This method does NOT commit to database.
        /// </summary>
        void AddStoreMapping<T>(T entity, int storeId) where T : BaseEntity, IStoreRestricted;

        /// <summary>
        /// Checks whether an entity can be accessed in the current store.
        /// </summary>
        /// <param name="entityName">Entity name to check</param>
        /// <param name="entityId">Entity id to check</param>
        /// <returns><c>true</c> authorized, otherwise <c>false</c>.</returns>
        Task<bool> AuthorizeAsync(string entityName, int entityId);

        /// <summary>
        /// Checks whether an entity can be accessed in a given store.
        /// </summary>
        /// <param name="entityName">Entity name to check</param>
        /// <param name="entityId">Entity id to check</param>
        /// <param name="storeId">Store identifier to check against</param>
        /// <returns><c>true</c> authorized, otherwise <c>false</c>.</returns>
        Task<bool> AuthorizeAsync(string entityName, int entityId, int storeId);

        /// <summary>
        /// Finds store identifiers with granted access (mapped to the entity)
        /// </summary>
        /// <param name="entityName">Entity name to check</param>
        /// <param name="entityId">Entity id to check</param>
        /// <returns>Store identifiers</returns>
        Task<int[]> GetAuthorizedStoreIdsAsync(string entityName, int entityId);

        /// <summary>
        /// Prefetches a collection of store mappings for a range of entities in one go
        /// and caches them for the duration of the current request.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="entityIds">
        /// The entity ids to load store mappings for. Can be null,
        /// in which case all store mappings for the requested scope are loaded.
        /// </param>
        /// <param name="isRange">A value indicating whether <paramref name="entityIds"/> represents a range of ids (perf).</param>
        /// <param name="isSorted">A value indicating whether <paramref name="entityIds"/> is already sorted (perf).</param>
        /// <param name="tracked">A value indicating whether to put prefetched entities to EF change tracker.</param>
        Task PrefetchStoreMappingsAsync(string entityName, int[] entityIds, bool isRange = false, bool isSorted = false, bool tracked = false);

        /// <summary>
        /// Gets a collection of store mappings for a range of entities in one go.
        /// </summary>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="entityIds">
        /// The entity ids to load store mappings for. Can be null,
        /// in which case all store mappings for the requested scope are loaded.
        /// </param>
        /// <param name="isRange">A value indicating whether <paramref name="entityIds"/> represents a range of ids (perf).</param>
        /// <param name="isSorted">A value indicating whether <paramref name="entityIds"/> is already sorted (perf).</param>
        /// <param name="tracked">A value indicating whether to put prefetched entities to EF change tracker.</param>
        /// <returns>Store mapping collection.</returns>
        Task<StoreMappingCollection> GetStoreMappingCollectionAsync(string entityName, int[] entityIds, bool isRange = false, bool isSorted = false, bool tracked = false);
    }
}

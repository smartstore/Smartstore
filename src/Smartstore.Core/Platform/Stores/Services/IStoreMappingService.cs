using System;
using System.Threading.Tasks;
using Smartstore.Domain;

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
        /// <returns>true - authorized; otherwise, false</returns>
        Task<bool> AuthorizeAsync(string entityName, int entityId);

        /// <summary>
        /// Checks whether an entity can be accessed in a given store.
        /// </summary>
        /// <param name="entityName">Entity name to check</param>
        /// <param name="entityId">Entity id to check</param>
        /// <param name="storeId">Store identifier to check against</param>
        /// <returns>true - authorized; otherwise, false</returns>
        Task<bool> AuthorizeAsync(string entityName, int entityId, int storeId);

        /// <summary>
        /// Finds store identifiers with granted access (mapped to the entity)
        /// </summary>
        /// <param name="entityName">Entity name to check</param>
        /// <param name="entityId">Entity id to check</param>
        /// <returns>Store identifiers</returns>
        Task<int[]> GetAuthorizedStoreIdsAsync(string entityName, int entityId);
    }
}

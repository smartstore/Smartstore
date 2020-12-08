using System;
using System.Threading.Tasks;

namespace Smartstore.Core.Common.Services
{
    // TODO: (core) Finish IGenericAttributeService contract.

    /// <summary>
    /// Generic attribute service interface
    /// </summary>
    public partial interface IGenericAttributeService
    {
        /// <summary>
        /// Gets a specialized generic attributes collection for the given entity.
        /// Loaded data will be cached for the duration of the request.
        /// </summary>
        /// <param name="entityName">Key group</param>
        /// <param name="entityId">Entity identifier</param>
        /// <returns>Generic attributes collection</returns>
        GenericAttributeCollection GetAttributesForEntity(string entityName, int entityId);

        /// <summary>
        /// Get attribute value
        /// </summary>
        /// <param name="entityName">Key group</param>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="key">Key</param>
        /// <param name="storeId">Store identifier; pass 0 for store neutral attribute.</param>
        /// <returns>Converted generic attribute value</returns>
        TProp GetAttribute<TProp>(string entityName, int entityId, string key, int storeId = 0);

        /// <summary>
        /// Get attribute value
        /// </summary>
        /// <param name="entityName">Key group</param>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="key">Key</param>
        /// <param name="storeId">Store identifier; pass 0 for store neutral attribute.</param>
        /// <returns>Converted generic attribute value</returns>
        Task<TProp> GetAttributeAsync<TProp>(string entityName, int entityId, string key, int storeId = 0);

        /// <summary>
        /// Applies generic attribute value. The caller is responsible for database commit.
        /// </summary>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="key">The key</param>
        /// <param name="keyGroup">The key group</param>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="storeId">Store identifier; pass 0 if this attribute will be available for all stores.</param>
        void ApplyAttribute<TProp>(int entityId, string key, string keyGroup, TProp value, int storeId = 0);

        /// <summary>
        /// Applies generic attribute value. The caller is responsible for database commit.
        /// </summary>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="entityId">Entity identifier</param>
        /// <param name="key">The key</param>
        /// <param name="keyGroup">The key group</param>
        /// <typeparam name="TProp">Property type</typeparam>
        /// <param name="storeId">Store identifier; pass 0 if this attribute will be available for all stores.</param>
        Task ApplyAttributeAsync<TProp>(int entityId, string key, string keyGroup, TProp value, int storeId = 0);
    }
}
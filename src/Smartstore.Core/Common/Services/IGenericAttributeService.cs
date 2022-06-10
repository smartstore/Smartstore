namespace Smartstore.Core.Common.Services
{
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
        /// Prefetches a collection of generic attributes for a range of entities in one go
        /// and caches them for the duration of the current request.
        /// </summary>
        /// <param name="entityName">Key group</param>
        /// <param name="entityIds">The entity ids to prefetch attributes for.</param>
        Task PrefetchAttributesAsync(string entityName, int[] entityIds);
    }
}
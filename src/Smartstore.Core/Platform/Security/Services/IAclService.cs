using Smartstore.Core.Identity;

namespace Smartstore.Core.Security
{
    /// <summary>
    /// ACL (Access Control List) service inerface.
    /// </summary>
    public interface IAclService
    {
        /// <summary>
        /// Gets a value indicating whether at least one ACL record is in active state system-wide.
        /// </summary>
        bool HasActiveAcl();

        /// <summary>
        /// Gets a value indicating whether at least one ACL record is in active state system-wide.
        /// </summary>
        Task<bool> HasActiveAclAsync();

        /// <summary>
        /// Creates ACL mapping entities for a mappable entity and begins change tracking.
        /// The caller is responsible for database commit.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="entity">The entity.</param>
        /// <param name="selectedCustomerRoleIds">Array of selected customer role identifiers.</param>
        Task ApplyAclMappingsAsync<T>(T entity, int[] selectedCustomerRoleIds) where T : BaseEntity, IAclRestricted;

        /// <summary>
        /// Finds customer role identifiers with granted access (mapped to the entity).
        /// </summary>
        /// <param name="entityName">Entity name to check.</param>
        /// <param name="entityId">Entity identifier to check.</param>
        /// <returns>Customer role identifiers.</returns>
        Task<int[]> GetAuthorizedCustomerRoleIdsAsync(string entityName, int entityId);

        /// <summary>
        /// Checks whether the current customer has been granted access to an entity.
        /// </summary>
        /// <param name="entityName">Entity name to check.</param>
        /// <param name="entityId">Entity identifier to check.</param>
        /// <returns><c>true</c> if granted, otherwise <c>false</c>.</returns>
        Task<bool> AuthorizeAsync(string entityName, int entityId);

        /// <summary>
        /// Checks whether certain customer roles has been granted access to an entity.
        /// </summary>
        /// <param name="entityName">Entity name to check.</param>
        /// <param name="entityId">Entity identifier to check.</param>
        /// <param name="roles">Customer roles to check. Inactive roles will be skipped.</param>
        /// <returns><c>true</c> if granted, otherwise <c>false</c>.</returns>
        Task<bool> AuthorizeAsync(string entityName, int entityId, IEnumerable<CustomerRole> roles);
    }
}

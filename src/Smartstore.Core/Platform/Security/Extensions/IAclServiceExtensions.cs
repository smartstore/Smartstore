using Smartstore.Core.Identity;

namespace Smartstore.Core.Security
{
    public static partial class IAclServiceExtensions
    {
        /// <summary>
        /// Finds customer role identifiers with granted access (mapped to the entity).
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="service">ACL service.</param>
        /// <param name="entity">Entity.</param>
        /// <returns>Customer role identifiers.</returns>
        public static async Task<int[]> GetAuthorizedCustomerRoleIdsAsync<T>(this IAclService service, T entity)
            where T : BaseEntity, IAclRestricted
        {
            if (entity == null)
            {
                return Array.Empty<int>();
            }

            return await service.GetAuthorizedCustomerRoleIdsAsync(entity.GetEntityName(), entity.Id);
        }

        /// <summary>
        /// Checks whether the current customer has been granted access to an entity.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="service">ACL service.</param>
        /// <param name="entity">Entity.</param>
        /// <returns><c>true</c> if granted, otherwise <c>false</c>.</returns>
        public static async Task<bool> AuthorizeAsync<T>(this IAclService service, T entity)
            where T : BaseEntity, IAclRestricted
        {
            if (entity == null)
            {
                return false;
            }

            if (!entity.SubjectToAcl)
            {
                return true;
            }

            return await service.AuthorizeAsync(entity.GetEntityName(), entity.Id);
        }

        /// <summary>
        /// Checks whether a customer has been granted access to an entity.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="service">ACL service.</param>
        /// <param name="entity">Entity.</param>
        /// <param name="customer">Customer.</param>
        /// <returns><c>true</c> if granted, otherwise <c>false</c>.</returns>
        public static async Task<bool> AuthorizeAsync<T>(this IAclService service, T entity, Customer customer)
            where T : BaseEntity, IAclRestricted
        {
            if (entity == null)
            {
                return false;
            }

            if (!entity.SubjectToAcl)
            {
                return true;
            }

            return await service.AuthorizeAsync(entity.GetEntityName(), entity.Id, customer?.CustomerRoleMappings?.Select(x => x.CustomerRole));
        }

        /// <summary>
        /// Checks whether the current customer has been granted access to entities.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="service">ACL service.</param>
        /// <param name="entities">Entities to check.</param>
        /// <returns>Authorized entities.</returns>
        public static IAsyncEnumerable<T> SelectAuthorizedAsync<T>(this IAclService service, IEnumerable<T> entities)
            where T : BaseEntity, IAclRestricted
        {
            Guard.NotNull(entities, nameof(entities));

            return entities.WhereAwait(async x => await service.AuthorizeAsync(x));
        }
    }
}

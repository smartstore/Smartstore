using System.Runtime.CompilerServices;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Seo;

namespace Smartstore.Core.Security
{
    public static partial class IAclRestrictedQueryExtensions
    {
        /// <summary>
        /// Applies filter for entities restricted by ACL (access control list).
        /// </summary>
        /// <param name="customer">Customer to be filtered according to their assigned customer roles.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IQueryable<T> ApplyAclFilter<T>(this IQueryable<T> query, Customer customer)
            where T : BaseEntity, IAclRestricted, new()
        {
            Guard.NotNull(customer, nameof(customer));
            return ApplyAclFilter(query, customer.GetRoleIds());
        }

        /// <summary>
        /// Applies filter for entities restricted by ACL (access control list).
        /// </summary>
        /// <param name="customerRoleIds">Customer role identifiers to be filtered by. <c>null</c> to get all entities.</param>
        public static IQueryable<T> ApplyAclFilter<T>(this IQueryable<T> query, int[] customerRoleIds)
            where T : BaseEntity, IAclRestricted, new()
        {
            Guard.NotNull(query, nameof(query));

            if (customerRoleIds == null || !customerRoleIds.Any())
            {
                return query;
            }

            var db = query.GetDbContext<SmartDbContext>();
            if (db.QuerySettings.IgnoreAcl)
            {
                return query;
            }

            var entityName = NamedEntity.GetEntityName<T>();

            var subQuery = db.AclRecords
                .Where(x => x.EntityName == entityName && customerRoleIds.Contains(x.CustomerRoleId))
                .Select(x => x.EntityId);

            query = query.Where(x => !x.SubjectToAcl || subQuery.Contains(x.Id));

            return query;
        }
    }
}

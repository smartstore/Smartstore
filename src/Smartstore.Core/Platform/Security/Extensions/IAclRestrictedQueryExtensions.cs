using System.Runtime.CompilerServices;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Security
{
    public static partial class IAclRestrictedQueryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IQueryable<T> ApplyAclFilter<T>(this IQueryable<T> query, Customer customer)
            where T : BaseEntity, IAclRestricted
        {
            Guard.NotNull(customer, nameof(customer));
            return ApplyAclFilter(query, customer.GetRoleIds());
        }

        public static IQueryable<T> ApplyAclFilter<T>(this IQueryable<T> query, int[] customerRoleIds)
            where T : BaseEntity, IAclRestricted
        {
            Guard.NotNull(query, nameof(query));

            // TODO: (core) Find a way to make ApplyAclFilter to work in cross-context scenarios.

            if (customerRoleIds == null || !customerRoleIds.Any())
            {
                return query;
            }

            var db = query.GetDbContext<SmartDbContext>();
            if (db.QuerySettings.IgnoreAcl)
            {
                return query;
            }

            var entityName = typeof(T).Name;

            var subQuery = db.AclRecords
                .Where(x => x.EntityName == entityName && customerRoleIds.Contains(x.CustomerRoleId))
                .Select(x => x.EntityId);

            query = query.Where(x => !x.SubjectToAcl || subQuery.Contains(x.Id));

            return query;
        }
    }
}

using System.Linq;
using Smartstore.Core.Data;
using Smartstore.Domain;

namespace Smartstore.Core.Security
{
    public static partial class IAclRestrictedQueryExtensions
    {
        public static IQueryable<T> ApplyAclFilter<T>(this IQueryable<T> query, int[] customerRolesIds)
            where T : BaseEntity, IAclRestricted
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(customerRolesIds, nameof(customerRolesIds));

            var db = query.GetDbContext<SmartDbContext>();
            if (!customerRolesIds.Any() || db.QuerySettings.IgnoreAcl)
            {
                return query;
            }

            // TODO: (mg) (core) Join AclRecord in ApplyAclFilter.
            //var entityName = typeof(T).Name;

            //query = 
            //    from m in query
            //    join a in  db.AclRecord
            //    on new { m1 = m.Id, m2 = entityName } equals new { m1 = a.EntityId, m2 = a.EntityName } into ma
            //    from a in ma.DefaultIfEmpty()
            //    where !m.SubjectToAcl || customerRolesIds.Contains(a.CustomerRoleId)
            //    select m;

            return query;
        }
    }
}

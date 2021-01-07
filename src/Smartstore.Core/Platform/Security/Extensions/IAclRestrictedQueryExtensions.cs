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

            // TODO: (core) Find a way to make ApplyAclFilter to work in cross-context scenarios.

            var db = query.GetDbContext<SmartDbContext>();
            if (!customerRolesIds.Any() || db.QuerySettings.IgnoreAcl)
            {
                return query;
            }

            var entityName = typeof(T).Name;

            query =
                from m in query
                join a in db.AclRecords
                on new { m1 = m.Id, m2 = entityName } equals new { m1 = a.EntityId, m2 = a.EntityName } into ma
                from a in ma.DefaultIfEmpty()
                where !m.SubjectToAcl || customerRolesIds.Contains(a.CustomerRoleId)
                select m;

            // TODO: (core) Does not work with efcore5 anymore 
            //query = query.Distinct();

            //// Does not work anymore in efcore
            //query = from c in query
            //        group c by c.Id into cGroup
            //        orderby cGroup.Key
            //        select cGroup.FirstOrDefault();

            return query;
        }
    }
}

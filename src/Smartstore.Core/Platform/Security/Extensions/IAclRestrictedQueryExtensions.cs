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

            query =
                from m in query
                join a in db.AclRecords
                on new { m1 = m.Id, m2 = entityName } equals new { m1 = a.EntityId, m2 = a.EntityName } into ma
                from a in ma.DefaultIfEmpty()
                where !m.SubjectToAcl || customerRoleIds.Contains(a.CustomerRoleId)
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

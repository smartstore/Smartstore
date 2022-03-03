using Smartstore.Core.Data;

namespace Smartstore.Core.Stores
{
    public static partial class IStoreRestrictedQueryExtensions
    {
        public static IQueryable<T> ApplyStoreFilter<T>(this IQueryable<T> query, int storeId)
            where T : BaseEntity, IStoreRestricted
        {
            Guard.NotNull(query, nameof(query));

            // TODO: (core) Find a way to make ApplyStoreFilter to work in cross-context scenarios.

            if (storeId == 0)
            {
                return query;
            }

            var db = query.GetDbContext<SmartDbContext>();
            if (db.QuerySettings.IgnoreMultiStore)
            {
                return query;
            }

            var entityName = typeof(T).Name;

            query = from x in query
                    join m in db.StoreMappings
                    on new { id = x.Id, name = entityName } equals new { id = m.EntityId, name = m.EntityName } into xm
                    from sc in xm.DefaultIfEmpty()
                    where !x.LimitedToStores || storeId == sc.StoreId
                    select x;

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
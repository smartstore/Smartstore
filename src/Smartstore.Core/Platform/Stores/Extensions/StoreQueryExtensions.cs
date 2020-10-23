using System;
using System.Linq;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Domain;

namespace Smartstore
{
    public static partial class StoreQueryExtensions
    {
        public static IQueryable<T> ApplyStoreFilter<T>(this IQueryable<T> query, int storeId)
            where T : BaseEntity, IStoreRestricted
        {
            Guard.NotNull(query, nameof(query));

            var entityName = typeof(T).Name;
            var db = query.GetDbContext<SmartDbContext>();

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

        //public static IQueryable<T> LimitToStore<T>(this IDbQueryFilters<SmartDbContext> filters, IQueryable<T> query, int storeId)
        //    where T : BaseEntity, IStoreRestricted
        //{
        //    Guard.NotNull(query, nameof(query));
            
        //    var entityName = typeof(T).Name;
        //    var db = filters.Context;

        //    query = from x in query
        //            join m in db.StoreMappings
        //            on new { id = x.Id, name = entityName } equals new { id = m.EntityId, name = m.EntityName } into xm
        //            from sc in xm.DefaultIfEmpty()
        //            where !x.LimitedToStores || storeId == sc.StoreId
        //            select x;

        //    return query;
        //}
    }
}
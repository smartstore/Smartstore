using Smartstore.Core.Data;
using Smartstore.Core.Seo;

namespace Smartstore.Core.Stores
{
    public static partial class IStoreRestrictedQueryExtensions
    {
        /// <summary>
        /// Applies filter for entities restricted by store.
        /// </summary>
        /// <param name="storeId">Store identifier to be filtered by. 0 to get all entities.</param>
        public static IQueryable<T> ApplyStoreFilter<T>(this IQueryable<T> query, int storeId)
            where T : BaseEntity, IStoreRestricted, new()
        {
            Guard.NotNull(query, nameof(query));

            if (storeId == 0)
            {
                return query;
            }

            var db = query.GetDbContext<SmartDbContext>();
            if (db.QuerySettings.IgnoreMultiStore)
            {
                return query;
            }

            var entityName = NamedEntity.GetEntityName<T>();

            var subQuery = db.StoreMappings
                .Where(x => x.EntityName == entityName && x.StoreId == storeId)
                .Select(x => x.EntityId);

            query = query.Where(x => !x.LimitedToStores || subQuery.Contains(x.Id));

            return query;
        }
    }
}
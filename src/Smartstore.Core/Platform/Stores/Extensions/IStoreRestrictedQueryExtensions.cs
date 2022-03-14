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

            var subQuery = db.StoreMappings
                .Where(x => x.EntityName == entityName && x.StoreId == storeId)
                .Select(x => x.EntityId);

            query = query.Where(x => !x.LimitedToStores || subQuery.Contains(x.Id));

            return query;
        }
    }
}
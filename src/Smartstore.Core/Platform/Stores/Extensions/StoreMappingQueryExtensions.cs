using System;
using System.Linq;
using Smartstore.Domain;

namespace Smartstore.Core.Stores
{
    public static partial class StoreMappingQueryExtensions
    {
        public static IQueryable<StoreMapping> ApplyEntityFilter<T>(this IQueryable<StoreMapping> query, T entity)
            where T : BaseEntity, IStoreRestricted
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(entity, nameof(entity));

            return ApplyEntityFilter(query, entity.GetEntityName(), entity.Id);
        }

        public static IQueryable<StoreMapping> ApplyEntityFilter(this IQueryable<StoreMapping> query, string entityName, int entityId)
        {
            Guard.NotNull(query, nameof(query));

            if (entityName.HasValue())
                query = query.Where(x => x.EntityName == entityName);

            if (entityId != 0)
                query = query.Where(x => x.EntityId == entityId);

            return query;
        }
    }
}
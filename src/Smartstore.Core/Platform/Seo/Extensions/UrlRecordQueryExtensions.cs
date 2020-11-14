using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Domain;

namespace Smartstore.Core.Seo
{
    public static partial class UrlRecordQueryExtensions
    {
        public static IQueryable<UrlRecord> ApplyEntityFilter<T>(this IQueryable<UrlRecord> query, T entity, bool activeOnly = false)
            where T : BaseEntity, ISlugSupported
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(entity, nameof(entity));

            return ApplyEntityFilter(query, entity.GetEntityName(), entity.Id, activeOnly);
        }

        public static IQueryable<UrlRecord> ApplyEntityFilter(this IQueryable<UrlRecord> query, string entityName, int entityId, bool activeOnly = false)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotEmpty(entityName, nameof(entityName));

            query = query.Where(x => x.EntityId == entityId && x.EntityName == entityName);

            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            return query;
        }
    }
}

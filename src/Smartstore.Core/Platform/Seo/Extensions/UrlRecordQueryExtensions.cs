using System;
using System.Linq;
using Smartstore.Domain;

namespace Smartstore.Core.Seo
{
    public static partial class UrlRecordQueryExtensions
    {
        public static IQueryable<UrlRecord> ApplySlugFilter(this IQueryable<UrlRecord> query, string slug, bool exactMatch = true)
        {
            Guard.NotNull(query, nameof(query));

            if (string.IsNullOrEmpty(slug))
                return query;

            return exactMatch 
                ? query.Where(x => x.Slug == slug)
                : query.Where(x => x.Slug.Contains(slug));
        }

        public static IQueryable<UrlRecord> ApplyEntityFilter<T>(this IQueryable<UrlRecord> query, T entity, bool? active = null)
            where T : BaseEntity, ISlugSupported
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(entity, nameof(entity));

            return ApplyEntityFilter(query, entity.GetEntityName(), entity.Id, active);
        }

        public static IQueryable<UrlRecord> ApplyEntityFilter(this IQueryable<UrlRecord> query, string entityName, int entityId, bool? active = null)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotEmpty(entityName, nameof(entityName));

            query = query.Where(x => x.EntityId == entityId && x.EntityName == entityName);

            if (active.HasValue)
            {
                query = query.Where(x => x.IsActive == active.Value);
            }

            return query;
        }
    }
}

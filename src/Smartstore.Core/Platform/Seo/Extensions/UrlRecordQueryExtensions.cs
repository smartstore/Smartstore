namespace Smartstore.Core.Seo
{
    public static partial class UrlRecordQueryExtensions
    {
        /// <summary>
        /// Applies a slug filter.
        /// </summary>
        /// <param name="query"><see cref="UrlRecord"/> query.</param>
        /// <param name="slug">Applies filter by <see cref="UrlRecord.Slug"/>.</param>
        /// <param name="exactMatch">A value indicating whether to filter by exact match.</param>
        /// <returns><see cref="UrlRecord"/> query.</returns>
        public static IQueryable<UrlRecord> ApplySlugFilter(this IQueryable<UrlRecord> query, string slug, bool exactMatch = true)
        {
            Guard.NotNull(query, nameof(query));

            if (string.IsNullOrEmpty(slug))
                return query;

            return exactMatch
                ? query.Where(x => x.Slug == slug)
                : query.Where(x => x.Slug.Contains(slug));
        }

        /// <summary>
        /// Applies an entity filter and sorts by <see cref="BaseEntity.Id"/> descending.
        /// </summary>
        /// <param name="query"><see cref="UrlRecord"/> query.</param>
        /// <param name="entity">Applies a filter by <see cref="UrlRecord.EntityName"/> and <see cref="UrlRecord.EntityId"/>.</param>
        /// <param name="languageId">Applies a filter by <see cref="UrlRecord.LanguageId"/>.</param>
        /// <param name="active">Applies a filter by <see cref="UrlRecord.IsActive"/>.</param>
        /// <returns><see cref="UrlRecord"/> query.</returns>
        public static IOrderedQueryable<UrlRecord> ApplyEntityFilter<T>(this IQueryable<UrlRecord> query, T entity, int? languageId = null, bool? active = null)
            where T : ISlugSupported
        {
            Guard.NotNull(entity, nameof(entity));

            return ApplyEntityFilter(query, entity.GetEntityName(), entity.Id, languageId, active);
        }

        /// <summary>
        /// Applies an entity filter and sorts by <see cref="BaseEntity.Id"/> descending.
        /// </summary>
        /// <param name="query"><see cref="UrlRecord"/> query.</param>
        /// <param name="entityName">Applies a filter by <see cref="UrlRecord.EntityName"/>.</param>
        /// <param name="entityId">Applies a filter by <see cref="UrlRecord.EntityId"/>.</param>
        /// <param name="languageId">Applies a filter by <see cref="UrlRecord.LanguageId"/>.</param>
        /// <param name="active">Applies a filter by <see cref="UrlRecord.IsActive"/>.</param>
        /// <returns><see cref="UrlRecord"/> query.</returns>
        public static IOrderedQueryable<UrlRecord> ApplyEntityFilter(this IQueryable<UrlRecord> query,
            string entityName,
            int entityId,
            int? languageId,
            bool? active = null)
        {
            Guard.NotNull(query, nameof(query));

            if (entityId > 0)
            {
                query = query.Where(x => x.EntityId == entityId);
            }

            if (entityName.HasValue())
            {
                query = query.Where(x => x.EntityName == entityName);
            }

            if (languageId.HasValue)
            {
                query = query.Where(x => x.LanguageId == languageId.Value);
            }

            if (active.HasValue)
            {
                query = query.Where(x => x.IsActive == active.Value);
            }

            return query.OrderByDescending(x => x.Id);
        }
    }
}

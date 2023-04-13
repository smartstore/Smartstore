namespace Smartstore.Core.Search
{
    public abstract class SearchQueryHandler<TEntity, TQuery>
        where TEntity : BaseEntity
        where TQuery : ISearchQuery
    {
        public abstract IQueryable<TEntity> Apply(IQueryable<TEntity> query, SearchQueryContext<TQuery> ctx);

        /// <summary>
        /// Gets a list of entity identifiers from search filters excluding <see cref="IRangeSearchFilter"/>.
        /// </summary>
        protected static int[] GetIdList(string fieldName, SearchQueryContext<TQuery> ctx)
        {
            if (ctx.AttributeFilters.Count == 0)
            {
                return Array.Empty<int>();
            }

            var list = ctx.AttributeFilters
                .Where(x => x.FieldName == fieldName)
                .Select(x => (int)x.Term)
                .ToArray();

            return list;
        }
    }
}

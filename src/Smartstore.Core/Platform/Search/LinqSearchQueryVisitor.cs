namespace Smartstore.Core.Search
{
    public abstract class LinqSearchQueryVisitor<TEntity, TQuery, TContext>
        where TEntity : BaseEntity
        where TQuery : ISearchQuery
        where TContext : SearchQueryContext<TQuery>
    {
        private IQueryable<TEntity> _resultQuery;

        public TQuery SearchQuery
        {
            get => Context.SearchQuery;
        }

        public IQueryable<TEntity> ResultDbQuery
        {
            get => _resultQuery;
        }

        public TContext Context
        {
            get;
            private set;
        }
    }
}

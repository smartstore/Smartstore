using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;

namespace Smartstore.Core.Search
{
    /// <summary>
    /// Represents a stateless visitor for search queries of type <typeparamref name="TQuery"/>.
    /// A visitor is reponsible for creating LINQ expressions
    /// for search query terms, filters and sortings.
    /// A concrete visitor implementation should be registered as a transient dependency via DI.
    /// </summary>
    /// <typeparam name="TEntity">The type of entity the visitor handles</typeparam>
    /// <typeparam name="TQuery">The type of search query the visitor handles</typeparam>
    /// <typeparam name="TContext">The concrete type of the search query context.</typeparam>
    public abstract class LinqSearchQueryVisitor<TEntity, TQuery, TContext>
        where TEntity : BaseEntity
        where TQuery : ISearchQuery
        where TContext : SearchQueryContext<TQuery>
    {
        public virtual int Order
        {
            get => 0;
        }

        /// <summary>
        /// Dispatches the search query to one of the more specialized visit methods in this class.
        /// </summary>
        /// <param name="context">The search context that also provides the <typeparamref name="TQuery"/> instance.</param>
        /// <param name="baseQuery">The base LINQ query to start with.</param>
        /// <returns>The final LINQ query after all query nodes have been visited.</returns>
        public IQueryable<TEntity> Visit(TContext context, IQueryable<TEntity> baseQuery)
        {
            Guard.NotNull(context);
            Guard.NotNull(baseQuery);

            var query = baseQuery;

            // Filters
            for (var i = 0; i < context.Filters.Count; i++)
            {
                query = VisitFilter(context.Filters[i], context, query);
            }

            // Not supported by EF Core 5+
            //if (Context.IsGroupingRequired)
            //{
            //    query =
            //        from p in query
            //        group p by p.Id into grp
            //        orderby grp.Key
            //        select grp.FirstOrDefault();
            //}

            // INFO: Distinct does not preserve ordering.
            if (context.IsGroupingRequired)
            {
                // Distinct is very slow if there are many products.
                query = query.Distinct();
            }

            // Sorting.
            var sortings = context.SearchQuery.Sorting;
            if (sortings.Count > 0)
            {
                foreach (var sorting in sortings)
                {
                    // "query" is not necessarily of type IOrderedQueryable.
                    query = VisitSorting(sorting, context, query);
                }
            }
            else
            {
                query = ApplyDefaultSorting(context, query);
            }

            return query;
        }

        /// <summary>
        /// Visits a search filter expression.
        /// </summary>
        /// <param name="filter">The visited filter.</param>
        /// <param name="context">The search query context.</param>
        /// <param name="query">The LINQ query to apply the filter to.</param>
        /// <returns>The modified or the original LINQ query.</returns>
        protected abstract IQueryable<TEntity> VisitFilter(ISearchFilter filter, TContext context, IQueryable<TEntity> query);

        /// <summary>
        /// Visits a search sorting expression.
        /// </summary>
        /// <param name="sorting">The visited sorting expression.</param>
        /// <param name="context">The search query context.</param>
        /// <param name="query">The LINQ query to apply the filter to.</param>
        /// <returns>The modified or the original LINQ query.</returns>
        protected abstract IQueryable<TEntity> VisitSorting(SearchSort sorting, TContext context, IQueryable<TEntity> query);

        /// <summary>
        /// Helper to apply a search filter to simple member expressions.
        /// </summary>
        /// <param name="memberExpression">The member expression to apply the <paramref name="filter"/> to.</param>
        /// <param name="filter">The filter to apply.</param>
        /// <param name="query">The LINQ query to apply the filter to.</param>
        /// <returns>The modified LINQ query.</returns>
        protected IQueryable<TEntity> ApplySimpleMemberExpression<TMember>(
            Expression<Func<TEntity, TMember>> memberExpression,
            ISearchFilter filter,
            IQueryable<TEntity> query)
            where TMember : struct
        {
            var entityType = typeof(TEntity);
            var descriptor = new FilterDescriptor<TEntity, TMember>(memberExpression, entityType == typeof(Product) ? RuleScope.Product : RuleScope.Other);
            var expressions = new List<FilterExpression>(2);
            var negate = filter.Occurence == SearchFilterOccurence.MustNot;

            if (filter is IRangeSearchFilter rf)
            {
                var lower = rf.Term as TMember?;
                var upper = rf.UpperTerm as TMember?;

                if (lower.HasValue)
                {
                    expressions.Add(new FilterExpression
                    {
                        Descriptor = descriptor,
                        Operator = negate
                            ? (rf.IncludesLower ? RuleOperator.LessThan : RuleOperator.LessThanOrEqualTo)
                            : (rf.IncludesLower ? RuleOperator.GreaterThanOrEqualTo : RuleOperator.GreaterThan),
                        Value = lower.Value
                    });
                }

                if (upper.HasValue)
                {
                    expressions.Add(new FilterExpression
                    {
                        Descriptor = descriptor,
                        Operator = negate
                            ? (rf.IncludesUpper ? RuleOperator.GreaterThan : RuleOperator.GreaterThanOrEqualTo)
                            : (rf.IncludesUpper ? RuleOperator.LessThanOrEqualTo : RuleOperator.LessThan),
                        Value = upper.Value
                    });
                }
            }
            else
            {
                var terms = filter.GetTermsArray<TMember>();

                if (terms.Length == 1)
                {
                    expressions.Add(new FilterExpression
                    {
                        Descriptor = descriptor,
                        Operator = negate ? RuleOperator.IsNotEqualTo : RuleOperator.IsEqualTo,
                        Value = terms[0]
                    });
                }
                else if (terms.Length > 1)
                {
                    expressions.Add(new FilterExpression
                    {
                        Descriptor = descriptor,
                        Operator = negate ? RuleOperator.NotIn : RuleOperator.In,
                        Value = terms
                    });
                }
            }

            if (expressions.Count > 0)
            {
                var combinedExpression = new FilterExpressionGroup(entityType, expressions.ToArray())
                {
                    LogicalOperator = LogicalRuleOperator.And,
                };

                return query.Where(combinedExpression).Cast<TEntity>();
            }

            return query;
        }

        /// <summary>
        /// Applies a default sort to a LINQ query if the source search query does not
        /// contain any sort expressions.
        /// By default the query is sorted by <see cref="BaseEntity.Id"/> ascending.
        /// </summary>
        /// <param name="context">The search query context.</param>
        /// <param name="query">The LINQ query to apply the sort to.</param>
        /// <returns>The modified LINQ query.</returns>
        protected virtual IQueryable<TEntity> ApplyDefaultSorting(TContext context, IQueryable<TEntity> query)
        {
            return OrderBy(query, x => x.Id);
        }

        /// <summary>
        /// Helper to apply sort to a query.
        /// </summary>
        protected IOrderedQueryable<TEntity> OrderBy<TKey>(
            IQueryable<TEntity> query,
            Expression<Func<TEntity, TKey>> keySelector,
            bool descending = false)
        {
            // Don't check with "is...": will always return true.
            var isOrdered = query.Expression.Type == typeof(IOrderedQueryable<TEntity>);

            if (isOrdered)
            {
                if (descending)
                {
                    return ((IOrderedQueryable<TEntity>)query).ThenByDescending(keySelector);
                }
                else
                {
                    return ((IOrderedQueryable<TEntity>)query).ThenBy(keySelector);
                }
            }
            else
            {
                if (descending)
                {
                    return query.OrderByDescending(keySelector);
                }
                else
                {
                    return query.OrderBy(keySelector);
                }
            }
        }
    }
}

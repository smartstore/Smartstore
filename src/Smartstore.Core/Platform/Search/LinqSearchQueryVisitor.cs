using Smartstore.Core.Rules;
using Smartstore.Core.Rules.Filters;

namespace Smartstore.Core.Search
{
    public abstract class LinqSearchQueryVisitor<TEntity, TQuery>
        where TEntity : BaseEntity
        where TQuery : ISearchQuery
    {
        private IQueryable<TEntity> _resultQuery;

        public virtual int Order
        {
            get => 0;
        }

        public TQuery SearchQuery
        {
            get => Context.SearchQuery;
        }

        public virtual SearchQueryContext<TQuery> Context
        {
            get;
            protected set;
        }

        public IQueryable<TEntity> ResultDbQuery
        {
            get => _resultQuery;
        }

        public IQueryable<TEntity> Visit(SearchQueryContext<TQuery> context, IQueryable<TEntity> baseQuery)
        {
            Context = Guard.NotNull(context);

            _resultQuery = Guard.NotNull(baseQuery);
            _resultQuery = VisitCore(context, _resultQuery);

            return _resultQuery;
        }

        protected virtual IQueryable<TEntity> VisitCore(SearchQueryContext<TQuery> context, IQueryable<TEntity> query)
        {
            // TODO: (mg) Refactor after Terms isolation is implemented.
            query = VisitTerm(query);

            // Filters
            for (var i = 0; i < context.Filters.Count; i++)
            {
                query = VisitFilter(context.Filters[i], query);
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
            if (Context.IsGroupingRequired)
            {
                // Distinct is very slow if there are many products.
                query = query.Distinct();
            }

            // Sorting
            foreach (var sorting in SearchQuery.Sorting)
            {
                query = VisitSorting(sorting, query);
            }

            // Default sorting
            if (query.Expression.Type != typeof(IOrderedQueryable<TEntity>))
            {
                query = ApplyDefaultSorting(query);
            }

            return query;
        }

        protected abstract IQueryable<TEntity> VisitTerm(IQueryable<TEntity> query);

        protected abstract IQueryable<TEntity> VisitFilter(ISearchFilter filter, IQueryable<TEntity> query);

        protected abstract IQueryable<TEntity> VisitSorting(SearchSort sorting, IQueryable<TEntity> query);

        protected IQueryable<TEntity> ApplySimpleMemberExpression<TMember>(
            Expression<Func<TEntity, TMember>> memberExpression,
            ISearchFilter filter,
            IQueryable<TEntity> query)
            where TMember : struct
        {
            var descriptor = new FilterDescriptor<TEntity, TMember>(memberExpression);
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
                var combinedExpression = new FilterExpressionGroup(typeof(TEntity), expressions.ToArray())
                {
                    LogicalOperator = LogicalRuleOperator.And,
                };

                return query.Where(combinedExpression).Cast<TEntity>();
            }

            return query;
        }

        protected virtual IOrderedQueryable<TEntity> ApplyDefaultSorting(IQueryable<TEntity> query)
        {
            return query.OrderBy(x => x.Id);
        }

        /// <summary>
        /// Helper to apply ordering to a query.
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

using System;
using System.Linq;
using System.Linq.Expressions;
using Smartstore.Domain;

namespace Smartstore.Core.Rules.Filters
{
    public static class RuleQueryableExtensions
    {
        #region Common

        public static IQueryable Where(this IQueryable source, FilterExpression filter)
        {
            Expression predicate = filter.ToPredicate(null, source.Provider);
            return source.Where(predicate);
        }

        public static IQueryable Where(this IQueryable source, Expression predicate)
        {
            var typeArgs = new Type[] { source.ElementType };
            var exprArgs = new Expression[] { source.Expression, Expression.Quote(predicate) };

            return source.Provider.CreateQuery(
                Expression.Call(typeof(Queryable), "Where", typeArgs, exprArgs));
        }

        #endregion

        #region Entities

        /// <summary>
        /// Applies a (wildcard) search filter to given string member.
        /// </summary>
        /// <param name="expression">
        /// The member expression.
        /// </param>
        /// <param name="term">
        /// The term to search for. Enclose the term with quotes (" or ') to perform an exact match search, 
        /// otherwise a "Contains" search will be performed. If the term contains wildcard chars (* or ?),
        /// an adequate "LIKE" predicate will be built depending on the query provider.
        /// </param>
        public static IQueryable<T> ApplySearchTermFilterFor<T>(this IQueryable<T> query, Expression<Func<T, string>> expression, string term)
            where T : BaseEntity
        {
            return ApplySearchTermFilter(
                query,
                term, 
                LogicalRuleOperator.And, // Doesn't matter
                Guard.NotNull(expression, nameof(expression)));
        }

        /// <summary>
        /// Applies a (wildcard) search filter to given string members by combining 
        /// the predicates with <paramref name="logicalOperator"/>.
        /// </summary>
        /// <param name="filter">
        /// The search filter. Enclose the term with quotes (" or ') to perform an exact match search, 
        /// otherwise a "Contains" search will be performed. If the term contains wildcard chars (* or ?),
        /// an adequate "LIKE" predicate will be built depending on the query provider.
        /// </param>
        /// <param name="logicalOperator">
        /// The logical operator to combine multiple <paramref name="expressions"/> with.
        /// </param>
        /// <param name="expressions">
        /// All member access expressions to build a combined lambda predicate for.
        /// </param>
        public static IQueryable<T> ApplySearchTermFilter<T>(this IQueryable<T> query,
            string filter,
            LogicalRuleOperator logicalOperator,
            params Expression<Func<T, string>>[] expressions)
            where T : BaseEntity
        {
            Guard.NotNull(query, nameof(query));

            if (expressions.Length == 0)
            {
                return query;
            }

            var filterExpressions = expressions
                .Select(expression => 
                {
                    // TODO: (core) ErrorHandling and ModelState for ApplySearchTermFilter
                    if (FilterExpressionParser.TryParse(expression, filter, out var filterExpression))
                    {
                        return filterExpression;
                    }

                    return null;

                    //var descriptor = new FilterDescriptor<T, string>(expression)
                    //{
                    //    RuleType = RuleType.String,
                    //    Name = "SearchTerm"
                    //};

                    //RuleOperator op;

                    //if (string.IsNullOrEmpty(filter))
                    //{
                    //    op = RuleOperator.IsEqualTo;
                    //}
                    //else
                    //{
                    //    var startsWithQuote = filter[0] == '"' || filter[0] == '\'';
                    //    var exactMatch = startsWithQuote && filter.EndsWith(filter[0]);
                    //    if (exactMatch)
                    //    {
                    //        op = RuleOperator.IsEqualTo;
                    //        filter = filter.Trim(filter[0]);
                    //    }
                    //    else
                    //    {
                    //        var hasAnyWildcard = filter.IndexOfAny(new[] { '*', '?' }) > -1;
                    //        op = hasAnyWildcard ? RuleOperator.Like : RuleOperator.Contains;
                    //    }
                    //}

                    //return new FilterExpression
                    //{
                    //    Descriptor = descriptor,
                    //    Operator = op,
                    //    Value = filter
                    //};
                })
                .Where(x => x != null)
                .ToArray();

            if (filterExpressions.Length == 0)
            {
                return query;
            }
            else if (filterExpressions.Length == 1)
            {
                return query.Where(filterExpressions[0]).Cast<T>();
            }
            else
            {
                var compositeFilter = new FilterExpressionGroup(typeof(T), filterExpressions) 
                { 
                    LogicalOperator = logicalOperator 
                };
                return query.Where(compositeFilter).Cast<T>();
            }
        }

        #endregion
    }
}

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
        /// TODO: Describe
        /// </summary>
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
        /// TODO: Describe
        /// </summary>
        public static IQueryable<T> ApplySearchTermFilter<T>(this IQueryable<T> query,
            string term,
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
                    var descriptor = new FilterDescriptor<T, string>(expression)
                    {
                        RuleType = RuleType.String,
                        Name = "SearchTerm"
                    };

                    RuleOperator op;

                    if (string.IsNullOrEmpty(term))
                    {
                        op = RuleOperator.IsEqualTo;
                    }
                    else
                    {
                        var startsWithQuote = term[0] == '"' || term[0] == '\'';
                        var exactMatch = startsWithQuote && term.EndsWith(term[0]);
                        if (exactMatch)
                        {
                            op = RuleOperator.IsEqualTo;
                            term = term.Trim(term[0]);
                        }
                        else
                        {
                            op = RuleOperator.Like;
                        }
                    }

                    return new FilterExpression
                    {
                        Descriptor = descriptor,
                        Operator = op,
                        Value = term
                    };
                })
                .ToArray();

            FilterExpression filterExpression;

            if (filterExpressions.Length == 1)
            {
                filterExpression = filterExpressions[0];
            }
            else
            {
                var compositeFilter = new FilterExpressionGroup(typeof(T)) { LogicalOperator = logicalOperator };
                compositeFilter.AddExpressions(filterExpressions);
                filterExpression = compositeFilter;
            }

            return query.Where(filterExpression).Cast<T>();
        }

        #endregion
    }
}

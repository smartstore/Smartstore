using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Smartstore.Domain;

namespace Smartstore.Core.Rules.Filters
{
    public static class QueryableExtensions
    {
        private readonly static MethodInfo EFFunctionsLikeMethod 
            = typeof(Microsoft.EntityFrameworkCore.DbFunctionsExtensions).GetMethod("Like",
                new Type[] { typeof(DbFunctions), typeof(string), typeof(string), typeof(string) });

        #region Entities

        /// <summary>
        /// TODO: Describe
        /// </summary>
        public static IQueryable<T> ApplyWildcardFilterFor<T>(this IQueryable<T> query, Expression<Func<T, string>> expression, string filter)
            where T : BaseEntity
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotNull(expression, nameof(expression));

            if (string.IsNullOrEmpty(filter))
            {
                return query.Where(CreatePredicate(expression, RuleOperator.IsEqualTo, filter)).Cast<T>();
            }

            var hasAnyCharToken = filter.IndexOf('*') > -1;
            var hasSingleCharToken = filter.IndexOf('?') > -1;
            var hasAnyWildcard = hasAnyCharToken || hasSingleCharToken;

            if (!hasAnyWildcard)
            {
                var startsWithQuote = filter[0] == '"' || filter[0] == '\'';
                var exactMatch = startsWithQuote && filter.EndsWith(filter[0]);
                var predicate = exactMatch
                    ? CreatePredicate(expression, RuleOperator.IsEqualTo, filter.Trim(filter[0]))
                    : CreatePredicate(expression, RuleOperator.Contains, filter);

                return query.Where(predicate).Cast<T>();
            }
            else
            {
                // Convert file wildcard pattern to SQL LIKE expression:
                // my*new_file-?.png > my%new/_file-_.png

                var hasUnderscore = filter.IndexOf('_') > -1;

                if (hasUnderscore)
                {
                    filter = filter.Replace("_", "/_");
                }
                if (hasAnyCharToken)
                {
                    filter = filter.Replace('*', '%');
                }
                if (hasSingleCharToken)
                {
                    filter = filter.Replace('?', '_');
                }
                
                //var mi = EFFunctionsLikeMethod;
                //var call = Expression.Call(mi, 
                //    Expression.Constant(EF.Functions),
                //    Expression.Constant("yodele"),
                //    Expression.Constant(filter),
                //    Expression.Constant("/"));
                //var lambda = Expression.Equal(
                //    call,
                //    ExpressionHelper.TrueLiteral);

                var memberName = expression.ExtractPropertyInfo().Name;
                return query.Where(x => EF.Functions.Like(memberName, filter, "/"));
            }
        }

        private static LambdaExpression CreatePredicate<T>(Expression<Func<T, string>> left, RuleOperator op, object right)
            where T : BaseEntity
        {
            var paramExpr = Expression.Parameter(typeof(T), "it");
            var valueExpr = ExpressionHelper.CreateValueExpression(left.Body.Type, right);
            var expr = op.GetExpression(left.Body, valueExpr, true);
            var predicate = ExpressionHelper.CreateLambdaExpression(paramExpr, expr);

            return predicate;
        }

        #endregion

        #region Common

        public static IQueryable Where(this IQueryable source, FilterExpression filter)
        {
            Expression predicate = filter.ToPredicate(null, IsLinqToObjectsProvider(source.Provider));
            return source.Where(predicate);
        }

        public static IQueryable Where(this IQueryable source, Expression predicate)
        {
            var typeArgs = new Type[] { source.ElementType };
            var exprArgs = new Expression[] { source.Expression, Expression.Quote(predicate) };

            return source.Provider.CreateQuery(
                Expression.Call(typeof(Queryable), "Where", typeArgs, exprArgs));
        }

        internal static bool IsLinqToObjectsProvider(IQueryProvider provider)
        {
            return provider.GetType().FullName.Contains("EnumerableQuery");
        }

        #endregion
    }
}

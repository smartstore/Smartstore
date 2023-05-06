using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace Smartstore.Data.Sqlite.Translators
{
    public class SqliteNoCaseQueryTranslationPostprocessorFactory : IQueryTranslationPostprocessorFactory
    {
        private readonly QueryTranslationPostprocessorDependencies _dependencies;
        private readonly RelationalQueryTranslationPostprocessorDependencies _relationalDependencies;

        public SqliteNoCaseQueryTranslationPostprocessorFactory(
            QueryTranslationPostprocessorDependencies dependencies,
            RelationalQueryTranslationPostprocessorDependencies relationalDependencies)
        {
            _dependencies = dependencies;
            _relationalDependencies = relationalDependencies;
        }

        public QueryTranslationPostprocessor Create(QueryCompilationContext queryCompilationContext)
            => new SqliteNoCaseQueryTranslationPostprocessor(
                _dependencies,
                _relationalDependencies,
                queryCompilationContext);
    }

    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    public class SqliteNoCaseQueryTranslationPostprocessor : SqliteQueryTranslationPostprocessor
    {
        public SqliteNoCaseQueryTranslationPostprocessor(
            QueryTranslationPostprocessorDependencies dependencies, 
            RelationalQueryTranslationPostprocessorDependencies relationalDependencies, 
            QueryCompilationContext queryCompilationContext)
            : base(dependencies, relationalDependencies, queryCompilationContext)
        {
        }

        public override Expression Process(Expression query)
        {
            var result = base.Process(query);

            var visitor = new NoCaseVisitor(RelationalDependencies.SqlExpressionFactory);
            result = visitor.Visit(result);

            return result;
        }

        private sealed class NoCaseVisitor : ExpressionVisitor
        {
            private readonly ISqlExpressionFactory _sqlExpressionFactory;

            public NoCaseVisitor(ISqlExpressionFactory sqlExpressionFactory)
            {
                _sqlExpressionFactory = sqlExpressionFactory;
            }

            protected override Expression VisitExtension(Expression extensionExpression)
            {
                if (extensionExpression is ShapedQueryExpression shapedQueryExpression)
                {
                    return shapedQueryExpression.Update(
                        Visit(shapedQueryExpression.QueryExpression),
                        Visit(shapedQueryExpression.ShaperExpression));
                }
                else if (extensionExpression is LikeExpression likeExpression)
                {
                    return VisitLikeExpression(likeExpression);
                }
                else
                {
                    return base.VisitExtension(extensionExpression);
                }
            }

            private Expression VisitLikeExpression(LikeExpression expression)
            {
                if (!MustPerformCaseInsensitiveLike(expression))
                {
                    return base.VisitExtension(expression);
                }
                
                var stringTypeMapping = InferTypeMapping(expression.Pattern, expression.Match);

                var matchToUpper = _sqlExpressionFactory.Function(
                    "upper",
                    new[] { expression.Match },
                    nullable: true,
                    argumentsPropagateNullability: new[] { true },
                    typeof(string),
                    stringTypeMapping);

                var patternToUpper = _sqlExpressionFactory.Function(
                    "upper",
                    new[] { expression.Pattern },
                    nullable: true,
                    argumentsPropagateNullability: new[] { true },
                    typeof(string),
                    stringTypeMapping);

                return expression.Update(matchToUpper, patternToUpper, expression.EscapeChar);
            }

            private static bool MustPerformCaseInsensitiveLike(LikeExpression expression)
            {
                if (expression.Pattern is SqlConstantExpression constantExpression)
                {
                    if (constantExpression.Value is null)
                    {
                        return false;
                    }
                    else if (constantExpression.Value is string pattern)
                    {
                        if (pattern.Length > 0)
                        {
                            return pattern.Any(c => !char.IsAscii(c));
                        }
                    }
                }

                return true;
            }

            private static RelationalTypeMapping InferTypeMapping(params SqlExpression[] expressions)
            {
                for (var i = 0; i < expressions.Length; i++)
                {
                    var sql = expressions[i];
                    if (sql.TypeMapping != null)
                    {
                        return sql.TypeMapping;
                    }
                }

                return null;
            }
        }
    }
}

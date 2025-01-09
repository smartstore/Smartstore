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
        private readonly IRelationalTypeMappingSource _typeMappingSource;

        public SqliteNoCaseQueryTranslationPostprocessorFactory(
            QueryTranslationPostprocessorDependencies dependencies,
            RelationalQueryTranslationPostprocessorDependencies relationalDependencies,
            IRelationalTypeMappingSource typeMappingSource)
        {
            _dependencies = dependencies;
            _relationalDependencies = relationalDependencies;
            _typeMappingSource = typeMappingSource;
        }

        public QueryTranslationPostprocessor Create(QueryCompilationContext queryCompilationContext)
            => new SqliteNoCaseQueryTranslationPostprocessor(
                _dependencies,
                _relationalDependencies,
                (RelationalQueryCompilationContext)queryCompilationContext,
                _typeMappingSource);
    }

    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    public class SqliteNoCaseQueryTranslationPostprocessor : SqliteQueryTranslationPostprocessor
    {
        private readonly RelationalTypeMapping _textTypeMapping;

        public SqliteNoCaseQueryTranslationPostprocessor(
            QueryTranslationPostprocessorDependencies dependencies, 
            RelationalQueryTranslationPostprocessorDependencies relationalDependencies,
            RelationalQueryCompilationContext queryCompilationContext,
            IRelationalTypeMappingSource typeMappingSource)
            : base(dependencies, relationalDependencies, queryCompilationContext)
        {
            _textTypeMapping = typeMappingSource.GetMapping(typeof(string), queryCompilationContext.Model);
        }

        public override Expression Process(Expression query)
        {
            var result = base.Process(query);

            var visitor = new NoCaseVisitor(RelationalDependencies.SqlExpressionFactory, _textTypeMapping);
            result = visitor.Visit(result);

            return result;
        }

        private sealed class NoCaseVisitor : ExpressionVisitor
        {
            private readonly ISqlExpressionFactory _sqlExpressionFactory;
            private readonly RelationalTypeMapping _textTypeMapping;

            public NoCaseVisitor(ISqlExpressionFactory sqlExpressionFactory, RelationalTypeMapping textTypeMapping)
            {
                _sqlExpressionFactory = sqlExpressionFactory;
                _textTypeMapping = textTypeMapping;
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
                if (!MustPerformCaseInsensitiveLike(expression.Match, expression.Pattern))
                {
                    return base.VisitExtension(expression);
                }

                return expression.Update(
                    ToLower(expression.Match),
                    ToLower(expression.Pattern),
                    expression.EscapeChar);
            }

            private SqlExpression ToLower(SqlExpression expression)
            {
                // Wrap the expression in a lower() function call,
                // although natively SQLite cannot lowercase non-ascii chars correctly.
                // But we rely on our custom lower() function override (see SqliteDbFactory.SqliteLower()).
                return _sqlExpressionFactory.Function(
                    "lower",
                    new[] { expression },
                    nullable: true,
                    argumentsPropagateNullability: new[] { true },
                    typeof(string),
                    _textTypeMapping);
            }

            private static bool MustPerformCaseInsensitiveLike(SqlExpression left, SqlExpression right)
            {
                if (left is SqlConstantExpression && right is SqlConstantExpression)
                {
                    // If both expressions are explicit/constant (e.g. 'Bügel' LIKE 'Büg%'),
                    // we don't need to lowercase the predicate.
                    return false;
                }

                if (right is SqlConstantExpression rightConstant)
                {
                    return HasNonAsciiChars(rightConstant);
                }
                else if (left is SqlConstantExpression leftConstant)
                {
                    return HasNonAsciiChars(leftConstant);
                }

                return false;

                static bool HasNonAsciiChars(SqlConstantExpression expr)
                {
                    if (expr.Value is null)
                    {
                        return false;
                    }
                    else if (expr.Value is string pattern && pattern.Length > 0)
                    {
                        return pattern.Any(c => !char.IsAscii(c));
                    }

                    return false;
                }
            }
        }
    }
}

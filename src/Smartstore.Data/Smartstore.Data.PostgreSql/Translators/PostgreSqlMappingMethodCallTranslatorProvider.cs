using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;

namespace Smartstore.Data.PostgreSql.Translators
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    internal class PostgreSqlMappingMethodCallTranslatorProvider : NpgsqlMethodCallTranslatorProvider
    {
        public PostgreSqlMappingMethodCallTranslatorProvider(
            RelationalMethodCallTranslatorProviderDependencies dependencies,
            IModel model,
            IDbContextOptions options)
            : base(dependencies, model, options)
        {
            var sqlExpressionFactory = (NpgsqlSqlExpressionFactory)dependencies.SqlExpressionFactory;
            var typeMappingSource = (NpgsqlTypeMappingSource)dependencies.RelationalTypeMappingSource;

            AddTranslators(new IMethodCallTranslator[]
            {
                new PostgreSqlDateDiffTranslator(typeMappingSource, sqlExpressionFactory)
            });
        }
    }
}

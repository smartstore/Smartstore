using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;
using Smartstore.Data.PostgreSql.Translators;

namespace Smartstore.Data.PostgreSql
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    internal class PostgreSqlMappingMethodCallTranslatorProvider : NpgsqlMethodCallTranslatorProvider
    {
        public PostgreSqlMappingMethodCallTranslatorProvider(
            RelationalMethodCallTranslatorProviderDependencies dependencies,
            IModel model,
            INpgsqlSingletonOptions options)
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

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;
using Microsoft.EntityFrameworkCore.Sqlite.Storage.Internal;

namespace Smartstore.Data.Sqlite.Translators
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    internal class SqliteMappingMethodCallTranslatorProvider : SqliteMethodCallTranslatorProvider
    {
        public SqliteMappingMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies)
            : base(dependencies)
        {
            var sqlExpressionFactory = (SqliteSqlExpressionFactory)dependencies.SqlExpressionFactory;
            var typeMappingSource = (SqliteTypeMappingSource)dependencies.RelationalTypeMappingSource;

            AddTranslators(new IMethodCallTranslator[]
            {
                new SqliteDateDiffTranslator(typeMappingSource, sqlExpressionFactory)
            });
        }
    }
}

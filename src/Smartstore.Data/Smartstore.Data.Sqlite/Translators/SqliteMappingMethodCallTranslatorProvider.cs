using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;

namespace Smartstore.Data.Sqlite.Translators
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    internal class SqliteMappingMethodCallTranslatorProvider : SqliteMethodCallTranslatorProvider
    {
        public SqliteMappingMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies, IModel model)
            : base(dependencies)
        {
            //var sqlExpressionFactory = (NpgsqlSqlExpressionFactory)dependencies.SqlExpressionFactory;
            //var typeMappingSource = (NpgsqlTypeMappingSource)dependencies.RelationalTypeMappingSource;

            //AddTranslators(new IMethodCallTranslator[]
            //{
            //    new PostgreSqlDateDiffTranslator(typeMappingSource, sqlExpressionFactory)
            //});
        }
    }
}

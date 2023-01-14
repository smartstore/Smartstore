using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal;
using Smartstore.Data.Providers;

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
        }

        public override SqlExpression Translate(
            IModel model,
            SqlExpression instance,
            MethodInfo method,
            IReadOnlyList<SqlExpression> arguments,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            //if (UnifiedDbFunctionMethods.Methods.Contains(method))
            //{
            //    var mappedFunction = MapDbFunction(method);
            //    if (mappedFunction != null)
            //    {
            //        method = mappedFunction.Method;
            //    }
            //}

            return base.Translate(model, instance, method, arguments, logger);
        }

        private static MethodInfo FindMappedMethod(MethodInfo sourceMethod)
        {
            var parameterTypes = sourceMethod.GetParameters().Select(p => p.ParameterType).ToArray();

            var method = typeof(NpgsqlDbFunctionsExtensions).GetRuntimeMethod(
                sourceMethod.Name,
                parameterTypes);

            return method;
        }
    }
}

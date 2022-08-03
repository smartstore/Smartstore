using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Smartstore.Data.Providers;

namespace Smartstore.Data.SqlServer
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    internal class SqlServerMappingMethodCallTranslatorProvider : SqlServerMethodCallTranslatorProvider
    {
        private static readonly FieldInfo _translatorsField = typeof(RelationalMethodCallTranslatorProvider)
            .GetField("_translators", BindingFlags.NonPublic | BindingFlags.Instance);

        public SqlServerMappingMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies)
            : base(dependencies)
        {
        }

        public override SqlExpression Translate(
            IModel model,
            SqlExpression instance,
            MethodInfo method,
            IReadOnlyList<SqlExpression> arguments,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (UnifiedDbFunctionMethods.Methods.Contains(method))
            {
                var mappedFunction = MapDbFunction(method);
                if (mappedFunction != null)
                {
                    method = mappedFunction.Method;
                }
            }

            return base.Translate(model, instance, method, arguments, logger);
        }

        /// <summary>
        /// Maps the given provider-agnostic <see cref="DbFunctions"/> extension method
        /// (from <see cref="DbFunctionsExtensions"/> class)
        /// to the matching provider-specific method.
        /// </summary>
        /// <param name="sourceMethod">The source method from <see cref="DbFunctionsExtensions"/> to map.</param>
        /// <returns>Information about the target provider-specific method and translator.</returns>
        private DbFunctionMap MapDbFunction(MethodInfo sourceMethod)
        {
            var translator = FindMethodCallTranslator(sourceMethod);
            if (translator != null)
            {
                var method = FindMappedMethod(sourceMethod);
                if (method != null)
                {
                    return new DbFunctionMap { Method = method, Translator = translator };
                }
            }

            return null;
        }

        private IMethodCallTranslator FindMethodCallTranslator(MethodInfo sourceMethod)
        {
            var translators = _translatorsField.GetValue(this) as List<IMethodCallTranslator>;
            if (translators != null)
            {
                if (sourceMethod.Name.StartsWith("DateDiff"))
                {
                    return translators.FirstOrDefault(x => x is SqlServerDateDiffFunctionsTranslator);
                }
            }

            return null;
        }

        private static MethodInfo FindMappedMethod(MethodInfo sourceMethod)
        {
            var parameterTypes = sourceMethod.GetParameters().Select(p => p.ParameterType).ToArray();

            var method = typeof(SqlServerDbFunctionsExtensions).GetRuntimeMethod(
                sourceMethod.Name,
                parameterTypes);

            return method;
        }
    }
}

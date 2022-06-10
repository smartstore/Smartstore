using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Smartstore.Data.Providers
{
    public class DbFunctionMap
    {
        public MethodInfo Method { get; init; }
        public IMethodCallTranslator Translator { get; init; }
    }

    public static class UnifiedDbFunctionMethods
    {
        private readonly static HashSet<MethodInfo> _uniMethods
            = new()
            {
                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffYear),
                    new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffYear),
                    new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffYear),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffYear),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffMonth),
                    new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffMonth),
                    new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffMonth),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffMonth),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffDay),
                    new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffDay),
                    new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffDay),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffDay),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffHour),
                    new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffHour),
                    new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffHour),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffHour),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffMinute),
                    new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffMinute),
                    new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffMinute),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffMinute),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffSecond),
                    new[] { typeof(DbFunctions), typeof(DateTime), typeof(DateTime) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffSecond),
                    new[] { typeof(DbFunctions), typeof(DateTime?), typeof(DateTime?) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffSecond),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset), typeof(DateTimeOffset) }),

                typeof(DbFunctionsExtensions).GetRuntimeMethod(
                    nameof(DbFunctionsExtensions.DateDiffSecond),
                    new[] { typeof(DbFunctions), typeof(DateTimeOffset?), typeof(DateTimeOffset?) }),
            };

        public static HashSet<MethodInfo> Methods
        {
            get => _uniMethods;
        }
    }
}

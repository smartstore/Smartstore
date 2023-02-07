using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Smartstore.Data.Providers;

namespace Smartstore.Data.Sqlite.Translators
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    internal class SqliteDateDiffTranslator : IMethodCallTranslator
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;
        private readonly SqliteSqlExpressionFactory _sqlExpressionFactory;

        public SqliteDateDiffTranslator(IRelationalTypeMappingSource typeMappingSource, SqliteSqlExpressionFactory sqlExpressionFactory)
        {
            _typeMappingSource = typeMappingSource;
            _sqlExpressionFactory = sqlExpressionFactory;
        }

        public SqlExpression Translate(
            SqlExpression instance, 
            MethodInfo method, 
            IReadOnlyList<SqlExpression> arguments, 
            IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            // Check if one of our DateDiff* methods were called
            if (UnifiedDbFunctionMethods.Methods.Contains(method))
            {
                var part = method.Name[8..].ToLowerInvariant();
                var start = arguments[1];
                var end = arguments[2];

                // JULIANDAY(end) - JULIANDAY(start)
                var daysDiff = Subtract(JulianDay(end), JulianDay(start));

                if (part == "day")
                {
                    return daysDiff;
                }
                else if (part == "year")
                {
                    return Divide(daysDiff, Constant(365.25));
                }
                else if (part == "month")
                {
                    return Divide(daysDiff, Constant(30.4375));
                }
                else if (part == "hour")
                {
                    return Multiply(daysDiff, Constant(24));
                }
                else if (part == "minute")
                {
                    return Multiply(daysDiff, Constant(1440));
                }
                else if (part == "second")
                {
                    return Multiply(daysDiff, Constant(86400));
                }
            }
            
            return null;
        }

        private SqlExpression Constant<T>(T value)
        {
            return _sqlExpressionFactory.Constant(value, typeof(T));
        }

        private SqlExpression JulianDay(SqlExpression argument)
        {
            // JULIANDAY(argument)
            return _sqlExpressionFactory.Function(
                "JULIANDAY",
                new[] { argument },
                nullable: true,
                argumentsPropagateNullability: new[] { false, true },
                typeof(double?),
                _typeMappingSource.FindMapping(typeof(double?)));
        }

        private SqlExpression Subtract(SqlExpression left, SqlExpression right)
        {
            // left - right
            return _sqlExpressionFactory.Subtract(left, right, _typeMappingSource.FindMapping(typeof(double?)));
        }

        private SqlExpression Multiply(SqlExpression left, SqlExpression right)
        {
            // right * right
            return _sqlExpressionFactory.Multiply(left, right, _typeMappingSource.FindMapping(typeof(double?)));
        }
        
        private SqlExpression Divide(SqlExpression left, SqlExpression right)
        {
            // right / right
            return _sqlExpressionFactory.Divide(left, right, _typeMappingSource.FindMapping(typeof(double?)));
        }
    }
}

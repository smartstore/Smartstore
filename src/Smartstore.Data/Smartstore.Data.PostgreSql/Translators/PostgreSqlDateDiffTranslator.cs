using System.Collections.Generic;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;
using Smartstore.Data.Providers;

namespace Smartstore.Data.PostgreSql.Translators
{
    //[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    internal class PostgreSqlDateDiffTranslator : IMethodCallTranslator
    {
        private readonly IRelationalTypeMappingSource _typeMappingSource;
        private readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;

        public PostgreSqlDateDiffTranslator(IRelationalTypeMappingSource typeMappingSource, NpgsqlSqlExpressionFactory sqlExpressionFactory)
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

                if (part == "year")
                {
                    // DATE_PART('year', end) - DATE_PART('year', start)
                    return DateDiff(part, start, end);
                }
                else if (part == "month")
                {
                    // --> years_diff * 12 + (DATE_PART('month', end) - DATE_PART('month', start))

                    // DATE_PART('year', end) - DATE_PART('year', start)
                    var yearsDiff = DateDiff("year", start, end);

                    // DATE_PART('month', end) - DATE_PART('month', start)
                    var monthsDiff = DateDiff("month", start, end);

                    // {years_diff} * 12 + ({monthsDiff})
                    return Add(Multiply(yearsDiff, Constant(12)), monthsDiff);
                }
                else
                {
                    // DATE_PART('day', end - start)
                    var daysDiff = DatePart("day", Subtract(end, start));

                    if (part == "day")
                    {
                        return daysDiff;
                    }
                    else if (part == "week")
                    {
                        // --> TRUNC(DATE_PART('day', end - start) / 7)
                        var arg = Divide(daysDiff, Constant(7m));
                        return Trunc(arg);
                    }

                    // DATE_PART('hour', end - start )
                    var hoursDiff = DatePart("hour", Subtract(end, start));
                    // days_diff * 24 + hours_diff
                    var totalHoursDiff = Add(Multiply(daysDiff, Constant(24)), hoursDiff);

                    if (part == "hour")
                    {
                        return totalHoursDiff;
                    }

                    // DATE_PART('minute', end - start )
                    var minutesDiff = DatePart("minute", Subtract(end, start));
                    // hours_diff * 60 + minutes_diff
                    var totalMinutesDiff = Add(Multiply(totalHoursDiff, Constant(60)), minutesDiff);

                    if (part == "minute")
                    {
                        return totalMinutesDiff;
                    }
                    else if (part == "second")
                    {
                        // --> minutes_diff * 60 + DATE_PART('minute', end - start )
                        return Add(Multiply(totalMinutesDiff, Constant(60)), minutesDiff);
                    }
                }
            }
            
            return null;
        }

        private SqlExpression Constant<T>(T value)
        {
            return _sqlExpressionFactory.Constant(value, typeof(T));
        }

        private SqlExpression DateDiff(string part, SqlExpression start, SqlExpression end)
        {
            // DATE_PART('part', end) - DATE_PART('part', start)
            return Subtract(DatePart(part, end), DatePart(part, start));
        }

        private SqlExpression DatePart(string part, SqlExpression argument)
        {
            // DATE_PART('part', argument)
            return _sqlExpressionFactory.Function(
                "DATE_PART",
                new[] { _sqlExpressionFactory.Fragment("'" + part + "'"), argument },
                nullable: true,
                argumentsPropagateNullability: new[] { false, true },
                typeof(double?),
                _typeMappingSource.FindMapping(typeof(double?)));
        }

        private SqlExpression Trunc(SqlExpression argument)
        {
            // TRUNC(number)
            return _sqlExpressionFactory.Function(
                "TRUNC",
                new[] { argument },
                nullable: true,
                argumentsPropagateNullability: new[] { true },
                typeof(int?),
                _typeMappingSource.FindMapping(typeof(decimal?)));
        }

        private SqlExpression Add(SqlExpression left, SqlExpression right)
        {
            // left + right
            return _sqlExpressionFactory.Add(left, right, _typeMappingSource.FindMapping(typeof(double?)));
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

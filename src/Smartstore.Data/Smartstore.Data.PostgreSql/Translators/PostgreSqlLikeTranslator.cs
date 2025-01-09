using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Storage.Internal;

namespace Smartstore.Data.PostgreSql.Translators
{
    /// <summary>
    /// Provides translation services for some PostgreSQL string functions:
    /// Because of case-insensitive collation, the calls to StartsWith/EndsWith/Like
    /// all must call ILike internally.
    /// </summary>
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    [Obsolete("Using citext now instead of non-deterministic collation")]
    internal class PostgreSqlLikeTranslator : IMethodCallTranslator
    {
        private static readonly MethodInfo StartsWith = typeof(string).GetRuntimeMethod(nameof(string.StartsWith), new[] { typeof(string) });
        private static readonly MethodInfo EndsWith = typeof(string).GetRuntimeMethod(nameof(string.EndsWith), new[] { typeof(string) });

        private const char LikeEscapeChar = '\\';

        private static readonly bool[][] TrueArrays =
        {
            [],
            [true],
            [true, true]
        };

        private readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;
        private readonly SqlExpression _whitespace;
        private readonly RelationalTypeMapping _textTypeMapping;

        public PostgreSqlLikeTranslator(
            NpgsqlTypeMappingSource typeMappingSource,
            NpgsqlSqlExpressionFactory sqlExpressionFactory,
            IModel model)
        {
            _sqlExpressionFactory = sqlExpressionFactory;
            _whitespace = _sqlExpressionFactory.Constant(
                @" \t\n\r",  // TODO: Complete this
                typeMappingSource.EStringTypeMapping);
            _textTypeMapping = typeMappingSource.FindMapping(typeof(string), model);
        }

        public SqlExpression Translate(
            SqlExpression instance, 
            MethodInfo method, 
            IReadOnlyList<SqlExpression> arguments, 
            IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (method == StartsWith)
            {
                return TranslateStartsEndsWith(instance, arguments[0], true);
            }

            if (method == EndsWith)
            {
                return TranslateStartsEndsWith(instance, arguments[0], false);
            }

            return null;
        }

        /// <summary>
        /// Copied from NpgsqlStringMethodTranslator and replaces "Like" calls with "ILike" calls.
        /// </summary>
        private SqlExpression TranslateStartsEndsWith(SqlExpression instance, SqlExpression pattern, bool startsWith)
        {
            var stringTypeMapping = InferTypeMapping(instance, pattern);

            instance = _sqlExpressionFactory.ApplyTypeMapping(instance, stringTypeMapping);
            pattern = _sqlExpressionFactory.ApplyTypeMapping(pattern, stringTypeMapping);

            if (pattern is SqlConstantExpression constantExpression)
            {
                // The pattern is constant. Aside from null, we escape all special characters (%, _, \)
                // in C# and send a simple LIKE
                return constantExpression.Value is string constantPattern
                    ? _sqlExpressionFactory.ILike(
                        instance,
                        _sqlExpressionFactory.Constant(
                            startsWith
                                ? EscapeLikePattern(constantPattern) + '%'
                                : '%' + EscapeLikePattern(constantPattern)))
                    : _sqlExpressionFactory.Like(instance, _sqlExpressionFactory.Constant(null, stringTypeMapping));
            }

            // The pattern is non-constant, we use LEFT or RIGHT to extract substring and compare.
            // For StartsWith we also first run a LIKE to quickly filter out most non-matching results (sargable, but imprecise
            // because of wildchars).
            SqlExpression leftRight = _sqlExpressionFactory.Function(
                startsWith ? "left" : "right",
                new[]
                {
                    instance,
                    _sqlExpressionFactory.Function(
                        "length",
                        new[] { pattern },
                        nullable: true,
                        argumentsPropagateNullability: TrueArrays[1],
                        typeof(int))
                },
                nullable: true,
                argumentsPropagateNullability: TrueArrays[2],
                typeof(string),
                stringTypeMapping);

            // LEFT/RIGHT of a citext return a text, so for non-default text mappings we apply an explicit cast.
            if (instance.TypeMapping != _textTypeMapping)
            {
                leftRight = _sqlExpressionFactory.Convert(leftRight, typeof(string), instance.TypeMapping);
            }

            // Also add an explicit cast on the pattern; this is only required because of
            // The following is only needed because of https://github.com/aspnet/EntityFrameworkCore/issues/19120
            var castPattern = pattern.TypeMapping == _textTypeMapping
                ? pattern
                : _sqlExpressionFactory.Convert(pattern, typeof(string), pattern.TypeMapping);

            return startsWith
                ? _sqlExpressionFactory.AndAlso(
                    _sqlExpressionFactory.ILike(
                        instance,
                        _sqlExpressionFactory.Add(
                            pattern,
                            _sqlExpressionFactory.Constant("%")),
                        _sqlExpressionFactory.Constant(string.Empty)),
                    _sqlExpressionFactory.Equal(leftRight, castPattern))
                : _sqlExpressionFactory.Equal(leftRight, castPattern);
        }

        private static RelationalTypeMapping InferTypeMapping(params SqlExpression[] expressions)
        {
            for (var i = 0; i < expressions.Length; i++)
            {
                var sql = expressions[i];
                if (sql.TypeMapping != null)
                {
                    return sql.TypeMapping;
                }
            }

            return null;
        }

        private static bool IsLikeWildChar(char c) => c == '%' || c == '_';

        private static string EscapeLikePattern(string pattern)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < pattern.Length; i++)
            {
                var c = pattern[i];
                if (IsLikeWildChar(c) || c == LikeEscapeChar)
                {
                    builder.Append(LikeEscapeChar);
                }

                builder.Append(c);
            }

            return builder.ToString();
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Smartstore.Utilities;

namespace Smartstore.Data.Caching2
{
    public interface IQueryKeyGenerator
    {
        DbCacheKey GenerateQueryKey(Expression expression, DbCachingPolicy policy);
    }

    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "It's ok")]
    public sealed class QueryKeyGenerator : IQueryKeyGenerator
    {
        private readonly IQueryContextFactory _queryContextFactory;
        private readonly QueryCompiler _queryCompiler;
        private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;

        private readonly static ConcurrentDictionary<string, DbCacheKey> _keysCache 
            = new ConcurrentDictionary<string, DbCacheKey>();

        public QueryKeyGenerator(IQueryContextFactory queryContextFactory, IQueryCompiler queryCompiler, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
        {
            if (queryCompiler is not QueryCompiler compiler)
            {
                throw new InvalidCastException($"Implementation type of '{nameof(IQueryCompiler)}' must be '{typeof(QueryCompiler)}'");
            }

            _queryCompiler = compiler;
            _queryContextFactory = queryContextFactory;
            _logger = logger;
        }

        public DbCacheKey GenerateQueryKey(Expression expression, DbCachingPolicy policy)
        {
            var queryKey = GetExpressionKey(expression);

            var key = _keysCache.GetOrAdd(queryKey.Hash, key => 
            { 
                // TODO: (core) EfCache: determine cache dependencies.
                return new DbCacheKey
                {
                    Key = queryKey.Key,
                    KeyHash = queryKey.Hash
                };
            });

            return key;
        }

        private (string Key, string Hash) GetExpressionKey(Expression expression)
        {
            var queryContext = _queryContextFactory.Create();

            expression = _queryCompiler.ExtractParameters(
                expression,
                queryContext,
                _logger,
                parameterize: false);

            var parameterValues = queryContext.ParameterValues;

            // Creating a Uniform Resource Identifier
            var expressionKey = $"hash://{ExpressionEqualityComparer.Instance.GetHashCode(expression)}";

            // If query has parameter add key values as uri-query string
            if (parameterValues.Count > 0)
            {
                var parameterStrings = parameterValues.Select(d => $"{d.Key}={d.Value?.GetHashCode()}");
                expressionKey += $"?{string.Join("&", parameterStrings)}";
            }

            return (expressionKey, $"{XxHashUnsafe.ComputeHash(expressionKey):X}");
        }
    }
}
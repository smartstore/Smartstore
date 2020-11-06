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
using Smartstore.Data.Caching.Internal;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Generates keys for cacheable query result sets.
    /// </summary>
    public interface IQueryKeyGenerator
    {
        /// <summary>
        /// Generates a unique key for a query expression.
        /// </summary>
        /// <param name="expression">The expression to create key for.</param>
        /// <param name="policy">The resolved caching policy.</param>
        /// <returns>The unique key.</returns>
        DbCacheKey GenerateQueryKey(Expression expression, DbCachingPolicy policy);
    }

    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "It's ok")]
    public class QueryKeyGenerator : IQueryKeyGenerator
    {
        private readonly IQueryContextFactory _queryContextFactory;
        private readonly QueryCompiler _queryCompiler;
        private readonly IDiagnosticsLogger<DbLoggerCategory.Query> _logger;

        private readonly static ConcurrentDictionary<uint, DbCacheKey> _keysCache 
            = new ConcurrentDictionary<uint, DbCacheKey>();

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

        /// <summary>
        /// Generates a unique key for a query dependency (from a JOIN, INCLUDE etc.).
        /// </summary>
        /// <param name="entityType">The dependant entity type.</param>
        /// <returns>The unique dependency key.</returns>
        public static string GenerateDependencyKey(Type entityType)
        {
            return entityType.Name;
        }

        public virtual DbCacheKey GenerateQueryKey(Expression expression, DbCachingPolicy policy)
        {
            var queryKey = GetExpressionKey(expression);

            var key = _keysCache.GetOrAdd(queryKey.Hash, key => 
            {
                var visitor = new DependencyVisitor();
                visitor.ExtractDependencies(expression);
                
                return new DbCacheKey
                {
                    Key = queryKey.Key,
                    KeyHash = $"{queryKey.Hash:X}",
                    EntitySets = visitor.Types.Select(x => GenerateDependencyKey(x)).ToArray()
                };
            });

            return key;
        }

        private (string Key, uint Hash) GetExpressionKey(Expression expression)
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

            return (expressionKey, XxHashUnsafe.ComputeHash(expressionKey));
        }
    }
}
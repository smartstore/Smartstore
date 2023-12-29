using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Smartstore.Data.Caching.Internal;
using Smartstore.Utilities;

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

        private readonly static ConcurrentDictionary<int, DbCacheKey> _keysCache
            = new ConcurrentDictionary<int, DbCacheKey>();

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
            var hash = GetExpressionHash(expression);

            var key = _keysCache.GetOrAdd(hash.CombinedHash, key =>
            {
                var visitor = new DependencyVisitor();
                visitor.ExtractDependencies(expression);

                return new DbCacheKey
                {
                    Key = hash.CombinedHashString,
                    EntitySets = visitor.Types.Select(GenerateDependencyKey).ToArray()
                };
            });

            return key;
        }

        private HashCodeCombiner GetExpressionHash(Expression expression)
        {
            var queryContext = _queryContextFactory.Create();

            expression = _queryCompiler.ExtractParameters(
                expression,
                queryContext,
                _logger,
                parameterize: false);

            var hashCode = ExpressionEqualityComparer.Instance.GetHashCode(expression);
            var combiner = new HashCodeCombiner(hashCode);

            // If query has parameters add to combiner
            if (queryContext.ParameterValues.Count > 0)
            {
                combiner.AddDictionary(queryContext.ParameterValues);
            }

            return combiner;
        }
    }
}
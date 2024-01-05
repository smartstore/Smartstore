using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Logging;
using Smartstore.ComponentModel;
using Smartstore.Data.Caching.Internal;

namespace Smartstore.Data.Caching
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Best place to intercept for caching")]
    internal class CachingQueryProvider : EntityQueryProvider
    {
        private readonly IDbCache _cache;
        private readonly IQueryKeyGenerator _queryKeyGenerator;
        private readonly ICurrentDbContext _currentContext;
        private readonly CachingOptionsExtension _extension;
        private readonly ILogger _logger;

        public CachingQueryProvider(
            IDbCache cache,
            IQueryKeyGenerator queryKeyGenerator,
            IQueryCompiler queryCompiler,
            ICurrentDbContext currentContext,
            IDbContextOptions options,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger)
            : base(queryCompiler)
        {
            _cache = cache;
            _queryKeyGenerator = queryKeyGenerator;
            _currentContext = currentContext;
            _extension = options.FindExtension<CachingOptionsExtension>();
            _logger = logger.Logger;
        }

        public override object Execute(Expression expression)
        {
            return ExecuteInternal(expression, base.Execute);
        }

        public override TResult Execute<TResult>(Expression expression)
        {
            return ExecuteInternal(expression, base.Execute<TResult>);
        }

        public override TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var cachingResult = ReadFromCache<TResult>(expression);
            if (cachingResult.HasResult)
            {
                return cachingResult.WrapAsyncResult(cachingResult.CacheEntry.Value);
            }

            var result = base.ExecuteAsync<TResult>(cachingResult.Expression, cancellationToken);

            if (!cachingResult.CanPut || cancellationToken.IsCancellationRequested)
            {
                return result;
            }

            using (var scope = new DbContextScope((HookingDbContext)_currentContext.Context, lazyLoading: false))
            {
                var cacheValue = cachingResult.ConvertQueryAsyncResult(result).Await();

                if (cacheValue.Count <= cachingResult.Policy.MaxRows.Value)
                {
                    var entry = new DbCacheEntry
                    {
                        Key = cachingResult.CacheKey,
                        Value = cacheValue.Value,
                        ValueType = cacheValue.Value?.GetType()
                    };

                    _cache.Put(cachingResult.CacheKey, entry, cachingResult.Policy);

                    Log(DbCachingEventId.QueryResultCached,
                        "Has put query result to cache. Key: {0}, Type: {1}, Policy: {2}.",
                        cachingResult.CacheKey.Key,
                        typeof(TResult),
                        cachingResult.Policy);
                }
                else
                {
                    Log(DbCachingEventId.MaxRowsExceeded,
                        "Max rows limit exceeded. Will not cache. Actual: {0}, Limit: {1} Key: {2}, Type: {3}.",
                        cacheValue.Count,
                        cachingResult.Policy.MaxRows.Value,
                        cachingResult.CacheKey.Key,
                        typeof(TResult));
                }

                return cachingResult.WrapAsyncResult(cacheValue.Value);
            }
        }

        /// <summary>
        /// Executes the query represented by a specified expression tree to cache its results.
        /// </summary>
        /// <param name="expression">An expression tree that represents a LINQ query.</param>
        /// <param name="queryExecutor">How to run the query.</param>
        /// <returns>The value that results from executing the specified query.</returns>
        private TResult ExecuteInternal<TResult>(Expression expression, Func<Expression, TResult> queryExecutor)
        {
            var cachingResult = ReadFromCache<TResult>(expression);
            if (cachingResult.HasResult)
            {
                return cachingResult.CachedValue;
            }

            var queryResult = queryExecutor(cachingResult.Expression);

            if (!cachingResult.CanPut)
            {
                return queryResult;
            }

            using (var scope = new DbContextScope((HookingDbContext)_currentContext.Context, lazyLoading: false))
            {
                var cacheValue = cachingResult.ConvertQueryResult(queryResult);

                if (cacheValue.Count <= cachingResult.Policy.MaxRows.Value)
                {
                    var entry = new DbCacheEntry
                    {
                        Key = cachingResult.CacheKey,
                        Value = cacheValue.Value,
                        ValueType = cacheValue.Value?.GetType()
                    };

                    _cache.Put(cachingResult.CacheKey, entry, cachingResult.Policy);

                    Log(DbCachingEventId.QueryResultCached,
                        "Has put query result to cache. Key: {0}, Type: {1}, Policy: {2}.",
                        cachingResult.CacheKey.Key,
                        typeof(TResult),
                        cachingResult.Policy);
                }
                else
                {
                    Log(DbCachingEventId.MaxRowsExceeded,
                        "Max rows limit exceeded. Will not cache. Actual: {0}, Limit: {1} Key: {2}, Type: {3}.",
                        cacheValue.Count,
                        cachingResult.Policy.MaxRows.Value,
                        cachingResult.CacheKey.Key,
                        typeof(TResult));
                }

                return (TResult)cacheValue.Value;
            }
        }

        private CachingResult<TResult> ReadFromCache<TResult>(Expression expression)
        {
            var visitor = new CachingExpressionVisitor(_currentContext.Context, _extension);
            expression = visitor.ExtractPolicy(expression);

            var policy = visitor.CachingPolicy;

            if (policy == null)
            {
                return new CachingResult<TResult>(expression, visitor);
            }

            var cachingResultType = typeof(CachingResult<,>).MakeGenericType(typeof(TResult), visitor.ElementType);
            var cachingResult = (CachingResult<TResult>)FastActivator.CreateInstance(cachingResultType, expression, visitor);

            cachingResult.CacheKey = _queryKeyGenerator.GenerateQueryKey(expression, policy);
            cachingResult.CacheEntry = _cache.Get(cachingResult.CacheKey, policy);

            if (cachingResult.CacheEntry != null)
            {
                Log(DbCachingEventId.CacheHit,
                    "Has read query result from cache. Key: {0}, Type: {1}, Policy: {2}.",
                    cachingResult.CacheKey.Key,
                    typeof(TResult),
                    policy);
            }

            return cachingResult;
        }

        private void Log(EventId eventId, string message, params object[] args)
        {
            if (_extension.EnableLogging)
            {
                _logger.LogDebug(eventId, message, args);
            }
        }
    }
}
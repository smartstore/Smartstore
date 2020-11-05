using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Smartstore.Threading;

namespace Smartstore.Data.Caching2
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Best place to intercept for caching")]
    internal class CachingQueryProvider : EntityQueryProvider
    {
        private readonly ICurrentDbContext _currentContext;
        private readonly DbCache _cache;
        private readonly IQueryKeyGenerator _queryKeyGenerator;

        public CachingQueryProvider(
            IQueryCompiler queryCompiler,
            ICurrentDbContext currentContext,
            DbCache cache,
            IQueryKeyGenerator queryKeyGenerator)
            : base(queryCompiler)
        {
            _currentContext = currentContext;
            _cache = cache;
            _queryKeyGenerator = queryKeyGenerator;
        }

        public override object Execute(Expression expression)
        {
            return ExecuteInternal(expression, q => base.Execute(q));
        }

        public override TResult Execute<TResult>(Expression expression)
        {
            return ExecuteInternal(expression, q => base.Execute<TResult>(q));
        }

        public override TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var cachingResult = ReadFromCache<TResult>(expression, true);
            if (cachingResult.HasResult)
            {
                return cachingResult.WrapAsyncResult(cachingResult.CacheEntry.Value);
            }

            var result = base.ExecuteAsync<TResult>(cachingResult.Expression, cancellationToken);

            if (!cachingResult.CanPut)
            {
                return result;
            }

            using (var scope = new DbContextScope((HookingDbContext)_currentContext.Context, lazyLoading: false))
            {
                var cacheValue = AsyncRunner.RunSync(() => cachingResult.ConvertQueryAsyncResult(result));

                var entry = new DbCacheEntry
                {
                    Value = cacheValue,
                    EntitySets = cachingResult.CacheKey.CacheDependencies.ToArray()
                };

                _cache.Put(cachingResult.CacheKey, entry, cachingResult.Policy);

                return cachingResult.WrapAsyncResult(cacheValue);
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
            var cachingResult = ReadFromCache<TResult>(expression, false);
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

                var entry = new DbCacheEntry
                {
                    Value = cacheValue,
                    EntitySets = cachingResult.CacheKey.CacheDependencies.ToArray()
                };

                _cache.Put(cachingResult.CacheKey, entry, cachingResult.Policy);

                return (TResult)cacheValue;
            }
        }

        private CachingResult<TResult> ReadFromCache<TResult>(Expression expression, bool forAsync)
        {
            var visitor = new CachingExpressionVisitor<TResult>(_currentContext.Context, forAsync);
            expression = visitor.ExtractPolicy(expression);

            var policy = visitor.CachingPolicy;

            if (policy == null)
            {
                return new CachingResult<TResult>(expression, visitor);
            }

            var cachingResultType = typeof(CachingResult<,>).MakeGenericType(typeof(TResult), visitor.EntityType);
            var cachingResult = (CachingResult<TResult>)Activator.CreateInstance(cachingResultType, expression, visitor);

            cachingResult.CacheKey = _queryKeyGenerator.GenerateQueryKey(expression, policy);
            cachingResult.CacheEntry = _cache.Get(cachingResult.CacheKey, policy);

            return cachingResult;
        }
    }
}



namespace Smartstore.Data.Caching.Internal
{
    internal class CachingResult<TResult, TEntity> : CachingResult<TResult>
    {
        public CachingResult(Expression expression, CachingExpressionVisitor visitor)
            : base(expression, visitor)
        {
        }

        public override TResult WrapAsyncResult(object cachedValue)
        {
            object wrappedResult = !Visitor.IsSequenceType
                ? Task.FromResult((TEntity)cachedValue)
                : ((IEnumerable<TEntity>)cachedValue ?? Enumerable.Empty<TEntity>()).ToAsyncEnumerable();

            return (TResult)wrappedResult;
        }

        public override (object Value, int Count) ConvertQueryResult(TResult queryResult)
        {
            if (!Visitor.IsSequenceType)
            {
                return (queryResult, 1);
            }
            else
            {
                var list = ((IEnumerable<TEntity>)queryResult).ToList();
                return (list, list.Count);
            }
        }

        public override async Task<(object Value, int Count)> ConvertQueryAsyncResult(TResult queryResult)
        {
            object result = queryResult;

            if (!Visitor.IsSequenceType)
            {
                return (await ((Task<TEntity>)result), 1);
            }
            else
            {
                var list = await ((IAsyncEnumerable<TEntity>)result).ToListAsync();
                return (list, list.Count);
            }
        }
    }

    /// <summary>
    /// Cached result data container.
    /// </summary>
    internal class CachingResult<TResult>
    {
        public CachingResult(Expression expression, CachingExpressionVisitor visitor)
        {
            Expression = expression;
            Visitor = visitor;
        }

        /// <summary>
        /// Wraps the cached result for async EF call.
        /// </summary>
        /// <returns>Result for EF.</returns>
        public virtual TResult WrapAsyncResult(object cachedValue)
            => throw new NotImplementedException();

        /// <summary>
        /// Converts the EF query result to be cacheable.
        /// </summary>
        public virtual (object Value, int Count) ConvertQueryResult(TResult queryResult)
            => throw new NotImplementedException();

        /// <summary>
        /// Converts the async EF query result to be cacheable.
        /// </summary>
        public virtual Task<(object Value, int Count)> ConvertQueryAsyncResult(TResult queryResult)
            => throw new NotImplementedException();

        /// <summary>
        /// The visited expression.
        /// </summary>
        public Expression Expression { get; }

        /// <summary>
        /// The visitor used to resolve policy and types.
        /// </summary>
        public CachingExpressionVisitor Visitor { get; }

        /// <summary>
        /// The cache key
        /// </summary>
        public DbCacheKey CacheKey { set; get; }

        /// <summary>
        ///  Retrieved cache entry
        /// </summary>
        public DbCacheEntry CacheEntry { set; get; }

        /// <summary>
        /// Strongly typed value from cache.
        /// </summary>
        public TResult CachedValue => (TResult)CacheEntry?.Value ?? default;

        /// <summary>
        /// The resolved caching policy
        /// </summary>
        public DbCachingPolicy Policy => Visitor.CachingPolicy;

        /// <summary>
        /// The handled entity type
        /// </summary>
        public Type EntityType => Visitor.ElementType;

        /// <summary>
        /// Could read cached entry from cache?
        /// </summary>
        public bool HasResult => CacheEntry != null;

        /// <summary>
        /// Can result be put to cache?
        /// </summary>
        public bool CanPut => CacheKey != null && Policy != null;
    }
}
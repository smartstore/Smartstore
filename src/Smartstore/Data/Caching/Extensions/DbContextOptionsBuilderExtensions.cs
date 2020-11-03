using System;
using System.Linq.Expressions;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Data.Caching.Internal;

namespace Smartstore.Data.Caching.Extensions
{
    public static class DbContextOptionsBuilderExtensions
    {
        /// <summary>
        /// Configures the context to support second level query caching.
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder UseSecondLevelCache(this DbContextOptionsBuilder optionsBuilder, IServiceProvider p, Action<EfCacheOptions> options = null)
        {
#pragma warning disable EF1001 // Internal EF Core API usage.
            optionsBuilder.ReplaceService<IQueryCompiler, MyQueryCompiler>();
#pragma warning restore EF1001 // Internal EF Core API usage.

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new CachingOptionsExtension());
            return optionsBuilder;
        }

        ///// <summary>
        ///// Configures the context to support second level query caching.
        ///// </summary>
        ///// <param name="optionsBuilder">The builder being used to configure the context.</param>
        ///// <returns>The options builder so that further configuration can be chained.</returns>
        //public static DbContextOptionsBuilder UseSecondLevelCache(this DbContextOptionsBuilder optionsBuilder)
        //{
        //    return optionsBuilder.UseSecondLevelCache(new MemoryCacheProvider());
        //}

        ///// <summary>
        ///// Configures the context to support second level query caching.
        ///// </summary>
        ///// <param name="optionsBuilder">The builder being used to configure the context.</param>
        ///// <param name="cacheProvider">The cache provider to storage query results.</param>
        ///// <returns>The options builder so that further configuration can be chained.</returns>
        //public static DbContextOptionsBuilder UseSecondLevelCache(this DbContextOptionsBuilder optionsBuilder, ICacheProvider cacheProvider)
        //{
        //    optionsBuilder.ReplaceService<IQueryCompiler, CustomQueryCompiler>();

        //    ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(new CachingOptionsExtension(cacheProvider));

        //    return optionsBuilder;
        //}
    }


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Ausstehend>")]
    internal class MyQueryCompiler : QueryCompiler
    {
        private readonly ICurrentDbContext _currentContext;
        private readonly EfCacheKeyGenerator _cacheKeyGenerator;
        private readonly EfCachePolicyResolver _policyResolver;

        public MyQueryCompiler(
            IQueryContextFactory queryContextFactory,
            ICompiledQueryCache compiledQueryCache,
            ICompiledQueryCacheKeyGenerator compiledQueryCacheKeyGenerator,
            IDatabase database,
            IDiagnosticsLogger<DbLoggerCategory.Query> logger,
            ICurrentDbContext currentContext,
            IEvaluatableExpressionFilter evaluatableExpressionFilter,
            IModel model,
            EfCacheKeyGenerator cacheKeyGenerator,
            EfCachePolicyResolver policyResolver)
            : base(queryContextFactory, compiledQueryCache, compiledQueryCacheKeyGenerator, database, logger, currentContext, evaluatableExpressionFilter, model)
        {
            _currentContext = currentContext;

            _cacheKeyGenerator = cacheKeyGenerator; // EngineContext.Current.Application.Services.Resolve<EfCacheKeyGenerator>();
            _policyResolver = policyResolver; // EngineContext.Current.Application.Services.Resolve<EfCachePolicyResolver>();
        }

        //public override Func<QueryContext, TResult> CreateCompiledAsyncQuery<TResult>(Expression query)
        //{
        //    return base.CreateCompiledAsyncQuery<TResult>(query);
        //}

        //public override Func<QueryContext, TResult> CreateCompiledQuery<TResult>(Expression query)
        //{
        //    return base.CreateCompiledQuery<TResult>(query);
        //}

        public override TResult Execute<TResult>(Expression query)
        {
            var type = typeof(TResult);
            var result = base.Execute<TResult>(query);

            if (result is IRelationalQueryingEnumerable rqe)
            {
                var cmd = rqe.CreateDbCommand();
                var policy = _policyResolver.GetEfCachePolicy(cmd.CommandText, _currentContext.Context);
                if (policy != null)
                {
                    var cacheKey = _cacheKeyGenerator.GenerateCacheKey(cmd, _currentContext.Context, policy);
                }
            }

            return result;
        }

        public override TResult ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken)
        {
            var result = base.ExecuteAsync<TResult>(query);
            return result;
        }
    }
}

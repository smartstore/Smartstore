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
using Smartstore.Data.Caching.Internal;

namespace Smartstore.Data.Caching2
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "<Ausstehend>")]
    internal class CachingQueryCompiler : QueryCompiler
    {
        private readonly ICurrentDbContext _currentContext;
        //private readonly EfCacheKeyGenerator _cacheKeyGenerator;
        //private readonly EfCachePolicyResolver _policyResolver;

        public CachingQueryCompiler(
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

            //_cacheKeyGenerator = cacheKeyGenerator; // EngineContext.Current.Application.Services.Resolve<EfCacheKeyGenerator>();
            //_policyResolver = policyResolver; // EngineContext.Current.Application.Services.Resolve<EfCachePolicyResolver>();
        }

        public override TResult Execute<TResult>(Expression query)
        {
            return base.Execute<TResult>(query);
        }

        public override TResult ExecuteAsync<TResult>(Expression query, CancellationToken cancellationToken)
        {
            return base.ExecuteAsync<TResult>(query);
        }
    }
}

using System;
using System.Diagnostics.CodeAnalysis;
using Autofac;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Smartstore.Data;
using Smartstore.Engine;

namespace Smartstore.Core.Data
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Support for multi-provider pooled factory")]
    internal sealed class PooledApplicationDbContextFactory<TContext> : IDbContextFactory<TContext>
        where TContext : HookingDbContext
    {
        private readonly IDbContextPool _pool;

        public PooledApplicationDbContextFactory(Type contextImplType)
        {
            Guard.NotNull(contextImplType, nameof(contextImplType));
            
            var poolType = typeof(IDbContextPool<>).MakeGenericType(contextImplType);
            _pool = (IDbContextPool)EngineContext.Current.Application.Services.Resolve(poolType);
        }

        public TContext CreateDbContext()
            => (TContext)new DbContextLease(_pool, standalone: true).Context;
    }
}

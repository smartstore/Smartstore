using System.Diagnostics.CodeAnalysis;
using Autofac;
using Microsoft.EntityFrameworkCore.Internal;
using Smartstore.Data;

namespace Smartstore.Core.Data
{
    /// <summary>
    /// Provides support for multi-provider-aware pooled DbContext factories.
    /// </summary>
    /// <remarks>After switching to FluentMigrator we won't need PooledApplicationDbContextFactory anymore. Maybe remove later.</remarks>
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Support for multi-provider pooled factory")]
    public sealed class PooledApplicationDbContextFactory<TContext, TContextImpl> : IDbContextFactory<TContext>
        where TContextImpl : HookingDbContext, TContext
        where TContext : DbContext
    {
        private readonly IDbContextPool<TContextImpl> _pool;

        public PooledApplicationDbContextFactory(IDbContextPool<TContextImpl> pool)
        {
            Guard.NotNull(pool, nameof(pool));
            _pool = pool;
        }

        public TContext CreateDbContext()
            => (TContext)new DbContextLease(_pool, standalone: true).Context;
    }

    /// <summary>
    /// Provides support for multi-provider-aware pooled DbContext factories.
    /// </summary>
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

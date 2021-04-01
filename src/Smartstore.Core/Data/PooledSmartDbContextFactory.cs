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
    internal sealed class PooledSmartDbContextFactory : IDbContextFactory<SmartDbContext>
    {
        private readonly IDbContextPool _pool;

        public PooledSmartDbContextFactory()
        {
            var poolType = typeof(IDbContextPool<>).MakeGenericType(DataSettings.Instance.DbFactory.SmartDbContextType);
            _pool = (IDbContextPool)EngineContext.Current.Application.Services.Resolve(poolType);
        }

        public SmartDbContext CreateDbContext()
            => (SmartDbContext)new DbContextLease(_pool, standalone: true).Context;
    }
}

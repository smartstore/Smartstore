using System;
using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data
{
    public class SimpleDbContextFactory<TContext> : IDbContextFactory<TContext>
        where TContext : DbContext
    {
        private readonly Func<TContext> _factory;
        
        public SimpleDbContextFactory(Func<TContext> factory)
        {
            _factory = Guard.NotNull(factory, nameof(factory));
        }

        public TContext CreateDbContext()
            => _factory();
    }
}

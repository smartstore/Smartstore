using Microsoft.EntityFrameworkCore;

namespace Smartstore.Data
{
    public class SimpleDbContextFactory<TContext> : IDbContextFactory<TContext>
        where TContext : DbContext
    {
        private readonly Func<TContext> _factory;

        /// <summary>
        /// Uses <see cref="DataSettings.DbFactory"/> to create <see cref="DbContext"/> instances.
        /// </summary>
        public SimpleDbContextFactory(int? commandTimeout)
        {
            var settings = DataSettings.Instance;
            _factory = () => settings.DbFactory.CreateDbContext<TContext>(settings.ConnectionString, commandTimeout);
        }

        /// <summary>
        /// Uses given <paramref name="factory"/> to create <see cref="DbContext"/> instances.
        /// </summary>
        public SimpleDbContextFactory(Func<TContext> factory)
        {
            _factory = Guard.NotNull(factory, nameof(factory));
        }

        public TContext CreateDbContext()
            => _factory();
    }
}

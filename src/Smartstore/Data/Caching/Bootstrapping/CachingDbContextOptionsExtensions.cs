using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;

namespace Smartstore.Data.Caching
{
    public static class CachingDbContextOptionsExtensions
    {
        /// <summary>
        /// Configures the context to support second level query caching.
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder UseSecondLevelCache<TContext>(
            this DbContextOptionsBuilder<TContext> optionsBuilder,
            Action<CachingDbContextOptionsBuilder> optionsAction = null)
            where TContext : HookingDbContext
            => (DbContextOptionsBuilder<TContext>)UseSecondLevelCache(
                (DbContextOptionsBuilder)optionsBuilder, optionsAction);

        /// <summary>
        /// Configures the context to support second level query caching.
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder UseSecondLevelCache(
            this DbContextOptionsBuilder optionsBuilder,
            Action<CachingDbContextOptionsBuilder> optionsAction = null)
        {
            Guard.NotNull(optionsBuilder, nameof(optionsBuilder));

            optionsBuilder.ReplaceService<IAsyncQueryProvider, CachingQueryProvider>();

            var extension = GetOrCreateExtension(optionsBuilder);
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            optionsAction?.Invoke(new CachingDbContextOptionsBuilder(optionsBuilder));

            return optionsBuilder;
        }

        private static CachingOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.Options.FindExtension<CachingOptionsExtension>()
                ?? new CachingOptionsExtension();
    }
}

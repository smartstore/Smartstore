using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Data.Caching
{
    /// <summary>
    /// Allows 2nd level caching configuration to be performed on <see cref="DbContextOptions" />.
    /// </summary>
    public class CachingDbContextOptionsBuilder : IHideObjectMembers
    {
        public CachingDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
        {
            Guard.NotNull(optionsBuilder, nameof(optionsBuilder));

            OptionsBuilder = optionsBuilder;
        }

        /// <summary>
        /// Gets the core options builder.
        /// </summary>
        protected virtual DbContextOptionsBuilder OptionsBuilder { get; }

        /// <summary>
        /// Should the debug level logging be enabled?
        /// </summary>
        public CachingDbContextOptionsBuilder EnableLogging(bool enable)
            => WithOption(e => e.WithUseLogging(enable));

        /// <summary>
        /// Configures the default (fallback) expiration timeout for cacheable entities.
        /// Default is 1 day.
        /// </summary>
        public CachingDbContextOptionsBuilder DefaultExpirationTimeout(TimeSpan timeout)
            => WithOption(e => e.WithDefaultExpirationTimeout(timeout));

        /// <summary>
        /// Configures the default (fallback) max rows limit.
        /// Default is 1000 rows.
        /// </summary>
        public CachingDbContextOptionsBuilder DefaultMaxRows(int maxRows)
            => WithOption(e => e.WithDefaultMaxRows(maxRows));

        /// <summary>
        /// Sets an option by cloning the extension used to store the settings. This ensures the builder
        /// does not modify options that are already in use elsewhere.
        /// </summary>
        /// <param name="setAction">An action to set the option.</param>
        /// <returns> The same builder instance so that multiple calls can be chained.</returns>
        protected virtual CachingDbContextOptionsBuilder WithOption(Func<CachingOptionsExtension, CachingOptionsExtension> setAction)
        {
            ((IDbContextOptionsBuilderInfrastructure)OptionsBuilder).AddOrUpdateExtension(
                setAction(OptionsBuilder.Options.FindExtension<CachingOptionsExtension>() ?? new CachingOptionsExtension()));

            return this;
        }
    }
}

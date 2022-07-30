using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Data.Providers
{
    public static class DbFactoryDbContextOptionsExtensions
    {
        /// <summary>
        /// Configures the context to resolve database connection settings from the current <see cref="DbFactory"/>
        /// provider (either SqlServer or MySql).
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder UseDbFactory(
            this DbContextOptionsBuilder optionsBuilder,
            Action<DbFactoryDbContextOptionsBuilder> optionsAction = null)
        {
            Guard.NotNull(optionsBuilder, nameof(optionsBuilder));

            var settings = DataSettings.Instance;

            if (settings.ConnectionString.IsEmpty())
            {
                throw new InvalidOperationException("No database connection string found in current data settings.");
            }

            if (settings.DbFactory == null)
            {
                throw new InvalidOperationException("No database factory instance found in current data settings");
            }

            return UseDbFactory(optionsBuilder, settings.DbFactory, settings.ConnectionString, optionsAction);
        }

        /// <summary>
        /// Configures the context to resolve database connection settings from the given <see cref="DbFactory"/>
        /// provider (either SqlServer or MySql).
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <param name="factory">The factory instance</param>
        /// <param name="connectionString">Describe</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder UseDbFactory(
            this DbContextOptionsBuilder optionsBuilder,
            DbFactory factory,
            string connectionString,
            Action<DbFactoryDbContextOptionsBuilder> optionsAction = null)
        {
            Guard.NotNull(optionsBuilder, nameof(optionsBuilder));
            Guard.NotNull(factory, nameof(factory));
            Guard.NotEmpty(connectionString, nameof(connectionString));

            var extension = optionsBuilder.Options.FindExtension<DbFactoryOptionsExtension>();
            var hasExtension = extension != null;

            if (!hasExtension)
            {
                extension = new DbFactoryOptionsExtension(optionsBuilder.Options);

                optionsBuilder
                    //.EnableSensitiveDataLogging(true)
                    .ConfigureWarnings(w =>
                    {
                        // EF throws when query is untracked otherwise
                        w.Ignore(CoreEventId.DetachedLazyLoadingWarning);

                        // Turn off the global query filter warning. We use global query filters only for ISoftDeletable.
                        // Related entities are not required but we do not want to configure that for each relationship.
                        // This way we can leave the configuration of the relationship to EF in many cases.
                        w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning);

                        #region Test
                        //// To identify the query that's triggering MultipleCollectionIncludeWarning.
                        //w.Throw(RelationalEventId.MultipleCollectionIncludeWarning);
                        //w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
                        #endregion
                    });
            }

            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);

            if (optionsAction != null)
            {
                optionsAction?.Invoke(new DbFactoryDbContextOptionsBuilder(optionsBuilder));
            }

            if (!hasExtension)
            {
                // Run this only once
                factory.ConfigureDbContext(optionsBuilder, connectionString);
            }

            return optionsBuilder;
        }

        private static DbFactoryOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.Options.FindExtension<DbFactoryOptionsExtension>()
                ?? new DbFactoryOptionsExtension(optionsBuilder.Options);
    }
}

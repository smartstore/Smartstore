using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace Smartstore.Data.Providers
{
    public static class DbFactoryDbContextOptionsExtensions
    {
        /// <summary>
        /// TODO: (core) Describe
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <returns>The options builder so that further configuration can be chained.</returns>
        public static DbContextOptionsBuilder UseDbFactory(
            this DbContextOptionsBuilder optionsBuilder,
            Action<DbFactoryDbContextOptionsBuilder> optionsAction = null)
        {
            // TODO: (core) ErrHandling
            var settings = DataSettings.Instance;
            return UseDbFactory(optionsBuilder, settings.DbFactory, settings.ConnectionString, optionsAction);
        }

        /// <summary>
        /// TODO: (core) Describe
        /// </summary>
        /// <param name="optionsBuilder">The builder being used to configure the context.</param>
        /// <param name="factory">Describe</param>
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

            optionsBuilder
                .ReplaceService<IConventionSetBuilder, FixedRuntimeConventionSetBuilder>()
                .ConfigureWarnings(w =>
                {
                    // EF throws when query is untracked otherwise
                    w.Ignore(CoreEventId.DetachedLazyLoadingWarning);

                    //// To identify the query that's triggering MultipleCollectionIncludeWarning.
                    ////w.Throw(RelationalEventId.MultipleCollectionIncludeWarning);
                    ////w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
                });

            var extension = GetOrCreateExtension(optionsBuilder).WithSomething(true);
            ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
            
            if (optionsAction != null)
            {
                optionsAction?.Invoke(new DbFactoryDbContextOptionsBuilder(optionsBuilder));
            }

            factory.ConfigureDbContext(optionsBuilder, connectionString);

            return optionsBuilder;
        }

        private static DbFactoryOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.Options.FindExtension<DbFactoryOptionsExtension>() 
                ?? new DbFactoryOptionsExtension();
    }
}

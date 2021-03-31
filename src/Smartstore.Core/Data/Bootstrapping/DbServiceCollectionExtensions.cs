using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Data.Caching;
using Smartstore.Engine;

namespace Smartstore.Core.Bootstrapping
{
    public static class DbServiceCollectionExtensions
    {
        /// <summary>
        /// Registers a scoped <see cref="DbQuerySettings" /> factory.
        /// </summary>
        public static IServiceCollection AddDbQuerySettings(this IServiceCollection services)
        {
            services.TryAddScoped<DbQuerySettings>(c => 
            {
                var storeContext = c.GetService<IStoreContext>();
                var aclService = c.GetService<IAclService>();

                return new DbQuerySettings(
                    aclService != null && !aclService.HasActiveAcl(),
                    storeContext?.IsSingleStoreMode() ?? false);
            });

            return services;
        }

        /// <summary>
        /// Registers a scoped <typeparamref name="TContext"/>
        /// and configures <see cref="DbContextOptions"/> according to application setting.
        /// </summary>
        /// <param name="appContext">The application context</param>
        /// <param name="enableCache">Whether to add the interceptor for 2nd level entity caching.</param>
        /// <param name="optionsAction">A custom options modifier</param>
        public static IServiceCollection AddApplicationDbContext<TContext>(this IServiceCollection services,
            IApplicationContext appContext,
            bool enableCache = true,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = null)
            where TContext : HookingDbContext
        {
            services.AddDbContext<TContext>(
                (p, o) => ConfigureDbContext(p, o, enableCache, optionsAction),
                ServiceLifetime.Scoped, ServiceLifetime.Singleton);

            return services;
        }

        /// <summary>
        /// Registers a pool for <typeparamref name="TContext"/>
        /// and configures <see cref="DbContextOptions"/> according to application setting.
        /// </summary>
        /// <param name="appContext">The application context</param>
        /// <param name="enableCaching">Whether to add the interceptor for 2nd level entity caching.</param>
        /// <param name="optionsAction">A custom options modifier</param>
        public static IServiceCollection AddApplicationDbContextPool<TContext>(this IServiceCollection services,
            IApplicationContext appContext,
            bool enableCaching = true,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = null)
            where TContext : HookingDbContext
        {
            services.AddDbContextPool<TContext>(
                (p, o) => ConfigureDbContext(p, o, enableCaching, optionsAction),
                appContext.AppConfiguration.DbContextPoolSize);

            return services;
        }

        /// <summary>
        /// Registers a pooling <see cref="IDbContextFactory{TContext}"/> as singleton, <typeparamref name="TContext"/> as scoped,
        /// and configures <see cref="DbContextOptions"/> according to application setting.
        /// </summary>
        /// <param name="appContext">The application context</param>
        /// <param name="enableCaching">Whether to add the interceptor for 2nd level entity caching.</param>
        /// <param name="optionsAction">A custom options modifier</param>
        public static IServiceCollection AddPooledApplicationDbContextFactory<TContext>(this IServiceCollection services, 
            IApplicationContext appContext,
            bool enableCaching = true,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = null)
            where TContext : HookingDbContext
        {
            services.AddPooledDbContextFactory<TContext>(
                (p, o) => ConfigureDbContext(p, o, enableCaching, optionsAction), 
                appContext.AppConfiguration.DbContextPoolSize);

            services.AddScoped<TContext>(sp => sp.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());

            return services;
        }

        private static void ConfigureDbContext(
            IServiceProvider p, 
            DbContextOptionsBuilder o,
            bool enableCaching,
            Action<IServiceProvider, DbContextOptionsBuilder> customOptionsAction)
        {
            var appContext = p.GetRequiredService<IApplicationContext>();
            var appConfig = appContext.AppConfiguration;
            var dbFactory = DataSettings.Instance.DbFactory;

            o = dbFactory.ConfigureDbContext(o, DataSettings.Instance.ConnectionString, appContext);

            o.ConfigureWarnings(w =>
            {
                // EF throws when query is untracked otherwise
                w.Ignore(CoreEventId.DetachedLazyLoadingWarning);

                // To identify the query that's triggering MultipleCollectionIncludeWarning.
                //w.Throw(RelationalEventId.MultipleCollectionIncludeWarning);
                //w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
            });

            // Replace default conventionset builder with a custom one that removes
            // "ServicePropertyDiscoveryConvention" convention. See INFO in FixedRuntimeConventionSetBuilder class.
            o.ReplaceService<IConventionSetBuilder, FixedRuntimeConventionSetBuilder>();

            if (enableCaching)
            {
                o.UseSecondLevelCache();
            }

            // Custom action from module or alike
            customOptionsAction?.Invoke(p, o);
        }
    }
}
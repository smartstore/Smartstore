using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
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
        /// Registers the open generic <see cref="DbMigrator{TContext}" /> as transient dependency.
        /// </summary>
        public static IServiceCollection AddDbMigrator(this IServiceCollection services)
        {
            services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
            services.AddTransient(typeof(DbMigrator<>));
            return services;
        }

        #region AddDbContext

        /// <summary>
        /// Registers a scoped <typeparamref name="TContext"/>
        /// and configures <see cref="DbContextOptions"/> according to application setting.
        /// </summary>
        /// <param name="enableCache">Whether to add the interceptor for 2nd level entity caching.</param>
        /// <param name="optionsBuilder">A custom options modifier</param>
        public static IServiceCollection AddApplicationDbContext<TContext>(
            this IServiceCollection services,
            bool enableCache = true,
            Action<IServiceProvider, DbContextOptionsBuilder, RelationalOptionsExtension> optionsBuilder = null)
            where TContext : HookingDbContext
        {
            Guard.NotNull(services, nameof(services));

            services.AddDbContext<TContext>(
                (p, o) => ConfigureDbContext(p, o, typeof(TContext), enableCache, optionsBuilder),
                ServiceLifetime.Scoped, 
                ServiceLifetime.Singleton);

            return services;
        }

        /// <summary>
        /// Registers a scoped <typeparamref name="TContext"/>
        /// and configures <see cref="DbContextOptions"/> according to application setting.
        /// </summary>
        /// <typeparam name="TContext">The class or interface that will be used to resolve the context from the container.</typeparam>
        /// <typeparam name="TContextImpl">The concrete implementation type to create</typeparam>
        /// <param name="enableCache">Whether to add the interceptor for 2nd level entity caching.</param>
        /// <param name="optionsBuilder">A custom options modifier</param>
        public static IServiceCollection AddApplicationDbContext<TContext, TContextImpl>(
            this IServiceCollection services,
            bool enableCache = true,
            Action<IServiceProvider, DbContextOptionsBuilder, RelationalOptionsExtension> optionsBuilder = null)
            where TContextImpl : HookingDbContext, TContext
            where TContext : class
        {
            Guard.NotNull(services, nameof(services));

            services.AddDbContext<TContext, TContextImpl>(
                (p, o) => ConfigureDbContext(p, o, typeof(TContext), enableCache, optionsBuilder),
                ServiceLifetime.Scoped, 
                ServiceLifetime.Singleton);

            return services;
        }

        #endregion

        #region AddDbContextPool

        /// <summary>
        /// Registers a pool for <typeparamref name="TContext"/>
        /// and configures <see cref="DbContextOptions"/> according to application setting.
        /// </summary>
        /// <param name="appContext">The application context</param>
        /// <param name="enableCaching">Whether to add the interceptor for 2nd level entity caching.</param>
        /// <param name="optionsBuilder">A custom options modifier</param>
        public static IServiceCollection AddApplicationDbContextPool<TContext>(
            this IServiceCollection services,
            IApplicationContext appContext,
            bool enableCaching = true,
            Action<IServiceProvider, DbContextOptionsBuilder, RelationalOptionsExtension> optionsBuilder = null)
            where TContext : HookingDbContext
        {
            Guard.NotNull(services, nameof(services));

            services.AddDbContextPool<TContext>(
                (p, o) => ConfigureDbContext(p, o, typeof(TContext), enableCaching, optionsBuilder),
                appContext.AppConfiguration.DbContextPoolSize);

            return services;
        }

        /// <summary>
        /// Registers a pool for <typeparamref name="TContext"/>
        /// and configures <see cref="DbContextOptions"/> according to application setting.
        /// </summary>
        /// <typeparam name="TContext">The class or interface that will be used to resolve the context from the container.</typeparam>
        /// <typeparam name="TContextImpl">The concrete implementation type to create</typeparam>
        /// <param name="poolSize">Sets the maximum number of instances retained by the pool.</param>
        /// <param name="enableCaching">Whether to add the interceptor for 2nd level entity caching.</param>
        /// <param name="optionsBuilder">A custom options modifier</param>
        public static IServiceCollection AddApplicationDbContextPool<TContext, TContextImpl>(
            this IServiceCollection services,
            int poolSize = 128,
            bool enableCaching = true,
            Action<IServiceProvider, DbContextOptionsBuilder, RelationalOptionsExtension> optionsBuilder = null)
            where TContextImpl : HookingDbContext, TContext
            where TContext : class
        {
            Guard.NotNull(services, nameof(services));

            services.AddDbContextPool<TContext, TContextImpl>(
                (p, o) => ConfigureDbContext(p, o, typeof(TContext), enableCaching, optionsBuilder),
                poolSize);

            return services;
        }

        #endregion

        #region AddPooledDbContextFactory

        /// <summary>
        /// Registers a pooling <see cref="IDbContextFactory{TContext}"/> as singleton, <typeparamref name="TContext"/> as scoped,
        /// and configures <see cref="DbContextOptions"/> according to application setting.
        /// </summary>
        /// <param name="poolSize">Sets the maximum number of instances retained by the pool.</param>
        /// <param name="enableCaching">Whether to add the interceptor for 2nd level entity caching.</param>
        /// <param name="optionsBuilder">A custom options modifier</param>
        public static IServiceCollection AddPooledApplicationDbContextFactory<TContext>(
            this IServiceCollection services, 
            int poolSize = 128,
            bool enableCaching = true,
            Action<IServiceProvider, DbContextOptionsBuilder, RelationalOptionsExtension> optionsBuilder = null)
            where TContext : HookingDbContext
        {
            Guard.NotNull(services, nameof(services));

            services.AddPooledDbContextFactory<TContext>(
                (p, o) => ConfigureDbContext(p, o, typeof(TContext), enableCaching, optionsBuilder), 
                poolSize);

            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());

            return services;
        }

        /// <summary>
        /// Registers a pooling <see cref="IDbContextFactory{TContext}"/> as singleton, <typeparamref name="TContext"/> as scoped,
        /// and configures <see cref="DbContextOptions"/> according to application setting.
        /// </summary>
        /// <param name="contextImplType">The type of database provider specific, derived context.</param>
        /// <param name="poolSize">Sets the maximum number of instances retained by the pool.</param>
        /// <param name="enableCaching">Whether to add the interceptor for 2nd level entity caching.</param>
        /// <param name="optionsBuilder">A custom options modifier.</param>
        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Support for multi-provider pooled factory")]
        public static IServiceCollection AddPooledApplicationDbContextFactory<TContext>(
            this IServiceCollection services,
            Type contextImplType,
            int poolSize = 128,
            bool enableCaching = true,
            Action<IServiceProvider, DbContextOptionsBuilder, RelationalOptionsExtension> optionsBuilder = null)
            where TContext : HookingDbContext
        {
            // INFO: TContextImpl cannot be a type parameter because type is defined in an assembly that is not referenced.
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(contextImplType, nameof(contextImplType));

            var addPoolingOptionsMethod = typeof(EntityFrameworkServiceCollectionExtensions)
                .GetMethod("AddPoolingOptions", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(contextImplType);

            // --> Call AddPoolingOptions<TContextImplementation>(services, optionsAction, poolSize)
            addPoolingOptionsMethod.Invoke(null, new object[] {
                services,
                (Action<IServiceProvider, DbContextOptionsBuilder>)DbProviderConfigurer,
                poolSize });

            // --> Call services.TryAddSingleton<IDbContextPool<TContextImpl>, DbContextPool<TContextImpl>>()
            var contextPoolServiceType = typeof(IDbContextPool<>).MakeGenericType(contextImplType);
            var contextPoolImplType = typeof(DbContextPool<>).MakeGenericType(contextImplType);
            services.TryAddSingleton(contextPoolServiceType, contextPoolImplType);

            // --> Register provider-aware IDbContextFactory<TContext>
            services.TryAddSingleton(c => 
            {
                var pool = c.GetRequiredService(contextPoolServiceType);
                var pooledFactoryType = typeof(PooledApplicationDbContextFactory<,>).MakeGenericType(typeof(TContext), contextImplType);

                var instance = Activator.CreateInstance(pooledFactoryType, new object[] { pool });
                return (IDbContextFactory<TContext>)instance;
            });

            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());

            return services;

            void DbProviderConfigurer(IServiceProvider p, DbContextOptionsBuilder o)
            {
                ConfigureDbContext(p, o, typeof(TContext), enableCaching, optionsBuilder);
            }
        }

        #endregion

        private static void ConfigureDbContext(
            IServiceProvider p,
            DbContextOptionsBuilder builder,
            Type invariantDbContextType,
            bool enableCaching,
            Action<IServiceProvider, DbContextOptionsBuilder, RelationalOptionsExtension> customOptionsBuilder)
        {
            var appContext = p.GetRequiredService<IApplicationContext>();
            var appConfig = appContext.AppConfiguration;
            var dbFactory = DataSettings.Instance.DbFactory;

            builder = dbFactory
                .ConfigureDbContext(builder, DataSettings.Instance.ConnectionString, appContext)
                .ConfigureWarnings(w =>
                {
                    // EF throws when query is untracked otherwise
                    w.Ignore(CoreEventId.DetachedLazyLoadingWarning);

                    //// To identify the query that's triggering MultipleCollectionIncludeWarning.
                    ////w.Throw(RelationalEventId.MultipleCollectionIncludeWarning);
                    ////w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
                })
                // Replace default ConventionSet builder with a custom one that removes
                // "ServicePropertyDiscoveryConvention" convention. See INFO in FixedRuntimeConventionSetBuilder class.
                .ReplaceService<IConventionSetBuilder, FixedRuntimeConventionSetBuilder>();

            var options = builder.Options;
            var relationalOptions = options.Extensions.OfType<RelationalOptionsExtension>().FirstOrDefault();
            if (relationalOptions != null)
            {
                // TODO: (core) RelationalOptionsExtension is always cloned and cannot be modified this way. Find another way.
                if (appConfig.DbCommandTimeout.HasValue)
                {
                    relationalOptions = relationalOptions.WithCommandTimeout(appConfig.DbCommandTimeout.Value);
                }

                relationalOptions = relationalOptions.WithMigrationsHistoryTableName("__EFMigrationsHistory_" + options.ContextType.Name);
            }

            if (enableCaching)
            {
                builder.UseSecondLevelCache();
            }

            // Custom action from module or alike
            customOptionsBuilder?.Invoke(p, builder, relationalOptions);

            // Enables us to initialize/migrate all active contexts during app startup.
            DbMigrationManager.Instance.RegisterDbContext(invariantDbContextType);
        }
    }
}
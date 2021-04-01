using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Internal;
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

        #region AddDbContext

        /// <summary>
        /// Registers a scoped <typeparamref name="TContext"/>
        /// and configures <see cref="DbContextOptions"/> according to application setting.
        /// </summary>
        /// <param name="enableCache">Whether to add the interceptor for 2nd level entity caching.</param>
        /// <param name="optionsAction">A custom options modifier</param>
        public static IServiceCollection AddApplicationDbContext<TContext>(
            this IServiceCollection services,
            bool enableCache = true,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = null)
            where TContext : HookingDbContext
        {
            Guard.NotNull(services, nameof(services));

            services.AddDbContext<TContext>(
                (p, o) => ConfigureDbContext(p, o, enableCache, optionsAction),
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
        /// <param name="optionsAction">A custom options modifier</param>
        public static IServiceCollection AddApplicationDbContext<TContext, TContextImpl>(
            this IServiceCollection services,
            bool enableCache = true,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = null)
            where TContextImpl : HookingDbContext, TContext
            where TContext : class
        {
            Guard.NotNull(services, nameof(services));

            services.AddDbContext<TContext, TContextImpl>(
                (p, o) => ConfigureDbContext(p, o, enableCache, optionsAction),
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
        /// <param name="optionsAction">A custom options modifier</param>
        public static IServiceCollection AddApplicationDbContextPool<TContext>(
            this IServiceCollection services,
            IApplicationContext appContext,
            bool enableCaching = true,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = null)
            where TContext : HookingDbContext
        {
            Guard.NotNull(services, nameof(services));

            services.AddDbContextPool<TContext>(
                (p, o) => ConfigureDbContext(p, o, enableCaching, optionsAction),
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
        /// <param name="optionsAction">A custom options modifier</param>
        public static IServiceCollection AddApplicationDbContextPool<TContext, TContextImpl>(
            this IServiceCollection services,
            int poolSize = 128,
            bool enableCaching = true,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = null)
            where TContextImpl : HookingDbContext, TContext
            where TContext : class
        {
            Guard.NotNull(services, nameof(services));

            services.AddDbContextPool<TContext, TContextImpl>(
                (p, o) => ConfigureDbContext(p, o, enableCaching, optionsAction),
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
        /// <param name="optionsAction">A custom options modifier</param>
        public static IServiceCollection AddPooledApplicationDbContextFactory<TContext>(
            this IServiceCollection services, 
            int poolSize = 128,
            bool enableCaching = true,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = null)
            where TContext : HookingDbContext
        {
            Guard.NotNull(services, nameof(services));

            services.AddPooledDbContextFactory<TContext>(
                (p, o) => ConfigureDbContext(p, o, enableCaching, optionsAction), 
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
        /// <param name="optionsAction">A custom options modifier</param>
        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Support for multi-provider pooled factory")]
        public static IServiceCollection AddPooledApplicationDbContextFactory<TContext>(
            this IServiceCollection services,
            Type contextImplType,
            int poolSize = 128,
            bool enableCaching = true,
            Action<IServiceProvider, DbContextOptionsBuilder> optionsAction = null)
            where TContext : HookingDbContext
        {
            Guard.NotNull(services, nameof(services));
            Guard.NotNull(contextImplType, nameof(contextImplType));

            // INFO: TContextImpl cannot be a type parameter because type is defined in an assembly that is not referenced.

            var addPoolingOptionsMethod = typeof(EntityFrameworkServiceCollectionExtensions)
                .GetMethod("AddPoolingOptions", BindingFlags.NonPublic | BindingFlags.Static)
                .MakeGenericMethod(contextImplType);

            // --> Call AddPoolingOptions<TContextImplementation>(services, optionsAction, poolSize)
            addPoolingOptionsMethod.Invoke(null, new object[] { 
                services, 
                (Action<IServiceProvider, DbContextOptionsBuilder>)DbProviderConfigurer, 
                poolSize });

            // --> Call services.TryAddSingleton<IDbContextPool<TContextImpl>, DbContextPool<TContextImpl>>()
            services.TryAddSingleton(
                typeof(IDbContextPool<>).MakeGenericType(contextImplType),
                typeof(DbContextPool<>).MakeGenericType(contextImplType));

            services.TryAddSingleton<IDbContextFactory<SmartDbContext>, PooledSmartDbContextFactory>();

            services.AddScoped(sp => sp.GetRequiredService<IDbContextFactory<SmartDbContext>>().CreateDbContext());

            return services;

            void DbProviderConfigurer(IServiceProvider p, DbContextOptionsBuilder o)
            {
                ConfigureDbContext(p, o, enableCaching, optionsAction);
            }
        }

        #endregion

        private static void ConfigureDbContext(
            IServiceProvider p, 
            DbContextOptionsBuilder o,
            bool enableCaching,
            Action<IServiceProvider, DbContextOptionsBuilder> customOptionsAction)
        {
            var appContext = p.GetRequiredService<IApplicationContext>();
            var appConfig = appContext.AppConfiguration;
            var dbFactory = DataSettings.Instance.DbFactory;

            o = dbFactory
                .ConfigureDbContext(o, DataSettings.Instance.ConnectionString, appContext)
                .ConfigureWarnings(w =>
                {
                    // EF throws when query is untracked otherwise
                    w.Ignore(CoreEventId.DetachedLazyLoadingWarning);

                    // To identify the query that's triggering MultipleCollectionIncludeWarning.
                    //w.Throw(RelationalEventId.MultipleCollectionIncludeWarning);
                    //w.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
                })
                // Replace default conventionset builder with a custom one that removes
                // "ServicePropertyDiscoveryConvention" convention. See INFO in FixedRuntimeConventionSetBuilder class.
                .ReplaceService<IConventionSetBuilder, FixedRuntimeConventionSetBuilder>();

            if (enableCaching)
            {
                o.UseSecondLevelCache();
            }

            // Custom action from module or alike
            customOptionsAction?.Invoke(p, o);
        }
    }
}
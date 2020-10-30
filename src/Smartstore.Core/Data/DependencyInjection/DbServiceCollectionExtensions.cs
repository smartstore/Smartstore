using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Data.Caching;
using Smartstore.Engine;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DbServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IDbContextFactory{TContext}"/> as singleton, <typeparamref name="TContext"/> as scoped,
        /// and configures <see cref="DbContextOptions"/>.
        /// </summary>
        public static IServiceCollection AddDbContext<TContext>(this IServiceCollection services, IApplicationContext appContext)
            where TContext : HookingDbContext
        {
            //services.AddDbContextFactory<SmartDbContext>();
            //services.AddDbContextPool<SmartDbContext>(ConfigureDbContext, appConfig.DbContextPoolSize);
            services.AddPooledDbContextFactory<TContext>(ConfigureDbContext, appContext.AppConfiguration.DbContextPoolSize);
            services.AddScoped<TContext>(sp => sp.GetRequiredService<IDbContextFactory<TContext>>().CreateDbContext());

            return services;
        }

        private static void ConfigureDbContext(IServiceProvider p, DbContextOptionsBuilder o)
        {
            var appContext = p.GetRequiredService<IApplicationContext>();
            var appConfig = appContext.AppConfiguration;

            //// TODO: (core) Fetch services which SmartDbContext depends on from IInfrastructure<IServiceProvider>
            //o.UseSqlServer(appContext.Configuration.GetConnectionString("DefaultConnection"), sql =>
            //{
            //    if (appConfig.DbCommandTimeout.HasValue)
            //    {
            //        sql.CommandTimeout(appConfig.DbCommandTimeout.Value);
            //    }
            //});
            o.UseSqlServer(DataSettings.Instance.ConnectionString, sql =>
            {
                if (appConfig.DbCommandTimeout.HasValue)
                {
                    sql.CommandTimeout(appConfig.DbCommandTimeout.Value);
                }
            })
            .AddInterceptors(p.GetRequiredService<EfCacheInterceptor>())
            .ConfigureWarnings(w =>
            {
                // EF throws when query is untracked otherwise
                w.Ignore(CoreEventId.DetachedLazyLoadingWarning);
            });
        }
    }
}
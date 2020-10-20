using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Smartstore.Core.Data;
using Smartstore.Data;
using Smartstore.Engine;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DbServiceCollectionExtensions
    {
        public static IServiceCollection AddSmartDbContext(this IServiceCollection services, IApplicationContext appContext)
        {
            //services.AddDbContextFactory<SmartDbContext>();
            services.AddPooledDbContextFactory<SmartDbContext>(ConfigureDbContext, appContext.AppConfiguration.DbContextPoolSize);
            //services.AddDbContextPool<SmartDbContext>(ConfigureDbContext, appConfig.DbContextPoolSize);
            services.AddScoped<SmartDbContext>(sp => sp.GetRequiredService<IDbContextFactory<SmartDbContext>>().CreateDbContext());

            return services;
        }

        private static void ConfigureDbContext(IServiceProvider p, DbContextOptionsBuilder o)
        {
            var appContext = p.GetRequiredService<IApplicationContext>();
            var appConfig = appContext.AppConfiguration;

            //// TODO: (core) Fetch ConnectionString from tenant settings
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
            .ConfigureWarnings(w =>
            {
                // EF throws when query is untracked otherwise
                w.Ignore(CoreEventId.DetachedLazyLoadingWarning);
            });
        }
    }
}

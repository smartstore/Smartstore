using System;
using System.Linq;
using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;
using Smartstore.Data;
using Smartstore.Engine;

namespace Smartstore.Core.Bootstrapping
{
    internal class DataSettingsConnectionStringReader : IConnectionStringReader
    {
        public int Priority => 0;
        public string GetConnectionString(string connectionStringOrName) 
            => DataSettings.Instance.ConnectionString;
    }

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
        public static IServiceCollection AddDbMigrator(this IServiceCollection services, IApplicationContext appContext)
        {
            services
                .AddFluentMigratorCore()
                .AddScoped<IConnectionStringReader, DataSettingsConnectionStringReader>()
                .AddScoped<IProcessorAccessor, MigrationProcessorAccessor>()
                .AddTransient<IDatabaseInitializer, DatabaseInitializer>()
                .AddTransient(typeof(DbMigrator<>))
                .AddTransient(typeof(DbMigrator2<>))
                .ConfigureRunner(builder => 
                {
                    // TODO: (mg) (core) I highly doubt that we'll ever need FluentMigrator's built-in scanning capabilities.
                    //       WE do the scans. Thoroughly check if I miss something and strip this off completely.
                    var migrationAssemblies = appContext.TypeScanner.FindTypes<MigrationBase>()// TODO: (mg) (core) ignore inactive modules when ready.
                        .Select(x => x.Assembly)
                        .Where(x => !x.FullName.Contains("FluentMigrator.Runner"))
                        .Distinct()
                        .ToArray();

                    builder
                        .AddSqlServer()
                        .AddMySql5()
                        .WithVersionTable(new MigrationHistory())
                        .WithGlobalCommandTimeout(TimeSpan.FromSeconds(appContext.AppConfiguration.DbMigrationCommandTimeout ?? 120))
                        .ScanIn(migrationAssemblies)
                            .For.Migrations()
                            .For.EmbeddedResources();
                })
                .Configure<FluentMigratorLoggerOptions>(o =>
                {
                    o.ShowSql = false;  // TODO: (mg) (core) Security risk logging SQL. Config has no effect here. Loggs like crazy.
                    o.ShowElapsedTime = false;
                });

            return services;
        }
    }
}
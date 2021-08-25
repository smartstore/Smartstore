using System;
using System.Linq;
using FluentMigrator;
using FluentMigrator.Runner;
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
            // TODO: (mg) (core) Some parts of FluentMigrator are required during installation (VersionInfo), but we can't register 
            // this whole stuff during installation (because of DbFactory which is available later in the install pipeline).
            // Find a way to use VersionInfo dependency during installation. TBD with MC.
            
            services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
            services.AddTransient(typeof(DbMigrator<>));

            // Fluent migrator.
            var migrationAssemblies = appContext.TypeScanner.FindTypes<MigrationBase>(true, true)
                .Select(x => x.Assembly)
                .Where(x => !x.FullName.Contains("FluentMigrator.Runner"))
                .Distinct()
                .ToArray();
            //$"assemblies {string.Join(", ", migrationAssemblies.Select(x => x.GetName().Name))}".Dump();

            var dataSettings = DataSettings.Instance;

            void migrationRunner(IMigrationRunnerBuilder rb)
            {
                var migrationTimeout = appContext.AppConfiguration.DbMigrationCommandTimeout ?? 60;
                var dbSystemName = dataSettings.DbFactory.DbSystem.ToString();

                rb = dbSystemName.EqualsNoCase("MySql") ? rb.AddMySql5() : rb.AddSqlServer();

                rb.WithVersionTable(new MigrationHistory())
                    .WithGlobalConnectionString(dataSettings.ConnectionString)
                    .WithGlobalCommandTimeout(TimeSpan.FromSeconds(migrationTimeout))
                    .ScanIn(migrationAssemblies)
                        .For.Migrations()
                        .For.EmbeddedResources();
            }

            services
                .AddFluentMigratorCore()
                .AddScoped<IProcessorAccessor, MigrationProcessorAccessor>()
                .AddTransient(typeof(DbMigrator2<>))
                //.AddSingleton<IConventionSet, MigrationConventionSet>()
                //.AddLogging(lb => lb.AddFluentMigratorConsole())
                //.Configure<RunnerOptions>(opt =>
                //{
                //    opt.Profile = "Development"   // Selectively apply migrations depending on whatever.
                //    opt.Tags = new[] { "UK", "Production" }   // Used to filter migrations by tags.
                //})
                .ConfigureRunner(migrationRunner)
                .Configure<FluentMigratorLoggerOptions>(o =>
                {
                    o.ShowSql = false;  // // TODO: (mg) (core) Security risk logging SQL. Config has no effect here. Loggs like crazy.
                    o.ShowElapsedTime = true;
                });

            return services;
        }
    }
}
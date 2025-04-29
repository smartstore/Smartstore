using FluentMigrator;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Generators;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Processors;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data;

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
        /// Registers the open generic <see cref="DbMigrator{TContext}" /> as transient dependency.
        /// </summary>
        public static IServiceCollection AddDbMigrator(this IServiceCollection services, IApplicationContext appContext)
        {
            services
                .AddFluentMigratorCore()
                .AddScoped<IConnectionStringReader, DataSettingsConnectionStringReader>()
                .AddScoped<IProcessorAccessor, MigrationProcessorAccessor>()
                .AddScoped<IConventionSetAccessor, SmartConventionSetAccessor>()
                .AddTransient<IDatabaseInitializer, DatabaseInitializer>()
                .AddTransient(typeof(IMigrationTable<>), typeof(MigrationTable<>))
                .AddTransient(typeof(DbMigrator<>))
                .ConfigureRunner(builder =>
                {
                    builder
                        //.AddSqlServer()
                        .AddSqlServer2014()
                        .AddMySql5()
                        .AddPostgres()
                        .AddSQLite()
                        .WithVersionTable(new MigrationHistory())
                        .WithGlobalCommandTimeout(TimeSpan.FromSeconds(appContext.AppConfiguration.DbMigrationCommandTimeout ?? 120));
                })
                .Configure<SelectingGeneratorAccessorOptions>(opt =>
                {
                    switch (DataSettings.Instance.DbFactory.DbSystem)
                    {
                        case DbSystemType.SqlServer:
                            opt.GeneratorId = GeneratorIdConstants.SqlServer;
                            break;
                        case DbSystemType.MySql:
                            opt.GeneratorId = GeneratorIdConstants.MySql;
                            break;
                        case DbSystemType.PostgreSql:
                            opt.GeneratorId = GeneratorIdConstants.PostgreSQL;
                            break;
                        case DbSystemType.SQLite:
                            opt.GeneratorId = GeneratorIdConstants.SQLite;
                            break;
                    }                    
                })
                .Configure<FluentMigratorLoggerOptions>(o =>
                {
                    o.ShowSql = false;
                    o.ShowElapsedTime = false;
                });

            return services;
        }
    }
}
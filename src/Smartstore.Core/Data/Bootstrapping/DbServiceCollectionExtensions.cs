using FluentMigrator.Runner;
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
                .Configure<FluentMigratorLoggerOptions>(o =>
                {
                    o.ShowSql = false;  // TODO: (mg) (core) Security risk logging SQL. Find a way to get configuration working. Logs like crazy.
                    o.ShowElapsedTime = false;
                });

            return services;
        }
    }
}
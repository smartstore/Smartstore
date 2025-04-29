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
                    var provider = DataSettings.Instance.DbFactory?.DbSystem;

                    if (provider == null || provider == DbSystemType.SqlServer)     builder.AddSqlServer2014();
                    if (provider == null || provider == DbSystemType.MySql)         builder.AddMySql5();
                    if (provider == null || provider == DbSystemType.PostgreSql)    builder.AddPostgres();
                    if (provider == null || provider == DbSystemType.SQLite)        builder.AddSQLite();

                    builder
                        .WithVersionTable(new MigrationHistory())
                        .WithGlobalCommandTimeout(TimeSpan.FromSeconds(appContext.AppConfiguration.DbMigrationCommandTimeout ?? 120));
                })
                .Configure<SelectingGeneratorAccessorOptions>(opt =>
                {
                    var provider = DataSettings.Instance.DbFactory.DbSystem;
                    opt.GeneratorId = provider switch
                    {
                        DbSystemType.SqlServer =>   GeneratorIdConstants.SqlServer,
                        DbSystemType.MySql =>       GeneratorIdConstants.MySql,
                        DbSystemType.PostgreSql =>  GeneratorIdConstants.PostgreSQL,
                        DbSystemType.SQLite =>      GeneratorIdConstants.SQLite,
                        _ => throw new InvalidOperationException($"Unknown database provider: {provider}")
                    };
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
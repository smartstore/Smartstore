using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using MySqlConnector;
using Smartstore.Data.Providers;
using Smartstore.Engine;

// Add-Migration Initial -Context MySqlSmartDbContext -Project Smartstore.Data.MySql

namespace Smartstore.Data.SqlServer
{
    internal class MySqlDbFactory : DbFactory
    {
        public override DbSystemType DbSystem { get; } = DbSystemType.MySql;

        public override Type SmartDbContextType => typeof(MySqlSmartDbContext);

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(string connectionString)
            => new MySqlConnectionStringBuilder(connectionString) { AllowUserVariables = true, UseAffectedRows = false };

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(
            string server,
            string database,
            string userId,
            string password)
        {
            Guard.NotEmpty(server, nameof(server));
            Guard.NotEmpty(database, nameof(database));

            var builder = new MySqlConnectionStringBuilder
            {
                Server = server,
                Database = database,
                UserID = userId,
                Password = password,
                Pooling = true,
                MinimumPoolSize = 1,
                MaximumPoolSize = 100,
                AllowUserVariables = true,
                UseAffectedRows = false
            };

            return builder;
        }

        public override DataProvider CreateDataProvider(DatabaseFacade database)
            => new MySqlDataProvider(database);

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString, IApplicationContext appContext)
        {
            return builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), mySql =>
            {
                //sql.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(100), null);
            });
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString)
        {
            return builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), sql =>
            {
                var extension = builder.Options.FindExtension<DbFactoryOptionsExtension>();

                if (extension != null)
                {
                    sql
                        .CommandTimeout(extension.CommandTimeout)
                        .ExecutionStrategy(extension.ExecutionStrategyFactory)
                        .MigrationsAssembly(extension.MigrationsAssembly)
                        .MigrationsHistoryTable(extension.MigrationsHistoryTableName, extension.MigrationsHistoryTableSchema)
                        .UseRelationalNulls(extension.UseRelationalNulls);

                    if (extension.MinBatchSize.HasValue)
                        sql.MinBatchSize(extension.MinBatchSize.Value);

                    if (extension.MaxBatchSize.HasValue)
                        sql.MaxBatchSize(extension.MaxBatchSize.Value);

                    if (extension.QuerySplittingBehavior.HasValue)
                        sql.UseQuerySplittingBehavior(extension.QuerySplittingBehavior.Value);
                }
            });
        }
    }
}

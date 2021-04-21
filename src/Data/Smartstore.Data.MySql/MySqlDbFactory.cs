using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using MySqlConnector;
using Smartstore.Core.Data;
using Smartstore.Data.Providers;

// Add-Migration Initial -Context MySqlSmartDbContext -Project Smartstore.Data.MySql

namespace Smartstore.Data.MySql
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

        public override HookingDbContext CreateApplicationDbContext(string connectionString, int? commandTimeout = null, string migrationHistoryTableName = null)
        {
            Guard.NotEmpty(connectionString, nameof(connectionString));

            var optionsBuilder = new DbContextOptionsBuilder<MySqlSmartDbContext>()
                .ReplaceService<IConventionSetBuilder, FixedRuntimeConventionSetBuilder>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), sql => 
                {
                    sql.CommandTimeout(commandTimeout);
                    sql.MigrationsHistoryTable(migrationHistoryTableName);
                });

            return new MySqlSmartDbContext(optionsBuilder.Options);
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString)
        {
            return builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), sql =>
            {
                var extension = builder.Options.FindExtension<DbFactoryOptionsExtension>();

                if (extension != null)
                {
                    if (extension.CommandTimeout.HasValue)
                        sql.CommandTimeout(extension.CommandTimeout.Value);

                    if (extension.MinBatchSize.HasValue)
                        sql.MinBatchSize(extension.MinBatchSize.Value);

                    if (extension.MaxBatchSize.HasValue)
                        sql.MaxBatchSize(extension.MaxBatchSize.Value);

                    if (extension.QuerySplittingBehavior.HasValue)
                        sql.UseQuerySplittingBehavior(extension.QuerySplittingBehavior.Value);

                    if (extension.UseRelationalNulls.HasValue)
                        sql.UseRelationalNulls(extension.UseRelationalNulls.Value);

                    if (extension.MigrationsAssembly.HasValue())
                        sql.MigrationsAssembly(extension.MigrationsAssembly);

                    if (extension.MigrationsHistoryTableName.HasValue())
                        sql.MigrationsHistoryTable(extension.MigrationsHistoryTableName, extension.MigrationsHistoryTableSchema);
                }
            });
        }
    }
}

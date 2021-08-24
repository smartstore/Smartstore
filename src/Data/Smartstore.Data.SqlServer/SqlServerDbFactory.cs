using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Smartstore.Core.Data;
using Smartstore.Data.Providers;

// Add-Migration Initial -Context SqlServerSmartDbContext -Project Smartstore.Data.SqlServer

namespace Smartstore.Data.SqlServer
{
    // TODO: (core) Find a way to deploy provider projects unreferenced.
    
    internal class SqlServerDbFactory : DbFactory
    {
        public override DbSystemType DbSystem { get; } = DbSystemType.SqlServer;

        public override Type SmartDbContextType => typeof(SqlServerSmartDbContext);

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(string connectionString)
            => new SqlConnectionStringBuilder(connectionString);

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(
            string server, 
            string database,
            string userId,
            string password)
        {
            Guard.NotEmpty(server, nameof(server));

            var builder = new SqlConnectionStringBuilder 
            {
                IntegratedSecurity = userId.IsEmpty(),
                DataSource = server,
                InitialCatalog = database,
                UserInstance = false,
                Pooling = true,
                MinPoolSize = 1,
                MaxPoolSize = 100,
                Enlist = false
            };
            
            if (!builder.IntegratedSecurity)
            {
                builder.UserID = userId;
                builder.Password = password;
            }

            return builder;
        }

        public override DataProvider CreateDataProvider(DatabaseFacade database)
            => new SqlServerDataProvider(database);

        public override HookingDbContext CreateApplicationDbContext(string connectionString, int? commandTimeout = null, string migrationHistoryTableName = null)
        {
            Guard.NotEmpty(connectionString, nameof(connectionString));

            var optionsBuilder = new DbContextOptionsBuilder<SqlServerSmartDbContext>()
                .ReplaceService<IConventionSetBuilder, FixedRuntimeConventionSetBuilder>()
                .UseSqlServer(connectionString, sql =>
                {
                    sql.CommandTimeout(commandTimeout);
                    sql.MigrationsHistoryTable(migrationHistoryTableName);
                });

            return new SqlServerSmartDbContext(optionsBuilder.Options);
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString)
        {
            return builder.UseSqlServer(connectionString, sql =>
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

        public override async Task<int> CreateDatabaseAsync(
            string connectionString,
            string collation = null,
            int? commandTimeout = null,
            CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(connectionString, nameof(connectionString));

            var conString = (SqlConnectionStringBuilder)CreateConnectionStringBuilder(connectionString);
            var databaseName = conString.InitialCatalog;

            conString.InitialCatalog = "master";

            var createSql = collation.HasValue()
                ? $"CREATE DATABASE [{databaseName}] COLLATE {collation}"
                : $"CREATE DATABASE [{databaseName}]";

            using var connection = new SqlConnection(conString.ConnectionString);

            var command = connection.CreateCommand();
            command.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}') BEGIN {createSql} END";

            if (commandTimeout.HasValue)
            {
                command.CommandTimeout = commandTimeout.Value;
            }

            await command.Connection.OpenAsync(cancelToken);

            return await command.ExecuteNonQueryAsync(cancelToken);
        }
    }
}

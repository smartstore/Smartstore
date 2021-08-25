using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Smartstore.Data.Providers;

namespace Smartstore.Data.SqlServer
{
    // TODO: (core) Find a way to deploy provider projects unreferenced.
    
    internal class SqlServerDbFactory : DbFactory
    {
        public override DbSystemType DbSystem { get; } = DbSystemType.SqlServer;

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

        public override TContext CreateDbContext<TContext>(string connectionString, int? commandTimeout = null)
        {
            Guard.NotEmpty(connectionString, nameof(connectionString));

            var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                .ReplaceService<IConventionSetBuilder, FixedRuntimeConventionSetBuilder>()
                .UseSqlServer(connectionString, sql =>
                {
                    sql.CommandTimeout(commandTimeout);
                });

            return (TContext)Activator.CreateInstance(typeof(TContext), new object[] { optionsBuilder.Options });
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

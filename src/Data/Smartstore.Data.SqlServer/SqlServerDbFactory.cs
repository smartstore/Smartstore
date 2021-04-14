using System;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Smartstore.Data.Providers;
using Smartstore.Engine;

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
            Guard.NotEmpty(database, nameof(database));

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

        public override DbContextOptionsBuilder ConfigureDbContext(
            DbContextOptionsBuilder builder, 
            string connectionString, 
            IApplicationContext appContext)
        {
            return builder.UseSqlServer(connectionString, sql =>
            {
                //sql.EnableRetryOnFailure(3, TimeSpan.FromMilliseconds(100), null);
            });
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString)
        {
            return builder.UseSqlServer(connectionString, sql =>
            {
                var extension = builder.Options.FindExtension<DbFactoryOptionsExtension>();
                
                if (extension != null)
                {
                    sql.UseRelationalNulls(extension.UseRelationalNulls);

                    if (extension.CommandTimeout.HasValue)
                        sql.CommandTimeout(extension.CommandTimeout.Value);

                    if (extension.ExecutionStrategyFactory != null)
                        sql.ExecutionStrategy(extension.ExecutionStrategyFactory);

                    if (extension.MigrationsAssembly.HasValue())
                        sql.MigrationsAssembly(extension.MigrationsAssembly);

                    if (extension.MigrationsHistoryTableName.HasValue())
                        sql.MigrationsHistoryTable(extension.MigrationsHistoryTableName, extension.MigrationsHistoryTableSchema);

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

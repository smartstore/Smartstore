using System;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Data.Providers;
using Smartstore.Data.SqlServer.Translators;
using Smartstore.Engine;

namespace Smartstore.Data.SqlServer
{
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
            Guard.NotEmpty(server);

            var builder = new SqlConnectionStringBuilder
            {
                IntegratedSecurity = userId.IsEmpty(),
                DataSource = server,
                InitialCatalog = database,
                UserInstance = false,
                Pooling = true,
                MinPoolSize = 1,
                MaxPoolSize = 1024,
                Enlist = false,
                MultipleActiveResultSets = true,
                Encrypt = false
            };

            if (!builder.IntegratedSecurity)
            {
                builder.UserID = userId;
                builder.Password = password;
            }

            return builder;
        }

        public override bool TryNormalizeConnectionString(string connectionString, out string normalizedConnectionString)
        {
            normalizedConnectionString = null;
            bool normalized = false;

            if (connectionString.HasValue())
            {
                connectionString = connectionString.Trim();

                // Ensure that MARS is enabled
                if (!connectionString.ContainsNoCase("MultipleActiveResultSets="))
                {
                    connectionString = connectionString.EnsureEndsWith(';') + "MultipleActiveResultSets=True";
                    normalized = true;
                }

                // Ensure that Encrypt is false, othwerwise SqlClient will reject connection.
                if (!connectionString.ContainsNoCase("Encrypt="))
                {
                    connectionString = connectionString.EnsureEndsWith(';') + "Encrypt=False";
                    normalized = true;
                }
            }

            if (normalized)
            {
                normalizedConnectionString = connectionString;
            }

            return normalized;
        }

        public override DataProvider CreateDataProvider(DatabaseFacade database)
            => new SqlServerDataProvider(database);

        public override TContext CreateDbContext<TContext>(string connectionString, int? commandTimeout = null)
        {
            Guard.NotEmpty(connectionString);

            var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                .UseSqlServer(connectionString, sql =>
                {
                    sql.CommandTimeout(commandTimeout);

                    var compatLevel = EngineContext.Current?.Application?.AppConfiguration?.SqlServerCompatLevel;
                    if (compatLevel >= 100)
                    {
                        sql.UseCompatibilityLevel(compatLevel.Value);
                    }
                })
                .ReplaceService<IMethodCallTranslatorProvider, SqlServerMappingMethodCallTranslatorProvider>();
            
            return (TContext)Activator.CreateInstance(typeof(TContext), [optionsBuilder.Options]);
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString)
        {
            Guard.NotNull(builder);
            Guard.NotEmpty(connectionString);

            return builder.UseSqlServer(connectionString, sql =>
            {
                var compatLevel = EngineContext.Current?.Application?.AppConfiguration?.SqlServerCompatLevel;
                if (compatLevel >= 100)
                {
                    sql.UseCompatibilityLevel(compatLevel.Value);
                }

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
            })
            .ReplaceService<IMethodCallTranslatorProvider, SqlServerMappingMethodCallTranslatorProvider>();
        }

        protected override UnifiedModelBuilderFacade CreateModelBuilderFacade()
        {
            return new SqlServerModelBuilderFacade();
        }
    }
}

using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql;
using Smartstore.Data.PostgreSql.Translators;
using Smartstore.Data.Providers;

namespace Smartstore.Data.PostgreSql
{
    /*
     * Kill processes SQL:
     * SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE pid <> pg_backend_pid() AND datname = 'xxxxx'
     * 
     * List connections SQL:
     * SELECT * FROM pg_stat_activity WHERE datname = 'xxxxx'
     */

    internal class PostgreSqlDbFactory : DbFactory
    {
        static PostgreSqlDbFactory()
        {
            // See: https://github.com/npgsql/efcore.pg/issues/2000
            // See: https://stackoverflow.com/questions/69961449/net6-and-datetime-problem-cannot-write-datetime-with-kind-utc-to-postgresql-ty
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        }

        public override DbSystemType DbSystem { get; } = DbSystemType.PostgreSql;

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(string connectionString)
            => new NpgsqlConnectionStringBuilder(connectionString);

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(
            string server,
            string database,
            string userId,
            string password)
        {
            Guard.NotEmpty(server);
            
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = server,
                Database = database,
                Username = userId,
                Password = password,
                Pooling = true,
                MinPoolSize = 1,
                MaxPoolSize = 1024, 
                Multiplexing = false
            };

            return builder;
        }

        public override DataProvider CreateDataProvider(DatabaseFacade database)
            => new PostgreSqlDataProvider(database);

        public override TContext CreateDbContext<TContext>(string connectionString, int? commandTimeout = null)
        {
            Guard.NotEmpty(connectionString);

            var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                .UseNpgsql(connectionString, sql =>
                {
                    sql.CommandTimeout(commandTimeout);
                })
                .ReplaceService<IMethodCallTranslatorProvider, PostgreSqlMappingMethodCallTranslatorProvider>();

            return (TContext)Activator.CreateInstance(typeof(TContext), new object[] { optionsBuilder.Options });
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString)
        {   
            return builder.UseNpgsql(connectionString, sql =>
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
            })
            .ReplaceService<IMethodCallTranslatorProvider, PostgreSqlMappingMethodCallTranslatorProvider>();
        }

        public override void ConfigureModelConventions(ModelConfigurationBuilder configurationBuilder)
        {
            // Use case-insensitive collation in all string columns
            //configurationBuilder.Properties<string>().UseCollation("und-x-icu-ci");
            configurationBuilder.Properties<string>().HaveColumnType("citext");
        }
        
        public override void CreateModel(ModelBuilder modelBuilder)
        {
            // Create a non-deterministic, case-insensitive collation
            //modelBuilder.HasCollation("und-x-icu-ci", locale: "und", provider: "icu", deterministic: false);
            modelBuilder.HasPostgresExtension("citext");
        }

        protected override UnifiedModelBuilderFacade CreateModelBuilderFacade()
        {
            return new PostgreSqlModelBuilderFacade();
        }
    }
}
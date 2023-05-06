using System;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Data.Providers;
using Smartstore.Data.Sqlite.Translators;
using Smartstore.IO;

namespace Smartstore.Data.Sqlite
{
    internal class SqliteDbFactory : DbFactory
    {
        public override DbSystemType DbSystem { get; } = DbSystemType.SQLite;

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(string connectionString)
            => new SqliteConnectionStringBuilder(connectionString);

        public override DbConnectionStringBuilder CreateConnectionStringBuilder(
            string server,
            string database,
            string userId,
            string password)
        {
            Guard.NotEmpty(database);

            var dbRelativePath = PathUtility.Join("App_Data", "Tenants", DataSettings.Instance.TenantName, $"{database}.db");

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = dbRelativePath,
                Pooling = true,
                Cache = SqliteCacheMode.Shared,
            };

            return builder;
        }

        public override DataProvider CreateDataProvider(DatabaseFacade database)
            => new SqliteDataProvider(database);

        public override TContext CreateDbContext<TContext>(string connectionString, int? commandTimeout = null)
        {
            Guard.NotEmpty(connectionString);

            var connection = CreateConnection(connectionString);

            var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                .UseSqlite(connection, sql =>
                {
                    sql.CommandTimeout(commandTimeout);
                })
                .ReplaceService<IQueryTranslationPostprocessorFactory, SqliteNoCaseQueryTranslationPostprocessorFactory>()
                .ReplaceService<IMethodCallTranslatorProvider, SqliteMappingMethodCallTranslatorProvider>();

            return (TContext)Activator.CreateInstance(typeof(TContext), new object[] { optionsBuilder.Options });
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString)
        {
            var connection = CreateConnection(connectionString);

            return builder.UseSqlite(connection, sql =>
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
            .ReplaceService<IQueryTranslationPostprocessorFactory, SqliteNoCaseQueryTranslationPostprocessorFactory>()
            .ReplaceService<IMethodCallTranslatorProvider, SqliteMappingMethodCallTranslatorProvider>();
        }

        public override void ConfigureModelConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Properties<decimal>().HaveConversion<double>();
            configurationBuilder.Properties<decimal?>().HaveConversion<double?>();
            configurationBuilder.Properties<string>().UseCollation("NOCASE");
        }

        public override void CreateModel(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("NOCASE");
        }

        private static SqliteConnection CreateConnection(string connectionString)
        {
            var connection = new SqliteConnection(connectionString);

            connection.CreateCollation("NOCASE", SqliteNoCase);
            connection.CreateFunction<string, string>("lower", SqliteLower, isDeterministic: true);
            connection.CreateFunction<string, string>("upper", SqliteUpper, isDeterministic: true);
            connection.CreateFunction<string, string, int?>("instr", SqliteInstr, isDeterministic: true);

            return connection;
        }

        private static int SqliteNoCase(string x, string y)
            => string.Compare(x, y, ignoreCase: true);

        private static string SqliteLower(string x)
            => x?.ToLower();

        private static string SqliteUpper(string x)
            => x?.ToUpper();

        private static int? SqliteInstr(string left, string right)
        {
            if (left == null || right == null)
            {
                return null;
            }

            var index = left.IndexOf(right, StringComparison.CurrentCultureIgnoreCase);

            // SQLite instr is 1-based.
            return index + 1;
        }
    }
}

using System;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Sqlite.Storage.Internal;
using Smartstore.Data.Providers;
using Smartstore.Data.Sqlite.Translators;
using Smartstore.IO;

namespace Smartstore.Data.Sqlite
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
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

            var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                .UseSqlite(connectionString, sql =>
                {
                    sql.CommandTimeout(commandTimeout);
                });

            optionsBuilder = (DbContextOptionsBuilder<TContext>)ReplaceServices(optionsBuilder);

            return (TContext)Activator.CreateInstance(typeof(TContext), new object[] { optionsBuilder.Options });
        }

        public override DbContextOptionsBuilder ConfigureDbContext(DbContextOptionsBuilder builder, string connectionString)
        {
            var optionsBuilder = builder.UseSqlite(connectionString, sql =>
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

            return ReplaceServices(optionsBuilder);
        }

        private static DbContextOptionsBuilder ReplaceServices(DbContextOptionsBuilder builder)
        {
            return builder
                .ReplaceService<ISqliteRelationalConnection, SqliteSmartRelationalConnection>()
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
    }
}
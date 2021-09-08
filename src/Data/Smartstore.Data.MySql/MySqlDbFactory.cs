using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Pomelo.EntityFrameworkCore.MySql.Query.Internal;
using Smartstore.Data.Providers;

namespace Smartstore.Data.MySql
{
    internal class MySqlDbFactory : DbFactory
    {
        public override DbSystemType DbSystem { get; } = DbSystemType.MySql;

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

        public override TContext CreateDbContext<TContext>(string connectionString, int? commandTimeout = null)
        {
            Guard.NotEmpty(connectionString, nameof(connectionString));

            var optionsBuilder = new DbContextOptionsBuilder<TContext>()
                .ReplaceService<IConventionSetBuilder, FixedRuntimeConventionSetBuilder>()
                .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString), sql =>
                {
                    sql.CommandTimeout(commandTimeout);
                });

            return (TContext)Activator.CreateInstance(typeof(TContext), new object[] { optionsBuilder.Options });
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
                }
            });
        }

        #region Method Call Translation

        private static readonly FieldInfo _translatorsField = typeof(RelationalMethodCallTranslatorProvider)
            .GetField("_translators", BindingFlags.NonPublic | BindingFlags.Instance);

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "No public API for this available")]
        protected override IMethodCallTranslator FindMethodCallTranslator(IServiceProvider services, MethodInfo sourceMethod)
        {
            var provider = services.GetRequiredService<IMethodCallTranslatorProvider>() as MySqlMethodCallTranslatorProvider;
            if (provider != null)
            {
                var translators = _translatorsField.GetValue(provider) as List<IMethodCallTranslator>;
                if (translators != null)
                {
                    if (sourceMethod.Name.StartsWith("DateDiff"))
                    {
                        return translators.FirstOrDefault(x => x is MySqlDateDiffFunctionsTranslator);
                    }
                }
            }

            return null;
        }

        protected override MethodInfo FindMappedMethod(MethodInfo sourceMethod)
        {
            var parameterTypes = sourceMethod.GetParameters().Select(p => p.ParameterType).ToArray();

            var method = typeof(MySqlDbFunctionsExtensions).GetRuntimeMethod(
                sourceMethod.Name,
                parameterTypes);

            return method;
        }

        #endregion
    }
}

using System.Reflection;
using System.Transactions;
using Autofac;
using FluentMigrator;
using Microsoft.EntityFrameworkCore.Storage;
using Smartstore.Data;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public abstract class DbMigrator
    {
        private readonly ILifetimeScope _scope;

        protected DbMigrator(ILifetimeScope scope, IMigrationTable migrationTable)
        {
            _scope = scope;
            MigrationTable = migrationTable;
        }

        public abstract HookingDbContext Context { get; }

        public virtual IMigrationTable MigrationTable { get; }

        /// <summary>
        /// Migrates the database to the latest version.
        /// </summary>
        /// <param name="assembly">
        ///     Pass an <see cref="Assembly"/> instance to reduce the set of processed migrations to migration classes found in the given assembly only.
        /// </param>
        /// <returns>The number of applied migrations.</returns>
        public abstract Task<int> RunPendingMigrationsAsync(Assembly assembly = null, CancellationToken cancelToken = default);

        /// <summary>
        /// Migrates the database to <paramref name="targetVersion"/> or to the latest version if no version was specified.
        /// </summary>
        /// <param name="targetVersion">The target migration version. Pass -1 to perform a full rollback.</param>
        /// <param name="assembly">
        ///     Pass an <see cref="Assembly"/> instance to reduce the set of processed migrations to migration classes found in the given assembly only.
        /// </param>
        /// <returns>The number of applied migrations.</returns>
        public abstract Task<int> MigrateAsync(long? targetVersion = null, Assembly assembly = null, CancellationToken cancelToken = default);

        /// <summary>
        /// Creates an instance of the migration class.
        /// </summary>
        /// <param name="migrationClass">
        /// The <see cref="Type" /> for the migration class, as obtained from the <see cref="MigrationTable" /> dictionary.
        /// </param>
        /// <returns>The migration instance.</returns>
        protected IMigration CreateMigration(Type migrationClass)
        {
            Guard.NotNull(migrationClass);

            return (IMigration)_scope.ResolveUnregistered(migrationClass);
        }

        #region Database initialization

        /// <summary>
        /// Determines whether the database contains ALL tables specified by <see cref="CheckTablesAttribute"/>. 
        /// If the DbContext is not annotated with <see cref="CheckTablesAttribute"/> this method will return
        /// <c>true</c> if at least one user table is present in the database, otherwise <c>false</c>.
        /// </summary>
        /// <returns> A value indicating whether the required tables are present in the database. </returns>
        public bool HasTables()
        {
            var tablesToCheck = Context.GetType().GetAttribute<CheckTablesAttribute>(true)?.TableNames;

            if (tablesToCheck != null && tablesToCheck.Length > 0)
            {
                var dbTables = Context.DataProvider.GetTableNames();

                // True when ALL required tables are present in the database
                return dbTables.Intersect(tablesToCheck, StringComparer.InvariantCultureIgnoreCase).Count() == tablesToCheck.Length;
            }

            return (Context.Database.GetFacadeDependencies().DatabaseCreator as RelationalDatabaseCreator)?.HasTables() ?? false;
        }

        /// <summary>
        /// Creates the schema for the current model in the database. The database must exist physically or this method
        /// will raise an exception. To specify the table names that the database should contain in order to satisfy the model, annotate
        /// the DbContext class with <see cref="CheckTablesAttribute"/>. 
        /// If all given tables already exist in the database, this method will exit.
        /// After the schema was created, the migration version info table is populated with all found migrations
        /// for the current model.
        /// </summary>
        /// <returns>
        /// <see langword="true" /> if the schema was created, <see langword="false" /> if it already existed.
        /// </returns>
        public bool EnsureSchemaPopulated()
        {
            var creator = Context.Database.GetFacadeDependencies().DatabaseCreator as RelationalDatabaseCreator;
            if (creator != null)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!HasTables())
                    {
                        creator.CreateTables();
                        PostPopulateSchema();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc cref="EnsureSchemaPopulated" />
        public async Task<bool> EnsureSchemaPopulatedAsync(CancellationToken cancelToken = default)
        {
            var creator = Context.Database.GetFacadeDependencies().DatabaseCreator as RelationalDatabaseCreator;
            if (creator != null)
            {
                using (new TransactionScope(TransactionScopeOption.Suppress, TransactionScopeAsyncFlowOption.Enabled))
                {
                    if (!HasTables())
                    {
                        await creator.CreateTablesAsync(cancelToken);
                        PostPopulateSchema();
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Runs all pending data seeders that are <see cref="DataSeederStage.Late"/>.
        /// </summary>
        /// <remarks>
        /// This method is not thread-safe and should only be called in an app initializer.
        /// </remarks>
        public abstract Task RunLateSeedersAsync(CancellationToken cancelToken = default);

        /// <summary>
        /// Seeds locale resources of pending migrations.
        /// </summary>
        /// <param name="fromVersion">Specifies the version of a migration from which locale resources are to be seeded.</param>
        /// <returns>The number of seeded migrations.</returns>
        public async Task<int> SeedPendingLocaleResourcesAsync(long fromVersion, CancellationToken cancelToken = default)
        {
            Guard.NotNegative(fromVersion);

            if (Context is not SmartDbContext db)
            {
                return 0;
            }

            var localMigrations = MigrationTable.GetMigrations();
            if (!localMigrations.Any())
            {
                return 0;
            }

            var succeeded = 0;
            var providers = localMigrations
                .Where(x => x.Version > fromVersion)
                .Select(x => CreateMigration(x.Type) as ILocaleResourcesProvider)
                .Where(x => x != null)
                .ToArray();

            foreach (var provider in providers)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                var builder = new LocaleResourcesBuilder();
                provider.MigrateLocaleResources(builder);

                var resEntries = builder.Build();
                var resMigrator = new LocaleResourcesMigrator(db);
                await resMigrator.MigrateAsync(resEntries);
                succeeded++;
            }

            return succeeded;
        }

        private void PostPopulateSchema()
        {
            var appliedMigrations = MigrationTable.GetAppliedMigrations().ToArray();

            foreach (var migration in MigrationTable.GetMigrations())
            {
                if (!appliedMigrations.Contains(migration.Version))
                {
                    MigrationTable.UpdateVersionInfo(migration.Version, migration.Description ?? migration.Type.Name);
                }
            }

            MigrationTable.Reload();
        }

        #endregion
    }
}

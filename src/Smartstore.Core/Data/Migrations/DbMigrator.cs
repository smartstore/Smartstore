using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Autofac;
using FluentMigrator;
using FluentMigrator.Runner;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Smartstore.Data;
using Smartstore.Data.Migrations;
using Smartstore.Engine;

namespace Smartstore.Core.Data.Migrations
{
    public abstract class DbMigrator
    {
        private Assembly _assembly;
        private IReadOnlyDictionary<long, MigrationDescriptor> _migrations;

        private readonly ILifetimeScope _scope;
        private readonly ITypeScanner _typeScanner;
        private readonly IVersionLoader _versionLoader;

        protected DbMigrator(ILifetimeScope scope, ITypeScanner typeScanner, IVersionLoader versionLoader)
        {
            _scope = scope;
            _typeScanner = typeScanner;
            _versionLoader = versionLoader;
        }

        public abstract HookingDbContext Context { get; }

        /// <summary>
        /// Migrates the database to the latest version.
        /// </summary>
        /// <returns>The number of applied migrations.</returns>
        public abstract Task<int> RunPendingMigrationsAsync(CancellationToken cancelToken = default);

        /// <summary>
        /// Migrates the database to <paramref name="targetVersion"/> or to the latest version if no version was specified.
        /// </summary>
        /// <param name="targetVersion">The target migration version.</param>
        /// <returns>The number of applied migrations.</returns>
        public abstract Task<int> MigrateAsync(long? targetVersion = null, CancellationToken cancelToken = default);

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
        /// Seeds locale resources of pending migrations.
        /// </summary>
        /// <param name="currentHead">
        /// Specifies a pending migration (class name) from which locale resources are to be seeded.
        /// <c>null</c> to seed locale resources of all pending migrations.
        /// </param>
        /// <returns>The number of seeded migrations.</returns>
        public async Task<int> SeedPendingLocaleResourcesAsync(string currentHead = null, CancellationToken cancelToken = default)
        {
            // TODO: (mg) (core) currentHead must be long type now.
            if (Context is not SmartDbContext db)
            {
                return 0;
            }

            var localMigrations = GetMigrations();
            if (localMigrations.Count == 0)
            {
                return 0;
            }

            var succeeded = 0;
            var pending = GetPendingMigrations().Select(v => localMigrations[v]);

            if (currentHead.HasValue())
            {
                var headMigration = pending.FirstOrDefault(x => x.Name.EqualsNoCase(currentHead));
                if (headMigration != null)
                {
                    pending = pending.Where(x => x.Version > headMigration.Version);
                }
            }

            var providers = pending
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
                ++succeeded;
            }

            return succeeded;
        }

        private void PostPopulateSchema()
        {
            var appliedMigrations = GetAppliedMigrations().ToArray();
            
            foreach (var migration in GetMigrations().Values)
            {
                if (!appliedMigrations.Contains(migration.Version))
                {
                    _versionLoader.UpdateVersionInfo(migration.Version, migration.Description ?? migration.Type.Name);
                } 
            }

            _versionLoader.LoadVersionInfo();
        }

        #endregion

        #region Migration history

        /// <summary>
        ///  The assembly that contains the migrations, usually the assembly containing the DbContext.
        /// </summary>
        public Assembly MigrationAssembly
        {
            get 
            {
                Assembly Resolve()
                {
                    var assemblyName = RelationalOptionsExtension.Extract(Context.Options)?.MigrationsAssembly;
                    return assemblyName == null
                        ? Context.GetType().Assembly
                        : Assembly.Load(new AssemblyName(assemblyName));
                }
                
                return _assembly ??= Resolve();
            }
        }

        /// <summary>
        /// Gets all the migrations that are defined in the configured migrations assembly.
        /// </summary>
        public IReadOnlyDictionary<long, MigrationDescriptor> GetMigrations()
        {
            IReadOnlyDictionary<long, MigrationDescriptor> Create()
            {
                var result = new SortedList<long, MigrationDescriptor>();

                var items
                    = from t in _typeScanner.FindTypes<IMigration>(new[] { MigrationAssembly })
                      let descriptor = new MigrationDescriptor(t)
                      where descriptor.Version > 0
                      orderby descriptor.Version
                      select descriptor;

                foreach (var descriptor in items)
                {
                    result.Add(descriptor.Version, descriptor);
                }

                return result;
            }

            return _migrations ??= Create();
        }

        /// <summary>
        /// Gets all migrations that have been applied to the target database.
        /// </summary>
        public IEnumerable<long> GetAppliedMigrations()
        {
            return GetMigrations().Select(x => x.Key).Intersect(_versionLoader.VersionInfo.AppliedMigrations());
        }

        /// <summary>
        /// Gets all migrations that are defined in the assembly but haven't been applied to the target database.
        /// </summary>
        public IEnumerable<long> GetPendingMigrations()
        {
            return GetMigrations().Select(x => x.Key).Except(GetAppliedMigrations());
        }

        /// <summary>
        /// Creates an instance of the migration class.
        /// </summary>
        /// <param name="migrationClass">
        /// The <see cref="Type" /> for the migration class, as obtained from the <see cref="GetMigrations()" /> dictionary.
        /// </param>
        /// <returns>The migration instance.</returns>
        protected IMigration CreateMigration(Type migrationClass)
        {
            Guard.NotNull(migrationClass, nameof(migrationClass));

            return (IMigration)_scope.ResolveUnregistered(migrationClass);
        }

        #endregion
    }
}

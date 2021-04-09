using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Data;
using Smartstore.Events;

namespace Smartstore.Core.Data.Migrations
{
    public class DbMigrator<TContext> where TContext : DbContext
    {
        private static readonly Regex _migrationIdPattern = new(@"\d{14}_.+");
        const string _migrationTypeFormat = "{0}.{1}, {2}";
        const string _automaticMigration = "AutomaticMigration";

        private readonly TContext _db;
        private readonly IEventPublisher _eventPublisher;

        private Exception _lastSeedException;

        public DbMigrator(TContext db, IEventPublisher eventPublisher)
        {
            Guard.NotNull(db, nameof(db));
            Guard.NotNull(eventPublisher, nameof(eventPublisher));

            _db = db;
            _eventPublisher = eventPublisher;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        /// <summary>
        /// Migrates the database to the latest version
        /// </summary>
        /// <returns>The number of applied migrations</returns>
        public async Task<int> RunPendingMigrationsAsync()
        {
            if (_lastSeedException != null)
            {
                // This can happen when a previous migration attempt failed with a rollback.
                //return 0;
                throw _lastSeedException;
            }

            var pendingMigrations = (await _db.Database.GetPendingMigrationsAsync()).ToList();
            if (!pendingMigrations.Any())
                return 0;

            var coreSeeders = new List<SeederEntry>();
            var externalSeeders = new List<SeederEntry>();
            var isCoreMigration = _db is SmartDbContext;
            var appliedMigrations = (await _db.Database.GetAppliedMigrationsAsync()).ToArray();
            var initialMigration = appliedMigrations.LastOrDefault() ?? "[Initial]";
            var lastSuccessfulMigration = appliedMigrations.FirstOrDefault();
            int result = 0;

            // Apply migrations
            foreach (var migrationId in pendingMigrations)
            {
                if (IsAutomaticMigration(migrationId))
                    continue;

                if (!IsValidMigrationId(migrationId))
                    continue;

                // Resolve and instantiate the DbMigration instance from the assembly
                var migration = CreateMigrationInstanceByMigrationId(migrationId, _db);

                // Seeders for the core DbContext must be run in any case 
                // (e.g. for Resource or Setting updates even from external plugins)
                var coreSeeder = migration as IDataSeeder<SmartDbContext>;
                IDataSeeder<TContext> externalSeeder = null;

                if (!isCoreMigration)
                {
                    // Context specific seeders should only be resolved
                    // when origin is external (e.g. a module)
                    externalSeeder = migration as IDataSeeder<TContext>;
                }

                try
                {
                    // Call the actual Migrate() to execute this migration
                    await _db.Database.MigrateAsync(migrationId);
                    result++;
                }
                catch (Exception ex)
                {
                    result = 0;
                    throw new DbMigrationException(lastSuccessfulMigration, migrationId, ex.InnerException ?? ex, false);
                }

                var migrationName = GetMigrationClassName(migrationId);

                if (coreSeeder != null)
                    coreSeeders.Add(new SeederEntry
                    {
                        DataSeeder = coreSeeder,
                        MigrationId = migrationId,
                        MigrationName = migrationName,
                        PreviousMigrationId = lastSuccessfulMigration,
                    });

                if (externalSeeder != null)
                    externalSeeders.Add(new SeederEntry
                    {
                        DataSeeder = externalSeeder,
                        MigrationId = migrationId,
                        MigrationName = migrationName,
                        PreviousMigrationId = lastSuccessfulMigration,
                    });

                lastSuccessfulMigration = migrationId;
                //DbMigrationContext.Current.AddAppliedMigration(typeof(TContext), migrationId); // TODO: (core) ???
            }

            if (coreSeeders.Any())
            {
                // Apply core data seeders first
                var coreContext = isCoreMigration ? _db as SmartDbContext : new SmartDbContext(null); // TODO: (core) Pass options
                await RunSeedersAsync<SmartDbContext>(coreSeeders, coreContext);
            }

            // Apply external data seeders
            await RunSeedersAsync<TContext>(externalSeeders, _db);

            Logger.Info("Database migration successful: {0} >> {1}".FormatInvariant(initialMigration, lastSuccessfulMigration));

            return result;
        }

        private async Task RunSeedersAsync<T>(IEnumerable<SeederEntry> seederEntries, T ctx) where T : DbContext
        {
            foreach (var seederEntry in seederEntries)
            {
                var seeder = (IDataSeeder<T>)seederEntry.DataSeeder;

                try
                {
                    // Pre seed event
                    await _eventPublisher.PublishAsync(new SeedingDbMigrationEvent { MigrationName = seederEntry.MigrationName, DbContext = ctx });

                    // Seed
                    await seeder.SeedAsync(ctx);

                    // Post seed event
                    await _eventPublisher.PublishAsync(new SeededDbMigrationEvent { MigrationName = seederEntry.MigrationName, DbContext = ctx });
                }
                catch (Exception ex)
                {
                    if (seeder.RollbackOnFailure)
                    {
                        _lastSeedException = new DbMigrationException(seederEntry.PreviousMigrationId, seederEntry.MigrationId, ex.InnerException ?? ex, true);

                        try
                        {
                            await _db.Database.MigrateAsync(seederEntry.PreviousMigrationId);
                        }
                        catch 
                        { 
                        }

                        throw _lastSeedException;
                    }

                    Logger.Warn(ex, "Seed error in migration '{0}'. The error was ignored because no rollback was requested.", seederEntry.MigrationId);
                }
            }
        }

        private void LogError(string initialMigration, string targetMigration, Exception exception)
        {
            Logger.Error(exception, "Database migration error: {0} >> {1}", initialMigration, targetMigration);
        }

        private class SeederEntry
        {
            public string PreviousMigrationId { get; set; }
            public string MigrationId { get; set; }
            public string MigrationName { get; set; }
            public object DataSeeder { get; set; }
        }

        #region Utils

        // TODO: (core) Replace MigratorUtils with IMigrationsAssembly.

        private static DbContextOptions GetOptions(DbContext context)
        {
            Guard.NotNull(context, nameof(context));
            return (context as IInfrastructure<IServiceProvider>).Instance.GetService<DbContextOptions>();
        }

        /// <summary>
        /// Creates a full type instance for the migration id by using the current migrations namespace
        /// ie: Smartstore.Data.SqlServer.Migrations.20201109094533_Initial
        /// </summary>
        /// <param name="migrationId">The migration id from the migrations list of the migrator</param>
        /// <returns>The full DbMigration instance</returns>
        private static Migration CreateMigrationInstanceByMigrationId(string migrationId, DbContext context)
        {
            var options = GetOptions(context);
            var relationalOptions = options.FindExtension<RelationalOptionsExtension>();

            if (relationalOptions == null)
            {
                // TODO: (core) Throw exception proper message.
                throw new Exception("Null");
            }

            var assemblyName = 
                relationalOptions.MigrationsAssembly.NullEmpty() ?? 
                context.GetType().Assembly.FullName;
            
            string migrationTypeName =
                string.Format(_migrationTypeFormat,
                              "", // TODO: (core) Resolve migration namespace
                              GetMigrationClassName(migrationId),
                              assemblyName);

            return CreateTypeInstance<Migration>(migrationTypeName);
        }

        /// <summary>
        /// Checks if the migration id is valid
        /// </summary>
        /// <param name="migrationId">The migration id from the migrations list of the migrator</param>
        /// <returns>true if valid, otherwise false</returns>
        private static bool IsValidMigrationId(string migrationId)
        {
            if (string.IsNullOrWhiteSpace(migrationId))
                return false;

            return _migrationIdPattern.IsMatch(migrationId) || migrationId == Migration.InitialDatabase;
        }

        /// <summary>
        /// Checks if the the migration id belongs to an automatic migration
        /// </summary>
        /// <param name="migrationId">The migration id from the migrations list of the migrator</param>
        /// <returns>true if automatic, otherwise false</returns>
        private static bool IsAutomaticMigration(string migrationId)
        {
            if (string.IsNullOrWhiteSpace(migrationId))
                return false;

            return migrationId.EndsWith(_automaticMigration, StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets the ClassName from a migration id
        /// </summary>
        /// <param name="migrationId">The migration id from the migrations list of the migrator</param>
        /// <returns>The class name for this migration id</returns>
        private static string GetMigrationClassName(string migrationId)
        {
            if (string.IsNullOrWhiteSpace(migrationId))
                return string.Empty;

            return migrationId[15..];
        }

        /// <summary>
        /// Creates a new instance of a typename
        /// </summary>
        /// <typeparam name="TType">The type of the return instance</typeparam>
        /// <param name="typeName">The full name (including assembly and namespaces) of the type to create</param>
        /// <returns>
        /// A new instance of the type if it is (or boxable to) <typeparamref name="TType"/>, 
        /// otherwise the default of <typeparamref name="TType"/>
        /// </returns>
        private static TType CreateTypeInstance<TType>(string typeName) where TType : class
        {
            Type classType = Type.GetType(typeName, false);

            if (classType == null)
                return default;

            object instance = Activator.CreateInstance(classType);
            return instance as TType;
        }

        #endregion
    }
}

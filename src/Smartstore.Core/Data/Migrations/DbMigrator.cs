using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Data;
using Smartstore.Data.Migrations;
using Smartstore.Events;

namespace Smartstore.Core.Data.Migrations
{
    public abstract class DbMigrator
    {
        /// <summary>
        /// The default name for the Migrations history table.
        /// </summary>
        public const string DefaultHistoryTableName = "__EFMigrationsHistory";

        public abstract HookingDbContext Context { get; }

        /// <summary>
        /// Migrates the database to the latest version
        /// </summary>
        /// <returns>The number of applied migrations</returns>
        public abstract Task<int> RunPendingMigrationsAsync(CancellationToken cancelToken = default);

        /// <summary>
        /// Seeds locale resources which are ahead of given <paramref name="currentHead"/> migration.
        /// </summary>
        public abstract Task SeedPendingLocaleResources(string currentHead, CancellationToken cancelToken = default);
    }
    
    public class DbMigrator<TContext> : DbMigrator where TContext : HookingDbContext
    {
        private static readonly Regex _migrationIdPattern = new Regex(@"\d{15}_.+");

        private readonly TContext _db;
        private readonly SmartDbContext _dbCore;
        private readonly IEventPublisher _eventPublisher;

        private Exception _lastSeedException;

        public DbMigrator(TContext db, SmartDbContext dbCore, IEventPublisher eventPublisher)
        {
            _db = Guard.NotNull(db, nameof(db));
            _dbCore = Guard.NotNull(dbCore, nameof(dbCore));
            _eventPublisher = Guard.NotNull(eventPublisher, nameof(eventPublisher));
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public override TContext Context => _db;

        private async Task<bool> ShouldSuppressInitialCreate()
        {
            var shouldSuppress = false;
            var tablesToCheck = _db.GetInvariantType().GetAttribute<CheckTablesAttribute>(true)?.TableNames;
            if (tablesToCheck != null && tablesToCheck.Length > 0)
            {
                var dbTables = await _db.DataProvider.GetTableNamesAsync();
                shouldSuppress = dbTables.Intersect(tablesToCheck, StringComparer.InvariantCultureIgnoreCase).Count() == tablesToCheck.Length;
            }

            return shouldSuppress;
        }

        public override async Task<int> RunPendingMigrationsAsync(CancellationToken cancelToken = default)
        {
            if (_lastSeedException != null)
            {
                // This can happen when a previous migration attempt failed with a rollback.
                //return 0;
                throw _lastSeedException;
            }

            var pendingMigrations = (await _db.Database.GetPendingMigrationsAsync(cancelToken)).ToList();
            if (!pendingMigrations.Any())
                return 0;

            var migrationsAssembly = _db.Database.GetMigrationsAssembly();
            var coreSeeders = new List<SeederEntry>();
            var externalSeeders = new List<SeederEntry>();
            var isCoreMigration = _db is SmartDbContext;
            var appliedMigrations = (await _db.Database.GetAppliedMigrationsAsync(cancelToken)).ToArray();
            var initialMigration = appliedMigrations.LastOrDefault() ?? "[Initial]";
            var lastSuccessfulMigration = appliedMigrations.FirstOrDefault();
            int result = 0;

            if (appliedMigrations.Length == 0 && await ShouldSuppressInitialCreate())
            {
                //DbMigrationManager.Instance.SetSuppressInitialCreate<TContext>(true);
            }

            // Apply migrations
            foreach (var migrationId in pendingMigrations)
            {
                if (cancelToken.IsCancellationRequested)
                    break;
                
                // Resolve and instantiate the Migration instance from the assembly
                var migrationType = migrationsAssembly.Migrations[migrationId];
                var migration = migrationsAssembly.CreateMigration(migrationType, _db.Database.ProviderName);

                // Seeders for the core DbContext must be run in any case 
                // (e.g. for Resource or Setting updates even from external modules)
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
                    await _db.Database.MigrateAsync(migrationId, cancelToken);
                    result++;

                    if (cancelToken.IsCancellationRequested)
                        break;
                }
                catch (Exception ex)
                {
                    result = 0;
                    throw new DbMigrationException(lastSuccessfulMigration, migrationId, ex.InnerException ?? ex, false);
                }

                var migrationName = migrationType.Name;

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
                //DbMigrationManager.Instance.AddAppliedMigration(typeof(TContext), migrationId);
            }

            cancelToken.ThrowIfCancellationRequested();

            if (coreSeeders.Any())
            {
                // Apply core data seeders first
                await RunSeedersAsync(coreSeeders, _dbCore);
            }

            // Apply external data seeders
            await RunSeedersAsync(externalSeeders, _db);

            Logger.Info("Database migration successful: {0} >> {1}".FormatInvariant(initialMigration, lastSuccessfulMigration));

            return result;
        }

        private async Task RunSeedersAsync<T>(IEnumerable<SeederEntry> seederEntries, T ctx, CancellationToken cancelToken = default) where T : HookingDbContext
        {
            foreach (var seederEntry in seederEntries)
            {
                if (cancelToken.IsCancellationRequested)
                    break;
                
                var seeder = (IDataSeeder<T>)seederEntry.DataSeeder;

                try
                {
                    // Pre seed event
                    await _eventPublisher.PublishAsync(new SeedingDbMigrationEvent { MigrationName = seederEntry.MigrationName, DbContext = ctx });

                    // Seed
                    await seeder.SeedAsync(ctx, cancelToken);

                    // Post seed event
                    await _eventPublisher.PublishAsync(new SeededDbMigrationEvent { MigrationName = seederEntry.MigrationName, DbContext = ctx });
                }
                catch (Exception ex)
                {
                    if (seeder.RollbackOnFailure)
                    {
                        _lastSeedException = new DbMigrationException(seederEntry.PreviousMigrationId, seederEntry.MigrationId, ex.InnerException ?? ex, true);

                        if (!cancelToken.IsCancellationRequested)
                        {
                            try
                            {
                                await _db.Database.MigrateAsync(seederEntry.PreviousMigrationId, cancelToken);
                            }
                            catch
                            {
                            }
                        }

                        throw _lastSeedException;
                    }

                    Logger.Warn(ex, "Seed error in migration '{0}'. The error was ignored because no rollback was requested.", seederEntry.MigrationId);
                }
            }
        }

        public override async Task SeedPendingLocaleResources(string currentHead, CancellationToken cancelToken = default)
        {
            Guard.NotEmpty(currentHead, nameof(currentHead));

            var db = _db as SmartDbContext;
            if (db == null)
            {
                return;
            }

            var migrations = GetPendingResourceMigrations(currentHead).ToArray();

            if (migrations.Any())
            {
                var migrationsAssembly = db.Database.GetMigrationsAssembly();

                foreach (var id in migrations)
                {
                    if (cancelToken.IsCancellationRequested)
                        break;
                    
                    // Resolve and instantiate the Migration instance from the assembly
                    var migrationType = migrationsAssembly.Migrations[id];
                    var migration = migrationsAssembly.CreateMigration(migrationType, db.Database.ProviderName);

                    var provider = migration as ILocaleResourcesProvider;
                    if (provider == null)
                        continue;

                    var builder = new LocaleResourcesBuilder();
                    provider.MigrateLocaleResources(builder);

                    var resEntries = builder.Build();
                    var resMigrator = new LocaleResourcesMigrator(db);
                    await resMigrator.MigrateAsync(resEntries);
                }
            }
        }

        private IEnumerable<string> GetPendingResourceMigrations(string currentHead)
        {
            var localMigrations = _db.Database.GetMigrations();
            var atHead = false;

            foreach (var id in localMigrations)
            {
                var name = id[15..]; // First part is {Timestamp:14}_
                
                if (!atHead)
                {
                    if (!name.EqualsNoCase(currentHead))
                    {
                        continue;
                    }
                    else
                    {
                        atHead = true;
                        continue;
                    }
                }

                yield return id;
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
    }
}

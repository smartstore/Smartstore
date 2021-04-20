using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        public abstract HookingDbContext Context { get; }

        /// <summary>
        /// Migrates the database to the latest version
        /// </summary>
        /// <returns>The number of applied migrations</returns>
        public abstract Task<int> RunPendingMigrationsAsync();

        /// <summary>
        /// Seeds locale resources which are ahead of given <paramref name="currentHead"/> migration.
        /// </summary>
        public abstract Task SeedPendingLocaleResources(string currentHead);
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

        public override async Task<int> RunPendingMigrationsAsync()
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

            // Never run initial migration except during installation.
            DbMigrationManager.Instance.SetSuppressInitialCreate<TContext>(true);

            var migrationsAssembly = _db.Database.GetMigrationsAssembly();
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
                    await _db.Database.MigrateAsync(migrationId);
                    result++;
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
                DbMigrationManager.Instance.AddAppliedMigration(typeof(TContext), migrationId);
            }

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

        private async Task RunSeedersAsync<T>(IEnumerable<SeederEntry> seederEntries, T ctx) where T : HookingDbContext
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

        public override async Task SeedPendingLocaleResources(string currentHead)
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

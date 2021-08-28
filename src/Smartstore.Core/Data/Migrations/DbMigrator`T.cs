using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentMigrator;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Data;
using Smartstore.Data.Migrations;
using Smartstore.Engine;
using Smartstore.Events;

namespace Smartstore.Core.Data.Migrations
{
    public class DbMigrator2<TContext> : DbMigrator2 where TContext : HookingDbContext
    {
        private readonly TContext _db;
        private readonly IMigrationRunnerConventions _migrationRunnerConventions;
        private readonly IMigrationRunner _migrationRunner;
        private readonly IVersionLoader _versionLoader;
        private readonly IEventPublisher _eventPublisher;

        private Exception _lastSeedException;
        private MigrationDescriptor _initialMigration;
        private IMigrationInfo _lastSuccessfulMigration;

        public DbMigrator2(
            TContext db,
            IApplicationContext appContext,
            ILifetimeScope scope,
            IMigrationRunnerConventions migrationRunnerConventions,
            IMigrationRunner migrationRunner,
            IVersionLoader versionLoader,
            IEventPublisher eventPublisher)
            : base(scope, appContext.TypeScanner, versionLoader)
        {
            _db = db;
            _migrationRunnerConventions = migrationRunnerConventions;
            _migrationRunner = migrationRunner;
            _versionLoader = versionLoader;
            _eventPublisher = eventPublisher;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        /// <inheritdoc/>
        public override TContext Context => _db;

        /// <inheritdoc/>
        public override Task<int> RunPendingMigrationsAsync(CancellationToken cancelToken = default)
        {
            return MigrateAsync(null, cancelToken);
        }

        /// <inheritdoc/>
        public override async Task<int> MigrateAsync(long? targetVersion = null, CancellationToken cancelToken = default)
        {
            // INFO: (mg) (core) My fault. It absolutely makes sense for modules to migrate during installation to be able to strip off everything when uninstalling.

            if (_lastSeedException != null)
            {
                // This can happen when a previous migration attempt failed with a rollback.
                throw _lastSeedException;
            }

            var localMigrations = GetMigrations();
            if (localMigrations.Count == 0)
            {
                return 0;
            }

            if (targetVersion > 0 && !localMigrations.ContainsKey(targetVersion.Value))
            {
                throw new DbMigrationException($"{_db.GetType().Name} does not contain a database migration with version {targetVersion.Value}.");
            }

            var appliedMigrations = GetAppliedMigrations().ToArray();
            var lastAppliedVersion = appliedMigrations.LastOrDefault();
            var versions = Enumerable.Empty<long>();
            var down = false;
            var result = 0;

            _initialMigration = localMigrations.GetValueOrDefault(lastAppliedVersion);
            // INFO: the initialization of lastSuccessfulMigration in Classic looks wrong to me. Should be the last of all applied migrations, rather than the very first one.
            // RE: (mg) (core) See rollback TODO further below in MigrateUpAsync()
            _lastSuccessfulMigration = null;

            if (targetVersion == null)
            {
                // null = run pending migrations up to last (inclusive).
                versions = GetPendingMigrations();
            }
            else if (targetVersion == -1)
            {
                // -1 = rollback all applied migrations.
                versions = appliedMigrations.Reverse();
                down = true;
            }
            else if (targetVersion < lastAppliedVersion)
            {
                // Rollback to given version (exclusive).
                versions = appliedMigrations.Where(x => x > targetVersion).Reverse();
                down = true;
            }
            else if (targetVersion > lastAppliedVersion)
            {
                // Migrate up to given version (inclusive).
                versions = localMigrations.Select(x => x.Key).Where(x => x > lastAppliedVersion && x <= targetVersion);
            }

            if (!versions.Any())
            {
                // Nothing to do.
                return 0;
            }

            var migrations = 
                from v in versions
                let descriptor = localMigrations[v]
                let instance = CreateMigration(descriptor.Type)
                select _migrationRunnerConventions.GetMigrationInfoForMigration(instance);

            if (!down)
            {
                result = await MigrateUpAsync(migrations.ToArray(), cancelToken);
            }
            else
            {
                result = MigrateDown(migrations.ToArray(), cancelToken);
            }

            _versionLoader.LoadVersionInfo();

            if (_lastSuccessfulMigration != null)
            {
                Logger.Info($"Database migration successful: {_initialMigration?.Description ?? "Initial"} >> {_lastSuccessfulMigration.Description}");
            }

            return result;
        }
        
        protected virtual async Task<int> MigrateUpAsync(IMigrationInfo[] migrations, CancellationToken cancelToken)
        {
            // TBD: (mg) (core) Why was scoping removed? Do I miss something?

            var succeeded = 0;
            var seederEntries = new List<SeederEntry>();
            var runner = (MigrationRunner)_migrationRunner;

            // Migrate up.
            foreach (var migration in migrations)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }
                
                try
                {
                    runner.ApplyMigrationUp(migration, migration.TransactionBehavior == TransactionBehavior.Default);
                    succeeded++;
                }
                catch (Exception ex)
                {
                    // TODO: (mg) (core) Need to refactor every part that takes migration ids as strings. It is a "long" version now.
                    throw new DbMigrationException(
                        _lastSuccessfulMigration?.Description ?? _initialMigration?.Description ?? "Initial",
                        migration.Description, 
                        ex.InnerException ?? ex, 
                        false);
                }

                if (migration.Migration is IDataSeeder<TContext> seeder)
                {
                    seederEntries.Add(new SeederEntry
                    {
                        Seeder = seeder,
                        Migration = migration,
                        PreviousMigration = _lastSuccessfulMigration
                    });
                }

                _lastSuccessfulMigration = migration;
            }

            cancelToken.ThrowIfCancellationRequested();

            // TODO: (mg) (core) Where are the core/global seeders gone?
            // TODO: (mg) (core) Granularity: make isolated method for seeding again.
            // Execute data seeders.
            foreach (var entry in seederEntries)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                var m = entry.Migration;

                try
                {
                    // Pre seed event.
                    await _eventPublisher.PublishAsync(new SeedingDbMigrationEvent
                    {
                        Version = m.Version,
                        Description = m.Description,
                        DbContext = _db
                    }, cancelToken);

                    // Seed.
                    await entry.Seeder.SeedAsync(_db, cancelToken);

                    // Post seed event.
                    await _eventPublisher.PublishAsync(new SeededDbMigrationEvent
                    {
                        Version = m.Version,
                        Description = m.Description,
                        DbContext = _db
                    }, cancelToken);
                }
                catch (Exception ex)
                {
                    if (entry.Seeder.RollbackOnFailure)
                    {
                        _lastSeedException = new DbMigrationException(
                            entry.PreviousMigration?.Description ?? _initialMigration?.Description ?? "Initial", 
                            m.Description, 
                            ex.InnerException ?? ex, 
                            true);

                        if (!cancelToken.IsCancellationRequested)
                        {
                            try
                            {
                                if (entry.PreviousMigration == null && _initialMigration != null)
                                {
                                    // No migration executed in this iteration -> get the last of all applied migrations (if any).
                                    entry.PreviousMigration = _migrationRunnerConventions.GetMigrationInfoForMigration(CreateMigration(_initialMigration.Type));
                                }

                                if (entry.PreviousMigration != null)
                                {
                                    // TODO: (mg) (core) This is fundamentally wrong. We need to rollback the complete migration session,
                                    // down to the last known applied one, which belongs to a previous session.
                                    runner.ApplyMigrationDown(entry.PreviousMigration, entry.PreviousMigration.TransactionBehavior == TransactionBehavior.Default);
                                }
                            }
                            catch (Exception ex2)
                            {
                                Logger.Error(ex2);
                            }
                        }

                        throw _lastSeedException;
                    }

                    Logger.Warn(ex, "Seed error in migration '{0}'. The error was ignored because no rollback was requested.", m.Description);
                }
            }

            return succeeded;
        }

        protected virtual int MigrateDown(IMigrationInfo[] migrations, CancellationToken cancelToken)
        {
            var succeeded = 0;
            var runner = (MigrationRunner)_migrationRunner;

            foreach (var migration in migrations)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    runner.ApplyMigrationDown(migration, migration.TransactionBehavior == TransactionBehavior.Default);
                    ++succeeded;
                }
                catch (Exception ex)
                {
                    throw new DbMigrationException(
                        _lastSuccessfulMigration?.Description ?? _initialMigration?.Description ?? "Initial", 
                        migration.Description, 
                        ex.InnerException ?? ex, 
                        false);
                }

                _lastSuccessfulMigration = migration;
            }

            return succeeded;
        }

        protected class MigrationResult
        {
            public int Succeeded { get; set; }
            public List<SeederEntry> SeederEntries { get; set; } = new();
        }

        protected class SeederEntry
        {
            public IDataSeeder<TContext> Seeder { get; set; }
            public IMigrationInfo Migration { get; set; }
            public IMigrationInfo PreviousMigration { get; set; }
        }
    }
}

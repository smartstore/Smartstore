using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentMigrator;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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
        private readonly RunnerOptions _runnerOptions;
        private readonly IEventPublisher _eventPublisher;

        private Exception _lastSeedException;
        private string _initialMigration;
        private string _lastSuccessfulMigration;

        public DbMigrator2(
            TContext db,
            IApplicationContext appContext,
            ILifetimeScope scope,
            IMigrationRunnerConventions migrationRunnerConventions,
            IMigrationRunner migrationRunner,
            IVersionLoader versionLoader,
            IOptions<RunnerOptions> runnerOptions,
            IEventPublisher eventPublisher)
            : base(scope, appContext.TypeScanner, versionLoader)
        {
            _db = db;
            _migrationRunnerConventions = migrationRunnerConventions;
            _migrationRunner = migrationRunner;
            _versionLoader = versionLoader;
            _runnerOptions = runnerOptions.Value;
            _eventPublisher = eventPublisher;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        /// <inheritdoc/>
        public override TContext Context => _db;

        /// <inheritdoc/>
        public override async Task<int> MigrateAsync(long? targetVersion = null, CancellationToken cancelToken = default)
        {
            // TODO: (mg) (core) Throw when this method is called during installation. Migrations MUST NOT run during installation.

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

            _initialMigration = localMigrations.GetValueOrDefault(lastAppliedVersion)?.Description ?? "[Initial]";
            _lastSuccessfulMigration = _initialMigration;

            if (targetVersion == null)
            {
                // null = run pending migrations up to last.
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
                // Rollback to given version.
                versions = appliedMigrations.Where(x => x > targetVersion).Reverse();
                down = true;
            }
            else if (targetVersion > lastAppliedVersion)
            {
                // Migrate up to given version.
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

            return result;
        }

        /// <inheritdoc/>
        public override Task<int> RunPendingMigrationsAsync(CancellationToken cancelToken = default)
        {
            return MigrateAsync(null, cancelToken);
        }

        protected virtual async Task<int> MigrateUpAsync(IMigrationInfo[] migrations, CancellationToken cancelToken)
        {
            var succeeded = 0;
            var seederEntries = new List<SeederEntry>();
            var runner = (MigrationRunner)_migrationRunner;

            foreach (var migration in migrations)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    // TODO: CreateMigration -> if (migration is IDataSeeder<TContext> seeder) -> add seeder entry

                    // TODO: check FM TransactionBehavior handling.
                    runner.ApplyMigrationUp(migration, migration.TransactionBehavior == TransactionBehavior.Default);

                    ++succeeded;
                    _lastSuccessfulMigration = migration.Description;
                    DbMigrationManager.Instance.AddAppliedMigration(typeof(TContext), migration.Description);
                }
                catch (Exception ex)
                {
                    throw new DbMigrationException(_lastSuccessfulMigration, migration.Description, ex.InnerException ?? ex, false);
                }
            }

            _versionLoader.LoadVersionInfo();
            cancelToken.ThrowIfCancellationRequested();

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
                        //_lastSeedException = new DbMigrationException(lastSuccessful, m.Description, ex.InnerException ?? ex, true);

                        if (!cancelToken.IsCancellationRequested)
                        {
                        }

                        //throw _lastSeedException;
                    }

                    Logger.Warn(ex, "Seed error in migration '{0}'. The error was ignored because no rollback was requested.", m.Description);
                }
            }

            Logger.Info($"Database migration successful: {_initialMigration} >> {_lastSuccessfulMigration}");

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
                    _lastSuccessfulMigration = migration.Description;
                    DbMigrationManager.Instance.AddAppliedMigration(typeof(TContext), migration.Description);
                }
                catch (Exception ex)
                {
                    throw new DbMigrationException(_lastSuccessfulMigration, migration.Description, ex.InnerException ?? ex, false);
                }
            }

            _versionLoader.LoadVersionInfo();

            return succeeded;
        }

        //private static string GetMigrationName(IMigrationInfo migration)
        //{
        //    return migration?.Migration?.GetType()?.Name;
        //}

        protected class MigrationResult
        {
            public int Succeeded { get; set; }
            public List<SeederEntry> SeederEntries { get; set; } = new();
        }

        protected class SeederEntry
        {
            public IDataSeeder<TContext> Seeder { get; set; }
            public IMigrationInfo PreviousMigration { get; set; }
            public IMigrationInfo Migration { get; set; }
        }
    }
}

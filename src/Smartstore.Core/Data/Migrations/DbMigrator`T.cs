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
            var result = MigrateInternal(migrations, true, cancelToken);

            cancelToken.ThrowIfCancellationRequested();

            await RunSeedersAsync(migrations, cancelToken);

            //Logger.Info("Database migration successful: {0} >> {1}".FormatInvariant(initialMigration, lastSuccessful));

            return result.SucceededMigrations;
        }

        protected virtual int MigrateDown(IMigrationInfo[] migrations, CancellationToken cancelToken)
        {
            return MigrateInternal(migrations, false, cancelToken).SucceededMigrations;
        }

        private MigrationResult MigrateInternal(IMigrationInfo[] migrations, bool up, CancellationToken cancelToken)
        {
            var result = new MigrationResult();
            var runner = (MigrationRunner)_migrationRunner;
            IMigrationInfo current = null;

            using (IMigrationScope scope = _runnerOptions.TransactionPerSession ? _migrationRunner.BeginScope() : null)
            {
                try
                {
                    foreach (var migration in migrations)
                    {
                        current = migration;

                        if (cancelToken.IsCancellationRequested)
                        {
                            break;
                        }

                        if (up)
                        {
                            runner.ApplyMigrationUp(migration, migration.TransactionBehavior == TransactionBehavior.Default);
                        }
                        else
                        {
                            runner.ApplyMigrationDown(migration, migration.TransactionBehavior == TransactionBehavior.Default);
                        }

                        ++result.SucceededMigrations;
                        result.LastSucceededMigration = migration;
                    }

                    scope?.Complete();

                    DbMigrationManager.Instance.AddAppliedMigration(typeof(TContext), result.LastSucceededMigration?.Description);
                }
                catch (Exception ex)
                {
                    result.SucceededMigrations = 0;

                    if (scope?.IsActive ?? false)
                    {
                        scope?.Cancel();
                    }

                    throw new DbMigrationException(
                        result.LastSucceededMigration?.Description ?? GetLastSuccessfulMigration()?.Description,
                        current?.Description, 
                        ex.InnerException ?? ex, 
                        false);
                }
            }

            _versionLoader.LoadVersionInfo();

            return result;
        }

        protected virtual async Task RunSeedersAsync(IMigrationInfo[] migrations, CancellationToken cancelToken)
        {
            foreach (var migration in migrations)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                if (migration is IDataSeeder<TContext> seeder)
                {
                    try
                    {
                        // Pre seed event.
                        await _eventPublisher.PublishAsync(new SeedingDbMigrationEvent
                        {
                            Version = migration.Version,
                            Description = migration.Description,
                            DbContext = _db
                        }, cancelToken);

                        // Seed.
                        await seeder.SeedAsync(_db, cancelToken);

                        // Post seed event.
                        await _eventPublisher.PublishAsync(new SeededDbMigrationEvent
                        {
                            Version = migration.Version,
                            Description = migration.Description,
                            DbContext = _db
                        }, cancelToken);
                    }
                    catch (Exception ex)
                    {
                        if (seeder.RollbackOnFailure)
                        {


                            //...
                        }

                        Logger.Warn(ex, "Seed error in migration '{0}'. The error was ignored because no rollback was requested.", migration.Description);
                    }
                }
            }
        }

        private MigrationDescriptor GetLastSuccessfulMigration()
        {
            return GetMigrations().GetValueOrDefault(GetAppliedMigrations().LastOrDefault());
        }

        //private static string GetMigrationName(IMigrationInfo migration)
        //{
        //    return migration?.Migration?.GetType()?.Name;
        //}

        private class MigrationResult
        {
            public int SucceededMigrations { get; set; }
            public IMigrationInfo LastSucceededMigration { get; set; }
        }
    }
}

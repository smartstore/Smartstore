using System.Reflection;
using Autofac;
using FluentMigrator;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.Options;
using Smartstore.Data;
using Smartstore.Data.Migrations;
using Smartstore.Events;

namespace Smartstore.Core.Data.Migrations
{
    public class DbMigrator<TContext> : DbMigrator where TContext : HookingDbContext
    {
        private readonly TContext _db;
        private readonly SmartDbContext _dbCore;
        private readonly IMigrationRunnerConventions _migrationRunnerConventions;
        private readonly IMigrationRunner _migrationRunner;
        private readonly RunnerOptions _runnerOptions;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger _logger;

        private Exception _lastSeedException;

        /// <summary>
        /// The last migration that was successfully executed in a previous session.
        /// </summary>
        private MigrationDescriptor _initialMigration;

        /// <summary>
        /// The last migration that was successfully executed in this session.
        /// </summary>
        private IMigrationInfo _lastSuccessfulMigration;

        public DbMigrator(
            TContext db,
            SmartDbContext dbCore,
            IMigrationTable<TContext> migrationTable,
            IApplicationContext appContext,
            ILifetimeScope scope,
            IMigrationRunnerConventions migrationRunnerConventions,
            IMigrationRunner migrationRunner,
            IOptions<RunnerOptions> runnerOptions,
            IEventPublisher eventPublisher,
            ILogger<DbMigrator<TContext>> logger)
            : base(scope, appContext.TypeScanner, migrationTable)
        {
            Guard.NotNull(db, nameof(db));
            Guard.NotNull(dbCore, nameof(dbCore));

            _db = db;
            _dbCore = dbCore;
            _migrationRunnerConventions = migrationRunnerConventions;
            _migrationRunner = migrationRunner;
            _runnerOptions = runnerOptions.Value;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        /// <inheritdoc/>
        public override TContext Context => _db;

        /// <inheritdoc/>
        public override IMigrationTable<TContext> MigrationTable
            => (IMigrationTable<TContext>)base.MigrationTable;

        /// <inheritdoc/>
        public override Task<int> RunPendingMigrationsAsync(Assembly assembly = null, CancellationToken cancelToken = default)
        {
            return MigrateAsync(null, assembly, cancelToken);
        }

        /// <inheritdoc/>
        public override async Task<int> MigrateAsync(long? targetVersion = null, Assembly assembly = null, CancellationToken cancelToken = default)
        {
            if (_lastSeedException != null)
            {
                // This can happen when a previous migration attempt failed with a rollback.
                throw _lastSeedException;
            }

            var localMigrations = MigrationTable.GetMigrations(assembly);
            if (!localMigrations.Any())
            {
                return 0;
            }

            if (targetVersion > 0 && MigrationTable.GetMigrationByVersion(targetVersion.Value) == null)
            {
                throw new DbMigrationException($"{_db.GetType().Name} does not contain a database migration with version {targetVersion.Value}.");
            }

            var appliedMigrations = MigrationTable.GetAppliedMigrations(assembly).ToArray();
            var lastAppliedVersion = appliedMigrations.LastOrDefault();
            var versions = Enumerable.Empty<long>();
            var down = false;
            var result = 0;

            _initialMigration = MigrationTable.GetMigrationByVersion(lastAppliedVersion);
            _lastSuccessfulMigration = null;

            if (targetVersion == null)
            {
                // null = run pending migrations up to last (inclusive).
                versions = MigrationTable.GetPendingMigrations(assembly);
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
                versions = localMigrations.Select(x => x.Version).Where(x => x > lastAppliedVersion && x <= targetVersion);
            }

            if (!versions.Any())
            {
                // Nothing to do.
                return 0;
            }

            var migrations =
                from v in versions
                let descriptor = MigrationTable.GetMigrationByVersion(v)
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

            MigrationTable.Reload();

            _logger.Info($"Database migration successful: {_initialMigration?.Version.ToString() ?? "[Initial]"} >> {migrations.Last().Version}");
            return result;
        }

        protected virtual async Task<int> MigrateUpAsync(IMigrationInfo[] migrations, CancellationToken cancelToken)
        {
            var isCoreMigration = _db is SmartDbContext;
            var coreSeeders = new List<SeederEntry>();
            var externalSeeders = new List<SeederEntry>();

            void migrationApplied(IMigrationInfo migration)
            {
                // Seeders for the core DbContext must be run in any case 
                // (e.g. for Resource or Setting updates even from external modules).
                if (migration.Migration is IDataSeeder<SmartDbContext> coreSeeder)
                {
                    coreSeeders.Add(new SeederEntry { Seeder = coreSeeder, Migration = migration, PreviousMigration = _lastSuccessfulMigration });
                }

                if (!isCoreMigration && migration.Migration is IDataSeeder<TContext> externalSeeder)
                {
                    externalSeeders.Add(new SeederEntry { Seeder = externalSeeder, Migration = migration, PreviousMigration = _lastSuccessfulMigration });
                }
            }

            var succeeded = MigrateInternal(migrations, true, migrationApplied, cancelToken);

            cancelToken.ThrowIfCancellationRequested();

            if (coreSeeders.Any())
            {
                // Apply core data seeders first,
                await RunSeedersAsync(coreSeeders, _dbCore, cancelToken);
            }

            // Apply external data seeders.
            await RunSeedersAsync(externalSeeders, _db, cancelToken);

            return succeeded;
        }

        protected virtual int MigrateDown(IMigrationInfo[] migrations, CancellationToken cancelToken)
        {
            return MigrateInternal(migrations, false, null, cancelToken);
        }

        private int MigrateInternal(IMigrationInfo[] migrations, bool up, Action<IMigrationInfo> migrationApplied, CancellationToken cancelToken)
        {
            var succeeded = 0;
            IMigrationInfo current = null;
            var runner = (MigrationRunner)_migrationRunner;

            // INFO: execute each migration in own transction scope (default) unless the global option TransactionPerSession is True.
            using (IMigrationScope scope = _runnerOptions.TransactionPerSession ? _migrationRunner.BeginScope() : null)
            {
                try
                {
                    foreach (var migration in migrations)
                    {
                        current = migration;
                        var name = migration.Description.NullEmpty() ?? migration.GetName();

                        if (cancelToken.IsCancellationRequested)
                        {
                            break;
                        }

                        if (up)
                        {
                            runner.ApplyMigrationUp(migration, migration.TransactionBehavior == TransactionBehavior.Default);
                            _logger.Info("Database up migration '{0}' successfully applied.", name);
                        }
                        else
                        {
                            runner.ApplyMigrationDown(migration, migration.TransactionBehavior == TransactionBehavior.Default);
                            _logger.Info("Database down migration '{0}' successfully applied.", name);
                        }

                        migrationApplied?.Invoke(migration);

                        succeeded++;
                        _lastSuccessfulMigration = migration;
                    }

                    scope?.Complete();
                }
                catch (Exception ex)
                {
                    succeeded = 0;

                    if (scope?.IsActive ?? false)
                    {
                        scope?.Cancel();
                    }

                    throw new DbMigrationException(_lastSuccessfulMigration?.Description ?? _initialMigration?.Description, current.Description, ex.InnerException ?? ex, false);
                }
            }

            return succeeded;
        }

        protected virtual async Task RunSeedersAsync<T>(IEnumerable<SeederEntry> seederEntries, T ctx, CancellationToken cancelToken = default) where T : HookingDbContext
        {
            var runner = (MigrationRunner)_migrationRunner;

            foreach (var entry in seederEntries)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                var m = entry.Migration;
                var seeder = (IDataSeeder<T>)entry.Seeder;

                try
                {
                    // Pre seed event.
                    await _eventPublisher.PublishAsync(new SeedingDbMigrationEvent
                    {
                        MigrationVersion = m.Version,
                        MigrationDescription = m.Description,
                        DbContext = ctx
                    }, cancelToken);

                    // Seed.
                    await seeder.SeedAsync(ctx, cancelToken);

                    // Post seed event.
                    await _eventPublisher.PublishAsync(new SeededDbMigrationEvent
                    {
                        MigrationVersion = m.Version,
                        MigrationDescription = m.Description,
                        DbContext = ctx
                    }, cancelToken);
                }
                catch (Exception ex)
                {
                    if (seeder.RollbackOnFailure)
                    {
                        _lastSeedException = new DbMigrationException(entry.PreviousMigration?.Description ?? _initialMigration?.Name, m.Description, ex.InnerException ?? ex, true);

                        if (!cancelToken.IsCancellationRequested)
                        {
                            try
                            {
                                if (entry.PreviousMigration == null && _initialMigration != null)
                                {
                                    // No migration executed in this session -> get the last of all applied migrations.
                                    entry.PreviousMigration = _migrationRunnerConventions.GetMigrationInfoForMigration(CreateMigration(_initialMigration.Type));
                                }

                                if (entry.PreviousMigration != null)
                                {
                                    runner.ApplyMigrationDown(entry.PreviousMigration, entry.PreviousMigration.TransactionBehavior == TransactionBehavior.Default);
                                }
                            }
                            catch (Exception ex2)
                            {
                                _logger.Error(ex2);
                            }
                        }

                        throw _lastSeedException;
                    }

                    _logger.Warn(ex, "Seed error in migration '{0}'. The error was ignored because no rollback was requested.", m.Description);
                }
            }
        }

        protected class SeederEntry
        {
            public object Seeder { get; set; }
            public IMigrationInfo Migration { get; set; }
            public IMigrationInfo PreviousMigration { get; set; }
        }
    }
}

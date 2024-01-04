using System.Reflection;
using Autofac;
using FluentMigrator;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Conventions;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.Options;
using Smartstore.Data;
using Smartstore.Data.Migrations;
using Smartstore.Data.Providers;
using Smartstore.Events;

namespace Smartstore.Core.Data.Migrations
{
    public class DbMigrator<TContext> : DbMigrator where TContext : HookingDbContext
    {
        private readonly TContext _db;
        private readonly SmartDbContext _dbCore;
        private readonly IApplicationContext _appContext;
        private readonly IMigrationRunnerConventions _migrationRunnerConventions;
        private readonly IMigrationRunner _migrationRunner;
        private readonly IConventionSet _conventionSet;
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

        // Value is migration version
        private readonly static List<long> _pendingSeeders = [];

        public DbMigrator(
            TContext db,
            SmartDbContext dbCore,
            IMigrationTable<TContext> migrationTable,
            IApplicationContext appContext,
            ILifetimeScope scope,
            IMigrationRunnerConventions migrationRunnerConventions,
            IMigrationRunner migrationRunner,
            IConventionSet conventionSet,
            IOptions<RunnerOptions> runnerOptions,
            IEventPublisher eventPublisher,
            ILogger<DbMigrator<TContext>> logger)
            : base(scope, migrationTable)
        {
            Guard.NotNull(db);
            Guard.NotNull(dbCore);

            _db = db;
            _dbCore = dbCore;
            _appContext = appContext;
            _migrationRunnerConventions = migrationRunnerConventions;
            _migrationRunner = migrationRunner;
            _conventionSet = conventionSet;
            _runnerOptions = runnerOptions.Value;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        private bool IsCoreMigration => _db is SmartDbContext;

        /// <inheritdoc/>
        public override TContext Context => _db;

        /// <inheritdoc/>
        public override IMigrationTable<TContext> MigrationTable
            => (IMigrationTable<TContext>)base.MigrationTable;

        /// <inheritdoc/>
        public override Task<int> RunPendingMigrationsAsync(Assembly assembly = null, CancellationToken cancelToken = default)
            => MigrateAsync(null, assembly, cancelToken);

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

            var migrations = GetMigrationInfosForVersions(versions);

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
            var coreSeeders = new List<SeederEntry>();
            var externalSeeders = new List<SeederEntry>();

            void migrationApplied(IMigrationInfo migration)
            {
                // Seeders for the core DbContext must be run in any case 
                // (e.g. for Resource or Setting updates even from external modules).
                if (migration.Migration is IDataSeeder<SmartDbContext> coreSeeder)
                {
                    if (coreSeeder.Stage == DataSeederStage.Early)
                    {
                        coreSeeders.Add(new SeederEntry { Seeder = coreSeeder, Migration = migration, PreviousMigration = _lastSuccessfulMigration });
                    }
                    else
                    {
                        _pendingSeeders.Add(migration.Version);
                    }
                }

                if (!IsCoreMigration && migration.Migration is IDataSeeder<TContext> externalSeeder)
                {
                    if (externalSeeder.Stage == DataSeederStage.Early)
                    {
                        externalSeeders.Add(new SeederEntry { Seeder = externalSeeder, Migration = migration, PreviousMigration = _lastSuccessfulMigration });
                    }
                    else
                    {
                        _pendingSeeders.Add(migration.Version);
                    }
                }
            }

            var succeeded = MigrateInternal(migrations, true, migrationApplied, cancelToken);

            cancelToken.ThrowIfCancellationRequested();

            if (coreSeeders.Any())
            {
                // Apply core data seeders first,
                await RunEarlySeedersAsync(coreSeeders, _dbCore, cancelToken);
            }

            if (externalSeeders.Any())
            {
                // Apply external data seeders.
                await RunEarlySeedersAsync(externalSeeders, _db, cancelToken);
            }

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

            ConfigureConventions(_conventionSet);

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

        protected virtual async Task RunEarlySeedersAsync<T>(IEnumerable<SeederEntry> seederEntries, T ctx, CancellationToken cancelToken = default) where T : HookingDbContext
        {
            foreach (var entry in seederEntries)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await RunSeeder(entry, ctx, cancelToken);
                }
                catch (Exception ex)
                {
                    var m = entry.Migration;
                    var seeder = (IDataSeeder<T>)entry.Seeder;

                    if (seeder.AbortOnFailure)
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
                                    var runner = (MigrationRunner)_migrationRunner;
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

        /// <inheritdoc/>
        public override async Task RunLateSeedersAsync(CancellationToken cancelToken = default)
        {
            if (_pendingSeeders.Count == 0)
            {
                return;
            }

            var migrations = GetMigrationInfosForVersions(_pendingSeeders);
            var coreSeeders = new List<SeederEntry>();
            var externalSeeders = new List<SeederEntry>();

            foreach (var migration in migrations)
            {
                // Seeders for the core DbContext must be run in any case 
                // (e.g. for Resource or Setting updates even from external modules).
                if (migration.Migration is IDataSeeder<SmartDbContext> coreSeeder)
                {
                    coreSeeders.Add(new SeederEntry { Seeder = coreSeeder, Migration = migration });
                }

                if (!IsCoreMigration && migration.Migration is IDataSeeder<TContext> externalSeeder)
                {
                    externalSeeders.Add(new SeederEntry { Seeder = externalSeeder, Migration = migration });
                }
            }

            cancelToken.ThrowIfCancellationRequested();

            if (coreSeeders.Any())
            {
                // Apply core data seeders first,
                await RunLateSeedersAsync(coreSeeders, _dbCore, cancelToken);
            }

            if (externalSeeders.Any())
            {
                // Apply external data seeders.
                await RunLateSeedersAsync(externalSeeders, _db, cancelToken);
            }
        }

        private async Task RunLateSeedersAsync<T>(IEnumerable<SeederEntry> seederEntries, T ctx, CancellationToken cancelToken = default) where T : HookingDbContext
        {
            foreach (var entry in seederEntries)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await RunSeeder(entry, ctx, cancelToken);
                    _pendingSeeders.Remove(entry.Migration.Version);
                }
                catch (Exception ex)
                {
                    var m = entry.Migration;
                    var seeder = (IDataSeeder<T>)entry.Seeder;

                    if (seeder.AbortOnFailure)
                    {
                        throw new DbMigrationException(null, m.Description, ex.InnerException ?? ex, true);
                    }
                    else
                    {
                        _logger.Error(ex, "Seed error in migration '{0}'.", m.Description);
                    }  
                }
            }
        }

        private async Task RunSeeder<T>(SeederEntry entry, T ctx, CancellationToken cancelToken = default) where T : HookingDbContext
        {
            var m = entry.Migration;
            var seeder = (IDataSeeder<T>)entry.Seeder;

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

        protected virtual void ConfigureConventions(IConventionSet conventionSet)
        {
            var options = _db.Options.FindExtension<DbFactoryOptionsExtension>();

            var conventionProviders = _appContext.TypeScanner
                .FindTypes<IConventionSource>(options.ModelAssemblies)
                .Select(Activator.CreateInstance)
                .Cast<IConventionSource>();

            foreach (var provider in conventionProviders)
            {
                provider.Configure(conventionSet);
            }
        }

        private IEnumerable<IMigrationInfo> GetMigrationInfosForVersions(IEnumerable<long> versions)
        {
            var migrations =
                from v in versions
                let descriptor = MigrationTable.GetMigrationByVersion(v)
                let instance = CreateMigration(descriptor.Type)
                select _migrationRunnerConventions.GetMigrationInfoForMigration(instance);

            return migrations;
        }

        protected class SeederEntry
        {
            public object Seeder { get; set; }
            public IMigrationInfo Migration { get; set; }
            public IMigrationInfo PreviousMigration { get; set; }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using Smartstore.Engine;

namespace Smartstore.Core.Data.Migrations
{
    public class DbMigrator2<TContext> : DbMigrator2 where TContext : HookingDbContext
    {
        private readonly TContext _db;
        private readonly IFilteringMigrationSource _filteringMigrationSource;
        private readonly IMigrationRunnerConventions _migrationRunnerConventions;
        private readonly IMigrationRunner _migrationRunner;
        private readonly IVersionLoader _versionLoader;
        private readonly RunnerOptions _runnerOptions;

        public DbMigrator2(
            TContext db,
            IApplicationContext appContext,
            ILifetimeScope scope,
            IFilteringMigrationSource filteringMigrationSource,
            IMigrationRunnerConventions migrationRunnerConventions,
            IMigrationRunner migrationRunner,
            IVersionLoader versionLoader,
            IOptions<RunnerOptions> runnerOptions)
            : base(scope, appContext.TypeScanner, versionLoader)
        {
            _db = db;
            _filteringMigrationSource = filteringMigrationSource;
            _migrationRunnerConventions = migrationRunnerConventions;
            _migrationRunner = migrationRunner;
            _versionLoader = versionLoader;
            _runnerOptions = runnerOptions.Value;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public override TContext Context => _db;

        /// <inheritdoc/>
        public override async Task<int> MigrateAsync(long? targetVersion = null, CancellationToken cancelToken = default)
        {
            // TODO: (mg) (core) Throw when this method is called during installation. Migrations MUST NOT run during installation.
            
            var localMigrations = GetMigrations();
            if (localMigrations.Count == 0)
            {
                return 0;
            }

            if (targetVersion > 0 && !localMigrations.ContainsKey(targetVersion.Value))
            {
                // TODO: (mg) (core) Throw after DbMigrationException has been refactored
                //throw new DbMigrationException("", "", null, false);
            }

            //var targetMigration = targetVersion == null
            //    // null = run pending migration up to last
            //    ? localMigrations.Last().Value
            //    : (targetVersion == -1
            //        // -1 = rollback all applied migrations
            //        ? localMigrations.First().Value
            //        // > 0 = up or down to given version
            //        : localMigrations[targetVersion.Value]);

            var appliedMigrations = GetAppliedMigrations().ToArray();
            //var lastAppliedVersion = appliedMigrations.LastOrDefault();

            var versions = targetVersion == null
                // null = run pending migration up to last
                ? GetPendingMigrations()
                : (targetVersion == -1
                    // -1 = rollback all applied migrations
                    ? appliedMigrations.Reverse()
                    // > 0 = up or down to given version
                    // TODO: (mg) (core) Determine migrations to run. Reverse if down.
                    : Enumerable.Empty<long>());

            if (!versions.Any())
            {
                // Nothing to do
                return 0;
            }

            var migrations = 
                from v in versions
                let descriptor = localMigrations[v]
                let instance = CreateMigration(descriptor.Type)
                select _migrationRunnerConventions.GetMigrationInfoForMigration(instance);

            // TODO: (mg) (core) Determine whether this is a rollback.
            var down = false;

            if (!down)
            {
                // TODO: (mg) (core) Perform MigrateUp for given migrations, e.g. MigrateUpAsync(migrations.ToArray())
            }
            else
            {
                // TODO: (mg) (core) Perform MigrateDown for given migrations, e.g. MigrateDown(migrations.ToArray()). Async not necessarily required 'cause no seeding here.
            }

            // TODO: (mg) (core) RunPendingMigrationsAsync() just calls MigrateAsync(null).

            // Remove later.
            await Task.Delay(10);

            return 0;
        }

        public override int RunPendingMigrationsAsync(CancellationToken cancelToken = default)
        {
            //if (!_migrationRunner.HasMigrationsToApplyUp())
            //{
            //    return 0;
            //}

            var result = 0;
            var assembly = _db.GetType().Assembly;
            //var assembly = EngineContext.Current.ResolveService<Engine.ITypeScanner>().Assemblies.SingleOrDefault(x => x.GetName().Name.StartsWith("Smartstore.DevTools"));
            var migrationInfos = GetMigrationInfos(assembly, true);
            var runner = (MigrationRunner)_migrationRunner;

            //LogInfo("Migrating up", assembly, migrationInfos);

            using (IMigrationScope scope = _runnerOptions.TransactionPerSession ? _migrationRunner.BeginScope() : null)
            {
                try
                {
                    foreach (var info in migrationInfos)
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            break;
                        }

                        runner.ApplyMigrationUp(info, info.TransactionBehavior == TransactionBehavior.Default);
                        ++result;
                    }
                    
                    scope?.Complete();
                }
                catch
                {
                    result = 0;

                    if (scope?.IsActive ?? false)
                    {
                        scope?.Cancel();
                    }

                    throw;
                }
            }

            _versionLoader.LoadVersionInfo();
            cancelToken.ThrowIfCancellationRequested();

            return result;
        }

        public override int MigrateDown(CancellationToken cancelToken = default)
        {
            var result = 0;
            var assembly = Assembly.GetAssembly(_db.GetType());
            var migrationInfos = GetMigrationInfos(assembly, false);
            var runner = (MigrationRunner)_migrationRunner;

            //LogInfo("Migrating down", assembly, migrationInfos);

            using (IMigrationScope scope = _runnerOptions.TransactionPerSession ? _migrationRunner.BeginScope() : null)
            {
                try
                {
                    foreach (var info in migrationInfos)
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            break;
                        }

                        runner.ApplyMigrationDown(info, info.TransactionBehavior == TransactionBehavior.Default);
                        ++result;
                    }

                    scope?.Complete();
                }
                catch
                {
                    result = 0;

                    if (scope?.IsActive ?? false)
                    {
                        scope?.Cancel();
                    }

                    throw;
                }
            }

            _versionLoader.LoadVersionInfo();
            cancelToken.ThrowIfCancellationRequested();

            return result;
        }

        protected virtual IEnumerable<IMigrationInfo> GetMigrationInfos(Assembly assembly, bool ascending)
        {
            // Perf: IMigration is internally cached by FM via ConcurrentDictionary<Type, IMigration> (see MigrationSource).
            var migrations = _filteringMigrationSource.GetMigrations(x => x.Assembly == assembly);

            if (migrations?.Any() ?? false)
            {
                var migrationInfos = migrations
                    //.Where(x => _migrationRunnerConventions.TypeIsMigration(x.GetType()))     // If someone forgets using MigrationAttribute GetMigrationInfoForMigration crashes.
                    .Select(x => _migrationRunnerConventions.GetMigrationInfoForMigration(x));

                return ascending
                    ? migrationInfos.OrderBy(x => x.Version)
                    : migrationInfos.OrderByDescending(x => x.Version);
            }

            return Enumerable.Empty<IMigrationInfo>();
        }

        //private void LogInfo(string text, Assembly assembly, IEnumerable<IMigrationInfo> infos)
        //{
        //    if (infos.Any())
        //    {
        //        Logger.Info($"{text} {assembly.GetName().Name}: {string.Join(" ", infos.Select(x => x.Description))}");
        //    }
        //}
    }
}

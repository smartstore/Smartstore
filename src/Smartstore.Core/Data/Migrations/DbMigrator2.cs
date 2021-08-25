using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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
    public abstract class DbMigrator2
    {
        // TODO: (mg) (core) Use only relevant FluentMigrator dependencies/packages (SqlServer, MySql). Remove everything else!
        public abstract HookingDbContext Context { get; }

        // TODO: (mg) (core) The contract should follow the old contract (DbMigrator). We need DbContext for translation and setting seeding. We can't break with our concept.
        public abstract int RunPendingMigrationsAsync(CancellationToken cancelToken = default);
        public abstract int MigrateDown(CancellationToken cancelToken = default);
    }

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
            IFilteringMigrationSource filteringMigrationSource,
            IMigrationRunnerConventions migrationRunnerConventions,
            IMigrationRunner migrationRunner,
            IVersionLoader versionLoader,
            IOptions<RunnerOptions> runnerOptions)
        {
            _db = Guard.NotNull(db, nameof(db));

            _filteringMigrationSource = filteringMigrationSource;
            _migrationRunnerConventions = migrationRunnerConventions;
            _migrationRunner = migrationRunner;
            _versionLoader = versionLoader;
            _runnerOptions = runnerOptions.Value;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public override TContext Context => _db;

        public override int RunPendingMigrationsAsync(CancellationToken cancelToken = default)
        {
            //if (!_migrationRunner.HasMigrationsToApplyUp())
            //{
            //    return 0;
            //}

            var result = 0;
            var assembly = Assembly.GetAssembly(_db.GetType());
            //var assembly = EngineContext.Current.ResolveService<Engine.ITypeScanner>().Assemblies.SingleOrDefault(x => x.GetName().Name.StartsWith("Smartstore.DevTools"));
            var migrationInfos = GetMigrationInfos(assembly, true);
            var runner = (MigrationRunner)_migrationRunner;

            LogInfo("Migrating up", assembly, migrationInfos);

            using (IMigrationScope scope = _runnerOptions.TransactionPerSession ? _migrationRunner.BeginScope() : null)
            {
                try
                {
                    runner.ApplyMaintenance(MigrationStage.BeforeAll, true);

                    foreach (var info in migrationInfos)
                    {
                        if (cancelToken.IsCancellationRequested)
                        {
                            break;
                        }

                        runner.ApplyMaintenance(MigrationStage.BeforeEach, true);
                        runner.ApplyMigrationUp(info, info.TransactionBehavior == TransactionBehavior.Default);
                        runner.ApplyMaintenance(MigrationStage.AfterEach, true);

                        ++result;
                    }

                    runner.ApplyMaintenance(MigrationStage.BeforeProfiles, true);
                    runner.ApplyMaintenance(MigrationStage.AfterAll, true);

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

            LogInfo("Migrating down", assembly, migrationInfos);

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
            var migrations = _filteringMigrationSource.GetMigrations(x => x.Assembly == assembly) ?? Enumerable.Empty<IMigration>();
            var migrationInfos = migrations.Select(x => _migrationRunnerConventions.GetMigrationInfoForMigration(x));

            return ascending
                ? migrationInfos.OrderBy(x => x.Version)
                : migrationInfos.OrderByDescending(x => x.Version);
        }

        private void LogInfo(string text, Assembly assembly, IEnumerable<IMigrationInfo> infos)
        {
            if (infos.Any())
            {
                Logger.Info($"{text} {assembly.GetName().Name}: {string.Join(" ", infos.Select(x => x.Description))}");
            }
        }
    }
}

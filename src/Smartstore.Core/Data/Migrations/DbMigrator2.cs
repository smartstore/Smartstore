using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentMigrator;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Data;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public abstract class DbMigrator2
    {
        // TODO: (mg) (core) Use only relevant FluentMigrator dependencies/packages (SqlServer, MySql). Remove everything else!
        public abstract HookingDbContext Context { get; }

        // TODO: (mg) (core) I don't like "Assembly" param in the signature. Maybe more conceptual? Not technical.
        // TODO: (mg) (core) The contract should follow the old contract (DbMigrator). We need DbContext for translation and setting seeding. We can't break with our concept.
        public abstract Task RunPendingMigrationsAsync();
        public abstract void MigrateDown(Assembly assembly, CancellationToken cancelToken = default);
    }

    public class DbMigrator2<TContext> : DbMigrator2 where TContext : HookingDbContext
    {
        private readonly TContext _db;
        private readonly IFilteringMigrationSource _filteringMigrationSource;
        private readonly IMigrationRunnerConventions _migrationRunnerConventions;
        private readonly IMigrationRunner _migrationRunner;
        private readonly IVersionLoader _versionLoader;

        public DbMigrator2(
            TContext db,
            IFilteringMigrationSource filteringMigrationSource,
            IMigrationRunnerConventions migrationRunnerConventions,
            IMigrationRunner migrationRunner,
            IVersionLoader versionLoader)
        {
            _db = Guard.NotNull(db, nameof(db));

            _filteringMigrationSource = filteringMigrationSource;
            _migrationRunnerConventions = migrationRunnerConventions;
            _migrationRunner = migrationRunner;
            _versionLoader = versionLoader;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public override TContext Context => _db;

        public override async Task RunPendingMigrationsAsync()
        {
            if (!_migrationRunner.HasMigrationsToApplyUp())
            {
                return;
            }

            var latestVersion = _versionLoader.VersionInfo.Latest();
            if (latestVersion == 0 && await ShouldSuppressInitialCreate())
            {
                DbMigrationManager.Instance.SetSuppressInitialCreate<TContext>(true);
            }

            // INFO: this executes all migrations in ALL assemblies registered by IMigrationRunnerBuilder.ScanIn:
            _migrationRunner.MigrateUp();
        }

        public override void MigrateDown(Assembly assembly, CancellationToken cancelToken = default)
        {
            Guard.NotNull(assembly, nameof(assembly));

            var migrationInfos = GetMigrationInfos(assembly, false);
            LogInfo("Migrating down", assembly, migrationInfos);

            foreach (var info in migrationInfos)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                _migrationRunner.Down(info.Migration);
                _versionLoader.DeleteVersion(info.Version);
            }
        }

        protected virtual IEnumerable<IMigrationInfo> GetMigrationInfos(Assembly assembly, bool ascending)
        {
            var migrations = _filteringMigrationSource.GetMigrations(x => x.Assembly == assembly) ?? Enumerable.Empty<IMigration>();
            var migrationInfos = migrations.Select(x => _migrationRunnerConventions.GetMigrationInfoForMigration(x));

            return ascending
                ? migrationInfos.OrderBy(x => x.Version)
                : migrationInfos.OrderByDescending(x => x.Version);
        }

        private async Task<bool> ShouldSuppressInitialCreate()
        {
            var tablesToCheck = _db.GetInvariantType().GetAttribute<CheckTablesAttribute>(true)?.TableNames;
            if (tablesToCheck != null && tablesToCheck.Length > 0)
            {
                var dbTables = await _db.DataProvider.GetTableNamesAsync();
                return dbTables.Intersect(tablesToCheck, StringComparer.InvariantCultureIgnoreCase).Count() == tablesToCheck.Length;
            }

            return false;
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

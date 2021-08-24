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

namespace Smartstore.Core.Data.Migrations
{
    public interface IDbMigrator2
    {
        // TODO: (mg) (core) I don't like "Assembly" param in the signature. Maybe more conceptual? Not technical.
        // TODO: (mg) (core) The contract should follow the old contract (DbMigrator). We need DbContext for translation and setting seeding. We can't break with our concept.
        void MigrateUp(Assembly assembly, CancellationToken cancelToken = default);
        void MigrateDown(Assembly assembly, CancellationToken cancelToken = default);
    }

    public partial class DbMigrator2 : IDbMigrator2
    {
        private readonly IFilteringMigrationSource _filteringMigrationSource;
        private readonly IMigrationRunnerConventions _migrationRunnerConventions;
        private readonly IMigrationRunner _migrationRunner;
        private readonly IVersionLoader _versionLoader;

        public DbMigrator2(
            IFilteringMigrationSource filteringMigrationSource,
            IMigrationRunnerConventions migrationRunnerConventions,
            IMigrationRunner migrationRunner,
            IVersionLoader versionLoader)
        {
            _filteringMigrationSource = filteringMigrationSource;
            _migrationRunnerConventions = migrationRunnerConventions;
            _migrationRunner = migrationRunner;
            _versionLoader = versionLoader;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public void MigrateUp(Assembly assembly, CancellationToken cancelToken = default)
        {
            Guard.NotNull(assembly, nameof(assembly));

            var migrationInfos = GetMigrationInfos(assembly, true);
            LogInfo("Migrating up", assembly, migrationInfos);

            foreach (var info in migrationInfos)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    break;
                }

                //var versionAttribute = info.Migration.GetType().GetCustomAttribute<MigrationVersionAttribute>(false);
                //if (versionAttribute.IsInitial && !includeInitial)
                //{
                //    continue;
                //}

                // INFO: this executes all migrations in ALL assemblies up to info.Version:
                _migrationRunner.MigrateUp(info.Version);
            }
        }

        public void MigrateDown(Assembly assembly, CancellationToken cancelToken = default)
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

        private void LogInfo(string text, Assembly assembly, IEnumerable<IMigrationInfo> infos)
        {
            if (infos.Any())
            {
                Logger.Info($"{text} {assembly.GetName().Name}: {string.Join(" ", infos.Select(x => x.Description))}");
            }
        }
    }
}

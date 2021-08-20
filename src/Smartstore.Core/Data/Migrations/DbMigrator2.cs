using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentMigrator;
using FluentMigrator.Infrastructure;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;

namespace Smartstore.Core.Data.Migrations
{
    public interface IDbMigrator2
    {
        void MigrateUp(Assembly assembly);
        void MigrateDown(Assembly assembly);
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

        public void MigrateUp(Assembly assembly)
        {
            Guard.NotNull(assembly, nameof(assembly));

            var migrationInfos = GetMigrationInfos(assembly, true);

            foreach (var info in migrationInfos)
            {
                _migrationRunner.MigrateUp(info.Version);
            }
        }

        public void MigrateDown(Assembly assembly)
        {
            Guard.NotNull(assembly, nameof(assembly));

            var migrationInfos = GetMigrationInfos(assembly, false);

            foreach (var info in migrationInfos)
            {
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
    }
}

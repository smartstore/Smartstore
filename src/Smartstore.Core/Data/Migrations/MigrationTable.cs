using FluentMigrator.Runner;
using Smartstore.Data;
using Smartstore.Data.Providers;

namespace Smartstore.Core.Data.Migrations
{
    /// <summary>
    /// Reads migration descriptors from all configured model assemblies.
    /// </summary>
    public interface IMigrationTable
    {
        /// <summary>
        /// Gets all the migrations that are defined in the migration assemblies.
        /// </summary>
        IReadOnlyCollection<MigrationDescriptor> GetMigrations();

        /// <summary>
        /// Gets a migration by version.
        /// </summary>
        MigrationDescriptor GetMigrationByVersion(long version);

        /// <summary>
        /// Gets all migrations that have been applied to the target database.
        /// </summary>
        IEnumerable<long> GetAppliedMigrations();

        /// <summary>
        /// Gets all migrations that are defined in the assemblies but haven't been applied to the target database.
        /// </summary>
        IEnumerable<long> GetPendingMigrations();

        /// <summary>
        /// Loads all version data stored in the version table.
        /// </summary>
        void Reload();

        /// <summary>
        /// Adds the version information to the version table in the database.
        /// </summary>
        /// <param name="version">The version number</param>
        /// <param name="description">The optional version description</param>
        void UpdateVersionInfo(long version, string description);
    }

    /// <inheritdoc cref="IMigrationTable" />
    public interface IMigrationTable<TContext> : IMigrationTable where TContext : HookingDbContext
    {
    }


    public class MigrationTable<TContext> : IMigrationTable<TContext>
        where TContext : HookingDbContext
    {
        private readonly IVersionLoader _versionLoader;
        private readonly MigrationAssembly[] _assemblies;
        private IReadOnlyCollection<MigrationDescriptor> _migrations;
        private Dictionary<long, int> _versionMap = new();

        public MigrationTable(DbContextOptions<TContext> options, IVersionLoader versionLoader)
        {
            _versionLoader = versionLoader;

            var assemblies = options.FindExtension<DbFactoryOptionsExtension>()?.ModelAssemblies;

            _assemblies = assemblies == null
                ? Array.Empty<MigrationAssembly>()
                : assemblies.Select(assembly => new MigrationAssembly(assembly)).ToArray();
        }

        /// <summary>
        /// Gets all the migrations that are defined in the migration assemblies.
        /// </summary>
        public virtual IReadOnlyCollection<MigrationDescriptor> GetMigrations()
        {
            IReadOnlyCollection<MigrationDescriptor> Create()
            {
                var result = _assemblies
                    .SelectMany(x => x.GetMigrations())
                    .ToList();

                for (var i = 0; i < result.Count; i++)
                {
                    _versionMap[result[i].Version] = i;
                }

                return result;
            }

            return _migrations ??= Create();
        }

        /// <summary>
        /// Gets a migration by version.
        /// </summary>
        public virtual MigrationDescriptor GetMigrationByVersion(long version)
        {
            var migrations = (List<MigrationDescriptor>)GetMigrations();
            if (_versionMap.TryGetValue(version, out var index))
            {
                return migrations[index];
            }

            return null;
        }

        /// <summary>
        /// Gets all migrations that have been applied to the target database.
        /// </summary>
        public virtual IEnumerable<long> GetAppliedMigrations()
            => GetMigrations().Select(x => x.Version).Intersect(_versionLoader.VersionInfo.AppliedMigrations());

        /// <summary>
        /// Gets all migrations that are defined in the assemblies but haven't been applied to the target database.
        /// </summary>
        public virtual IEnumerable<long> GetPendingMigrations()
            => GetMigrations().Select(x => x.Version).Except(GetAppliedMigrations());

        public virtual void Reload()
            => _versionLoader.LoadVersionInfo();

        public virtual void UpdateVersionInfo(long version, string description = null)
            => _versionLoader.UpdateVersionInfo(version, description);
    }
}

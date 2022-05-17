using System.Reflection;
using FluentMigrator.Runner;
using Smartstore.Data;
using Smartstore.Data.Providers;

namespace Smartstore.Core.Data.Migrations
{
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

        /// <inheritdoc />
        public virtual IEnumerable<MigrationDescriptor> GetMigrations(Assembly assembly = null)
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

            var migrations = _migrations ??= Create();
            if (assembly == null)
            {
                return migrations;
            }

            return migrations.Where(x => x.Type.Assembly == assembly);
        }

        /// <inheritdoc />
        public virtual MigrationDescriptor GetMigrationByVersion(long version)
        {
            var migrations = (List<MigrationDescriptor>)GetMigrations();
            if (_versionMap.TryGetValue(version, out var index))
            {
                return migrations[index];
            }

            return null;
        }

        /// <inheritdoc />
        public virtual IEnumerable<long> GetAppliedMigrations(Assembly assembly = null)
            => GetMigrations(assembly).Select(x => x.Version).Intersect(_versionLoader.VersionInfo.AppliedMigrations());

        /// <inheritdoc />
        public virtual IEnumerable<long> GetPendingMigrations(Assembly assembly = null)
            => GetMigrations(assembly).Select(x => x.Version).Except(GetAppliedMigrations());

        /// <inheritdoc />
        public virtual void Reload()
            => _versionLoader.LoadVersionInfo();

        /// <inheritdoc />
        public virtual void UpdateVersionInfo(long version, string description = null)
            => _versionLoader.UpdateVersionInfo(version, description);
    }
}

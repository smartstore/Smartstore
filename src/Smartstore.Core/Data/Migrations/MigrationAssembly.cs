using System.Reflection;
using FluentMigrator;

namespace Smartstore.Core.Data.Migrations
{
    /// <summary>
    /// Represents an assembly containing database migration classes.
    /// </summary>
    internal class MigrationAssembly
    {
        private readonly Assembly _assembly;
        private IReadOnlyCollection<MigrationDescriptor> _migrations;

        public MigrationAssembly(Assembly assembly)
        {
            _assembly = Guard.NotNull(assembly, nameof(assembly));
        }

        /// <summary>
        /// Gets all the migrations that are defined in the migrations assembly.
        /// </summary>
        public virtual IReadOnlyCollection<MigrationDescriptor> GetMigrations()
        {
            IReadOnlyCollection<MigrationDescriptor> Create()
            {
                var result = new List<MigrationDescriptor>();
                var typeScanner = new DefaultTypeScanner(_assembly);

                var items
                    = from t in typeScanner.FindTypes<IMigration>()
                      let descriptor = new MigrationDescriptor(t)
                      where descriptor.Version > 0
                      orderby descriptor.Version
                      select descriptor;

                foreach (var descriptor in items)
                {
                    result.Add(descriptor);
                }

                return result;
            }

            return _migrations ??= Create();
        }
    }
}

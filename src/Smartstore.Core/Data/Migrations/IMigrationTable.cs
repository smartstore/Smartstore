using System.Reflection;
using Smartstore.Data;

namespace Smartstore.Core.Data.Migrations
{
    /// <summary>
    /// Reads migration descriptors from all configured model assemblies.
    /// </summary>
    public interface IMigrationTable
    {
        /// <summary>
        /// Gets all the migrations that are defined in migration assemblies.
        /// </summary>
        /// <param name="assembly">
        ///     Pass an <see cref="Assembly"/> instance to reduce the result set to migrations found in the given assembly only.
        ///     Pass <c>null</c> to collect all migrations across all assemblies.
        /// </param>
        IEnumerable<MigrationDescriptor> GetMigrations(Assembly assembly = null);

        /// <summary>
        /// Gets a migration by version.
        /// </summary>
        MigrationDescriptor GetMigrationByVersion(long version);

        /// <summary>
        /// Gets all migrations that have been applied to the target database.
        /// </summary>
        /// <param name="assembly">
        ///     Pass an <see cref="Assembly"/> instance to reduce the result set to migrations found in the given assembly only.
        ///     Pass <c>null</c> to collect all migrations across all assemblies.
        /// </param>
        IEnumerable<long> GetAppliedMigrations(Assembly assembly = null);

        /// <summary>
        /// Gets all migrations that are defined but haven't been applied to the target database.
        /// </summary>
        /// <param name="assembly">
        ///     Pass an <see cref="Assembly"/> instance to reduce the result set to migrations found in the given assembly only.
        ///     Pass <c>null</c> to collect all migrations across all assemblies.
        /// </param>
        IEnumerable<long> GetPendingMigrations(Assembly assembly = null);

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
}

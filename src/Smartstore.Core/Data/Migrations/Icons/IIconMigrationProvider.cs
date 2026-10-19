#nullable enable

namespace Smartstore.Core.Data.Migrations.Icons;

/// <summary>
/// Migrates structured icon data when invoked by the icon migrator.
/// Event consumers add instances to <see cref="IconMigrationEvent.Providers"/>.
/// Providers run before the separately registered HTML fields.
/// Run-specific resources are supplied through the migration context, not constructor injection.
/// </summary>
public interface IIconMigrationProvider
{
    /// <summary>
    /// Migrates owned data, skipping already migrated values. The provider owns persistence
    /// and may publish report entries through the context. It must not dispose the shared context.
    /// Resources created by the provider must be released before this method returns.
    /// </summary>
    /// <param name="context">Shared database, converter and reporting access for this run.</param>
    /// <param name="cancelToken">A token to cancel the operation.</param>
    /// <returns>The provider's migration totals.</returns>
    Task<IconMigrationResult> MigrateAsync(IconMigrationContext context, CancellationToken cancelToken = default);
}

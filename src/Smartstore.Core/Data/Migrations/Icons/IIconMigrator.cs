#nullable enable

namespace Smartstore.Core.Data.Migrations.Icons;

/// <summary>
/// Defines the migration of persisted legacy icons to icon expressions.
/// </summary>
public interface IIconMigrator
{
    /// <summary>
    /// Migrates remaining legacy icons using the caller's database context.
    /// The caller owns the context and its lifetime; no HTTP context is required.
    /// </summary>
    /// <param name="context">The database context supplied by the migration or backend scope.</param>
    /// <param name="report">Receives structured entries as the migration progresses. Storage is owned by the caller.</param>
    /// <param name="cancelToken">A token to cancel the operation.</param>
    /// <returns>A summary of the migration run.</returns>
    Task<IconMigrationResult> MigrateAsync(
        SmartDbContext context,
        Action<IconMigrationEntry>? report = null,
        CancellationToken cancelToken = default);
}

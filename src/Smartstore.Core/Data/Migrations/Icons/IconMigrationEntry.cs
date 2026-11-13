#nullable enable

namespace Smartstore.Core.Data.Migrations.Icons;

/// <summary>
/// A single report entry emitted during icon migration, without prescribing report storage.
/// </summary>
/// <param name="EntityName">The entity or plugin record type.</param>
/// <param name="RecordId">The affected record identifier.</param>
/// <param name="Field">The affected field.</param>
/// <param name="Status">The outcome: Warning, Migrated or NotMigrated.</param>
/// <param name="OldValue">The original persisted value.</param>
/// <param name="NewValue">The migrated value, when available.</param>
/// <param name="Message">An optional diagnostic message.</param>
public sealed record IconMigrationEntry(
    string EntityName,
    int RecordId,
    string Field,
    string Status,
    string? OldValue = null,
    string? NewValue = null,
    string? Message = null);

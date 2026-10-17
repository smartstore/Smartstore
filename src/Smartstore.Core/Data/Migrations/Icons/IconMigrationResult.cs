#nullable enable

namespace Smartstore.Core.Data.Migrations.Icons;

/// <summary>
/// Summarizes an icon migration run without retaining its individual report entries.
/// </summary>
public class IconMigrationResult
{
    /// <summary>
    /// Gets the number of records checked.
    /// </summary>
    public int CheckedRecords { get; init; }

    /// <summary>
    /// Gets the number of icons migrated.
    /// </summary>
    public int MigratedIcons { get; init; }

    /// <summary>
    /// Gets the number of legacy icons that could not be migrated.
    /// </summary>
    public int NotMigratedIcons { get; init; }

    /// <summary>
    /// Gets the reference to the persisted report, or null when no report was created.
    /// </summary>
    public string? ReportReference { get; init; }
}

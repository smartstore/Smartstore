#nullable enable

using System.Linq.Expressions;

namespace Smartstore.Core.Data.Migrations.Icons;

/// <summary>
/// Shares conversion and reporting facilities for one migration run without owning the database context.
/// </summary>
public sealed class IconMigrationContext
{
    private readonly Action<IconMigrationEntry>? _report;

    /// <summary>
    /// Creates a context using the caller's database and optional report destination.
    /// </summary>
    public IconMigrationContext(SmartDbContext dbContext, Action<IconMigrationEntry>? report = null, LegacyIconConverter? converter = null)
    {
        DbContext = Guard.NotNull(dbContext);
        _report = report;
        Converter = converter ?? new LegacyIconConverter();
    }

    /// <summary>
    /// Gets the database context owned by the caller's migration or backend scope.
    /// </summary>
    public SmartDbContext DbContext { get; }

    /// <summary>
    /// Gets the persistence-independent converter shared by providers.
    /// </summary>
    public LegacyIconConverter Converter { get; }

    /// <summary>
    /// Gets the HTML field expressions registered for this run.
    /// </summary>
    public List<LambdaExpression> HtmlFields { get; } = [];

    /// <summary>
    /// Forwards a structured report entry when a report destination was supplied.
    /// </summary>
    public void Report(IconMigrationEntry entry)
    {
        Guard.NotNull(entry);
        _report?.Invoke(entry);
    }
}

using System.Linq.Expressions;
using Smartstore.Events;

namespace Smartstore.Core.Data.Migrations.Icons;

/// <summary>
/// Collects providers and HTML field expressions for one automatic or manual icon migration run.
/// Do not use fire-and-forget handlers or retain the message beyond the current run.
/// </summary>
public sealed class IconMigrationEvent : IEventMessage
{
    private readonly IconMigrationContext _context;

    /// <summary>
    /// Creates an event backed by the current migration state.
    /// </summary>
    public IconMigrationEvent(IconMigrationContext context)
    {
        _context = Guard.NotNull(context);
    }

    /// <summary>
    /// Gets the provider instances to execute before the registered HTML fields.
    /// Event consumers create instances directly; run-specific resources come from the context.
    /// </summary>
    public List<IIconMigrationProvider> Providers { get; } = [];

    /// <summary>
    /// Stores an HTML field expression for subsequent execution by the migrator.
    /// </summary>
    public void RegisterHtml<TEntity>(Expression<Func<TEntity, string>> field)
        where TEntity : BaseEntity
    {
        Guard.NotNull(field);
        // Registration only: do not compile the expression or access the database in an event handler.
        _context.HtmlFields.Add(field);
    }
}

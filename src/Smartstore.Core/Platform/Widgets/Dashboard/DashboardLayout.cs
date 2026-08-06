#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Identifies the audience to which a dashboard layout applies.
/// </summary>
public enum DashboardLayoutScope
{
    /// <summary>
    /// The layout is the shared default for all customers.
    /// </summary>
    Global,

    /// <summary>
    /// The layout is an override for one customer.
    /// </summary>
    User
}

/// <summary>
/// Describes a complete dashboard layout.
/// </summary>
public sealed class DashboardLayout
{
    /// <summary>
    /// Initializes a new dashboard layout.
    /// </summary>
    /// <param name="id">The stable and CSS-safe dashboard identifier.</param>
    public DashboardLayout(string id)
    {
        Id = id.SanitizeHtmlId();
    }

    /// <summary>
    /// Gets the stable and CSS-safe dashboard identifier.
    /// </summary>
    /// <remarks>The corresponding grid element is addressed as <c>#{Id}-grid</c>.</remarks>
    public string Id { get; }

    /// <summary>
    /// Gets the audience to which the layout applies.
    /// </summary>
    public DashboardLayoutScope Scope { get; init; }

    /// <summary>
    /// Gets the schema version of the layout model.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Gets the revision used to distinguish updates within the same layout schema version.
    /// </summary>
    public int Revision { get; init; }

    /// <summary>
    /// Gets the customer identifier for a user-scoped layout, or zero for a global layout.
    /// </summary>
    public int CustomerId { get; init; }

    /// <summary>
    /// Gets the number of columns in the dashboard grid.
    /// </summary>
    public int ColumnCount { get; init; } = 12;

    /// <summary>
    /// Gets the CSS <c>grid-template-columns</c> value of the dashboard grid.
    /// </summary>
    public string GridTemplateColumns { get; init; } = "repeat(12, minmax(0, 1fr))";

    /// <summary>
    /// Gets the CSS column gap value of the dashboard grid.
    /// </summary>
    public string ColumnGap { get; init; } = "1rem";

    /// <summary>
    /// Gets the CSS row gap value of the dashboard grid.
    /// </summary>
    public string RowGap { get; init; } = "1rem";

    /// <summary>
    /// Gets the CSS <c>grid-auto-rows</c> value of the dashboard grid.
    /// </summary>
    public string GridAutoRows { get; init; } = "auto";

    /// <summary>
    /// Gets the widget instances contained in the dashboard layout.
    /// </summary>
    public IReadOnlyList<DashboardWidgetInstance> Widgets { get; init; } = [];
}

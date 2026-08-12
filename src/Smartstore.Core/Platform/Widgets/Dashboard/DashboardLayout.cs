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
    /// Gets or sets the audience to which the layout applies.
    /// </summary>
    public DashboardLayoutScope Scope { get; set; }

    /// <summary>
    /// Gets or sets the schema version of the layout model.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the revision used to distinguish updates within the same layout schema version.
    /// </summary>
    public int Revision { get; set; }

    /// <summary>
    /// Gets or sets the customer identifier for a user-scoped layout, or zero for a global layout.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// Gets or sets the number of columns in the dashboard grid.
    /// </summary>
    public int ColumnCount { get; set; } = 12;

    /// <summary>
    /// Gets or sets the CSS <c>grid-template-columns</c> value of the dashboard grid.
    /// </summary>
    public string GridTemplateColumns { get; set; } = "repeat(12, minmax(0, 1fr))";

    /// <summary>
    /// Gets or sets the CSS column gap value of the dashboard grid.
    /// </summary>
    public string ColumnGap { get; set; } = "1rem";

    /// <summary>
    /// Gets or sets the CSS row gap value of the dashboard grid.
    /// </summary>
    public string RowGap { get; set; } = "1rem";

    /// <summary>
    /// Gets or sets the CSS <c>grid-auto-rows</c> value of the dashboard grid.
    /// </summary>
    public string GridAutoRows { get; set; } = "auto";

    /// <summary>
    /// Gets or sets the widget instances contained in the dashboard layout.
    /// </summary>
    public IList<DashboardWidgetInstance> Widgets { get; set; } = [];

    /// <summary>
    /// Builds the dashboard-qualified HTML identifier of a widget instance.
    /// </summary>
    /// <param name="widgetId">The dashboard-local widget instance identifier.</param>
    /// <returns>The HTML identifier using the dashboard and widget identifiers as BEM-style segments.</returns>
    public string BuildWidgetId(string widgetId)
    {
        Guard.NotEmpty(widgetId);

        return $"{Id}__{widgetId.SanitizeHtmlId()}";
    }
}

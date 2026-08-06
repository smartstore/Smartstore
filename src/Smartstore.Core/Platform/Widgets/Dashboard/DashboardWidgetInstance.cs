#nullable enable

using System.Text.Json.Nodes;

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Describes one concrete widget instance within a dashboard layout.
/// </summary>
public sealed record DashboardWidgetInstance
{
    /// <summary>
    /// Initializes a new dashboard widget instance.
    /// </summary>
    /// <param name="id">The dashboard-unique and CSS-safe instance identifier.</param>
    /// <param name="widgetSystemName">The system name of the widget type represented by this instance.</param>
    public DashboardWidgetInstance(string id, string widgetSystemName)
    {
        Guard.NotEmpty(widgetSystemName);

        Id = id.SanitizeHtmlId();
        WidgetSystemName = widgetSystemName;
    }

    /// <summary>
    /// Gets the dashboard-unique and CSS-safe instance identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Gets the system name of the widget type represented by this instance.
    /// </summary>
    public string WidgetSystemName { get; }

    /// <summary>
    /// Gets the explicit ordering priority within the dashboard layout.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Gets the schema version of <see cref="Settings"/>.
    /// </summary>
    public int SettingsVersion { get; init; } = 1;

    /// <summary>
    /// Gets the widget-specific settings payload.
    /// </summary>
    public JsonObject Settings { get; init; } = new();

    /// <summary>
    /// Gets the restrictions applied by this concrete layout instance.
    /// </summary>
    public DashboardWidgetPolicy Policy { get; init; } = new();

    /// <summary>
    /// Gets the responsive grid positions of the widget instance.
    /// </summary>
    public IReadOnlyCollection<DashboardWidgetPosition> Positions { get; init; } = [];

    /// <summary>
    /// Gets the most specific grid position that applies to a viewport width.
    /// </summary>
    /// <param name="viewportWidth">The viewport width in pixels.</param>
    /// <returns>The applicable responsive grid position.</returns>
    /// <exception cref="InvalidOperationException">No position applies to the specified viewport width.</exception>
    public DashboardWidgetPosition GetPosition(int viewportWidth = 0)
    {
        return Positions
            .Where(x => x.MinViewportWidth <= viewportWidth)
            .OrderByDescending(x => x.MinViewportWidth)
            .First();
    }
}

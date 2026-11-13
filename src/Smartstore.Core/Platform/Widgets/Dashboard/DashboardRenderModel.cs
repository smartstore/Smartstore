#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Provides the fully resolved dashboard data required by a renderer.
/// </summary>
public sealed class DashboardRenderModel
{
    /// <summary>
    /// Gets or sets the layout state resolved by the dashboard service, when available.
    /// </summary>
    public DashboardLayoutState? State { get; set; }

    /// <summary>
    /// Gets the effective dashboard layout.
    /// </summary>
    public required DashboardLayout Layout { get; init; }

    /// <summary>
    /// Gets the request-wide context shared by all rendered widgets.
    /// </summary>
    public required DashboardWidgetContext Context { get; init; }

    /// <summary>
    /// Gets the resolved and renderable widget items.
    /// </summary>
    public required IReadOnlyList<DashboardRenderItem> Widgets { get; init; }
}

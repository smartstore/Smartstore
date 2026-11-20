#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Provides the resolved metadata, policy and renderable widget for one dashboard instance.
/// </summary>
public sealed class DashboardRenderItem
{
    /// <summary>
    /// Gets the concrete widget instance from the effective layout.
    /// </summary>
    public required DashboardWidgetInstance Instance { get; init; }

    /// <summary>
    /// Gets the metadata declared by the widget type.
    /// </summary>
    public required DashboardWidgetDescriptor Descriptor { get; init; }

    /// <summary>
    /// Gets the effective policy after widget and layout restrictions have been combined.
    /// </summary>
    public required DashboardWidgetPolicy Policy { get; init; }

    /// <summary>
    /// Gets the renderable widget created for the dashboard instance.
    /// </summary>
    public required Widget Widget { get; init; }
}

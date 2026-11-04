#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Contains a detached layout, its selected layer and its source.
/// </summary>
public sealed class DashboardLayoutState
{
    /// <summary>
    /// Gets the effective layout or editable copy.
    /// </summary>
    public required DashboardLayout Layout { get; init; }
    /// <summary>
    /// Gets the selected layer.
    /// </summary>
    public DashboardLayoutLayer Layer { get; init; }
    /// <summary>
    /// Gets the layer that actually supplied the layout.
    /// </summary>
    public DashboardLayoutLayer Source { get; init; }
}

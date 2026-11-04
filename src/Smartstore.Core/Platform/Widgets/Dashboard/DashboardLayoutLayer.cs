#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Identifies a selectable dashboard layout layer.
/// </summary>
public enum DashboardLayoutLayer
{
    /// <summary>
    /// The immutable provider default.
    /// </summary>
    System,
    /// <summary>
    /// The shared customization, falling back to the provider default.
    /// </summary>
    Global,
    /// <summary>
    /// The personal customization, falling back to the shared layout.
    /// </summary>
    User
}

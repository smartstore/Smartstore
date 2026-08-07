#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Builds dashboard-specific CSS for resolved widget placements.
/// </summary>
public interface IDashboardCssBuilder
{
    /// <summary>
    /// Builds the responsive placement stylesheet for a resolved dashboard.
    /// </summary>
    /// <param name="model">The resolved dashboard render model.</param>
    /// <returns>The dashboard-specific CSS stylesheet.</returns>
    string Build(DashboardRenderModel model);
}

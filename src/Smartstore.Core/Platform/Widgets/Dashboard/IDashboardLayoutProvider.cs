#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Supplies the system-default layout for one dashboard.
/// </summary>
public interface IDashboardLayoutProvider
{
    /// <summary>
    /// Gets the stable identifier of the dashboard handled by this provider.
    /// </summary>
    string DashboardId { get; }

    /// <summary>
    /// Creates the system-default layout of the dashboard.
    /// </summary>
    /// <returns>A fresh system-default dashboard layout.</returns>
    DashboardLayout GetDefaultLayout();
}

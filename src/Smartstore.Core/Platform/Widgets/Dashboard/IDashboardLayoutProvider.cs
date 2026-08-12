#nullable enable

namespace Smartstore.Core.Widgets.Dashboard;

/// <summary>
/// Supplies the system-default layout for one dashboard.
/// </summary>
public interface IDashboardLayoutProvider
{
    /// <summary>
    /// Creates the system-default layout of the dashboard.
    /// </summary>
    /// <returns>A fresh system-default dashboard layout.</returns>
    DashboardLayout GetDefaultLayout();
}

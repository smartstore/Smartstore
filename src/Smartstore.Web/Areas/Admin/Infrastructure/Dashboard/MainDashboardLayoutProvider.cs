#nullable enable

using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Supplies the system-default layout of the main admin dashboard.
/// </summary>
public sealed class MainDashboardLayoutProvider : IDashboardLayoutProvider
{
    /// <summary>
    /// Identifies the main admin dashboard.
    /// </summary>
    public const string Id = "admin-dashboard";

    public string DashboardId => Id;

    public DashboardLayout GetDefaultLayout()
    {
        return new DashboardLayout(Id)
        {
            Scope = DashboardLayoutScope.Global,
            ColumnCount = 12,
            GridTemplateColumns = "repeat(10, 1fr) repeat(2, 130px)",
            ColumnGap = "1rem",
            RowGap = "1rem",
            GridAutoRows = "auto",
            Widgets =
            [
                new DashboardWidgetInstance("latest-orders", LatestOrdersDashboardWidget.SystemName)
                {
                    Order = 600,
                    Positions =
                    [
                        new DashboardWidgetPosition
                        {
                            MinViewportWidth = 0,
                            Column = 0,
                            Row = 5,
                            Size = new DashboardWidgetSize(12, 1)
                        },
                        new DashboardWidgetPosition
                        {
                            MinViewportWidth = 768,
                            Column = 0,
                            Row = 4,
                            Size = new DashboardWidgetSize(12, 1)
                        },
                        new DashboardWidgetPosition
                        {
                            MinViewportWidth = 992,
                            Column = 0,
                            Row = 4,
                            Size = new DashboardWidgetSize(10, 1)
                        },
                        new DashboardWidgetPosition
                        {
                            MinViewportWidth = 1600,
                            Column = 0,
                            Row = 3,
                            Size = new DashboardWidgetSize(6, 1)
                        }
                    ]
                }
            ]
        };
    }
}

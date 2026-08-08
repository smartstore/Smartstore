#nullable enable

using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Supplies the system-default layout of the main admin dashboard.
/// </summary>
public sealed class AdminDashboardLayoutProvider : IDashboardLayoutProvider
{
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
                        // mobile/base layout
                        At(0, 0, 5, 12),
                        // md layout
                        At(768, 0, 4, 12),
                        // lg layout
                        At(992, 0, 4, 10),
                        // special 1600px layout
                        At(1600, 0, 3, 6)
                    ]
                }
            ]
        };
    }

    private static DashboardWidgetPosition At(
        int minViewportWidth,
        int column,
        int row,
        int columnSpan,
        int? rowSpan = null)
    {
        return new DashboardWidgetPosition
        {
            MinViewportWidth = minViewportWidth,
            Column = column,
            Row = row,
            Size = new DashboardWidgetSize(columnSpan, rowSpan)
        };
    }
}

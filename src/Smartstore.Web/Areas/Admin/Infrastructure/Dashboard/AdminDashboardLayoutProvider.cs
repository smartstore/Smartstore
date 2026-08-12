#nullable enable

using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Supplies the system-default layout of the main admin dashboard.
/// </summary>
[SystemName(Id)]
public sealed class AdminDashboardLayoutProvider : IDashboardLayoutProvider
{
    public const string Id = "admin-dashboard";

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
                new DashboardWidgetInstance("incomplete-orders", IncompleteOrdersDashboardWidget.SystemName)
                {
                    Order = 100,
                    Positions =
                    [
                        At(0, 0, 0, 12),
                        At(992, 0, 0, 10)
                    ]
                },
                new DashboardWidgetInstance("orders", OrdersDashboardWidget.SystemName)
                {
                    Order = 200,
                    Positions =
                    [
                        At(0, 0, 1, 12),
                        At(992, 0, 1, 10),
                        At(1600, 0, 1, 7)
                    ]
                },
                new DashboardWidgetInstance("bestsellers", BestsellersDashboardWidget.SystemName)
                {
                    Order = 300,
                    Positions =
                    [
                        At(0, 0, 2, 12),
                        At(768, 0, 2, 6),
                        At(992, 0, 2, 4),
                        At(1600, 7, 1, 3)
                    ]
                },
                new DashboardWidgetInstance("top-customers", TopCustomersDashboardWidget.SystemName)
                {
                    Order = 400,
                    Positions =
                    [
                        At(0, 0, 3, 12),
                        At(768, 6, 2, 6),
                        At(992, 4, 2, 6),
                        At(1600, 0, 2, 3)
                    ]
                },
                new DashboardWidgetInstance("registered-customers", RegisteredCustomersDashboardWidget.SystemName)
                {
                    Order = 500,
                    Positions =
                    [
                        At(0, 0, 4, 12),
                        At(768, 0, 3, 12),
                        At(992, 0, 3, 10),
                        At(1600, 3, 2, 7)
                    ]
                },
                new DashboardWidgetInstance("latest-orders", LatestOrdersDashboardWidget.SystemName)
                {
                    Order = 600,
                    Positions =
                    [
                        At(0, 0, 5, 12),
                        At(768, 0, 4, 12),
                        At(992, 0, 4, 10),
                        At(1600, 0, 3, 6)
                    ]
                },
                new DashboardWidgetInstance("store-report", StoreReportDashboardWidget.SystemName)
                {
                    Order = 700,
                    Positions =
                    [
                        At(0, 0, 6, 12),
                        At(768, 0, 5, 12),
                        At(992, 0, 5, 10),
                        At(1600, 6, 3, 4)
                    ]
                },
                new DashboardWidgetInstance("news-feed", NewsFeedDashboardWidget.SystemName)
                {
                    Order = 800,
                    Positions =
                    [
                        At(0, 0, 7, 12),
                        At(768, 0, 6, 12),
                        At(992, 10, 0, 2, 6),
                        At(1600, 10, 0, 2, 4)
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
        int? rowSpan = 1)
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

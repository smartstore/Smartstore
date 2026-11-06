#nullable enable

using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Supplies the system-default layout of the main admin dashboard.
/// </summary>
[SystemName(Id)]
public class AdminDashboardLayoutProvider : IDashboardLayoutProvider
{
    public const string Id = "admin-dashboard";

    public virtual DashboardLayout GetDefaultLayout()
    {
        return new DashboardLayout(Id)
        {
            Scope = DashboardLayoutScope.Global,
            ColumnCount = 12,
            GridTemplateColumns = "repeat(10, minmax(0, 1fr)) repeat(2, 130px)",
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
                        At(DashboardBreakpoint.Xs, 0, 0, 12),
                        At(DashboardBreakpoint.Lg, 0, 0, 10)
                    ]
                },
                new DashboardWidgetInstance("orders", OrdersDashboardWidget.SystemName)
                {
                    Order = 200,
                    Positions =
                    [
                        At(DashboardBreakpoint.Xs, 0, 1, 12),
                        At(DashboardBreakpoint.Lg, 0, 1, 10),
                        At(DashboardBreakpoint.Xxl, 0, 1, 7)
                    ]
                },
                new DashboardWidgetInstance("bestsellers", BestsellersDashboardWidget.SystemName)
                {
                    Order = 300,
                    Positions =
                    [
                        At(DashboardBreakpoint.Xs, 0, 2, 12),
                        At(DashboardBreakpoint.Md, 0, 2, 6),
                        At(DashboardBreakpoint.Lg, 0, 2, 4),
                        At(DashboardBreakpoint.Xxl, 7, 1, 3)
                    ]
                },
                new DashboardWidgetInstance("top-customers", TopCustomersDashboardWidget.SystemName)
                {
                    Order = 400,
                    Positions =
                    [
                        At(DashboardBreakpoint.Xs, 0, 3, 12),
                        At(DashboardBreakpoint.Md, 6, 2, 6),
                        At(DashboardBreakpoint.Lg, 4, 2, 6),
                        At(DashboardBreakpoint.Xxl, 0, 2, 3)
                    ]
                },
                new DashboardWidgetInstance("registered-customers", RegisteredCustomersDashboardWidget.SystemName)
                {
                    Order = 500,
                    Positions =
                    [
                        At(DashboardBreakpoint.Xs, 0, 4, 12),
                        At(DashboardBreakpoint.Md, 0, 3, 12),
                        At(DashboardBreakpoint.Lg, 0, 3, 10),
                        At(DashboardBreakpoint.Xxl, 3, 2, 7)
                    ]
                },
                new DashboardWidgetInstance("latest-orders", LatestOrdersDashboardWidget.SystemName)
                {
                    Order = 600,
                    Positions =
                    [
                        At(DashboardBreakpoint.Xs, 0, 5, 12),
                        At(DashboardBreakpoint.Md, 0, 4, 12),
                        At(DashboardBreakpoint.Lg, 0, 4, 10),
                        At(DashboardBreakpoint.Xxl, 0, 3, 6)
                    ]
                },
                new DashboardWidgetInstance("store-report", StoreReportDashboardWidget.SystemName)
                {
                    Order = 700,
                    Positions =
                    [
                        At(DashboardBreakpoint.Xs, 0, 6, 12),
                        At(DashboardBreakpoint.Md, 0, 5, 12),
                        At(DashboardBreakpoint.Lg, 0, 5, 10),
                        At(DashboardBreakpoint.Xxl, 6, 3, 4)
                    ]
                },
                new DashboardWidgetInstance("news-feed", NewsFeedDashboardWidget.SystemName)
                {
                    Order = 800,
                    Positions =
                    [
                        At(DashboardBreakpoint.Xs, 0, 7, 12),
                        At(DashboardBreakpoint.Md, 0, 6, 12),
                        At(DashboardBreakpoint.Lg, 10, 0, 2, 6),
                        At(DashboardBreakpoint.Xxl, 10, 0, 2, 4)
                    ]
                }
            ]
        };
    }

    private static DashboardWidgetPosition At(
        DashboardBreakpoint breakpoint,
        int column,
        int row,
        int columnSpan,
        int? rowSpan = 1)
    {
        return new DashboardWidgetPosition
        {
            Breakpoint = breakpoint,
            Column = column,
            Row = row,
            Size = new DashboardWidgetSize(columnSpan, rowSpan)
        };
    }
}

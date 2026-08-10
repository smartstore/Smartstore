#nullable enable

using Smartstore.Admin.Components;
using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders incomplete orders.
/// </summary>
public sealed class IncompleteOrdersDashboardWidget : DashboardViewComponentWidget<DashboardIncompleteOrdersViewComponent>
{
    /// <summary>
    /// Identifies the incomplete orders dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.IncompleteOrders";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, "Admin.SalesReport.Incomplete")
    {
        DescriptionResKey = "Admin.SalesReport.Incomplete",
        CategoryResKey = "Admin.Dashboard.StoreStatistics",
        IconName = "hourglass-split",
        CssClass = "report-incomplete-orders",
        Order = 100,
        DefaultSize = new DashboardWidgetSize(10, 1),
        MinSize = new DashboardWidgetSize(4, 1),
        MaxSize = new DashboardWidgetSize(12, 1),
        AllowedSizes =
        [
            new DashboardWidgetSize(10, 1),
            new DashboardWidgetSize(12, 1)
        ]
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}

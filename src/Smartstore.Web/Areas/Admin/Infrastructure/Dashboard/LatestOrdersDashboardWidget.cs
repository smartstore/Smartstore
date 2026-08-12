#nullable enable

using Smartstore.Admin.Components;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the latest orders report.
/// </summary>
[SystemName(SystemName)]
public sealed class LatestOrdersDashboardWidget : DashboardViewComponentWidget<DashboardLatestOrdersViewComponent>
{
    public const string SystemName = "Smartstore.Admin.Dashboard.LatestOrders";

    /// <summary>
    /// Contains the immutable metadata and capabilities of the latest orders widget.
    /// </summary>
    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, "Admin.SalesReport.LatestOrders")
    {
        DescriptionResKey = "Admin.SalesReport.LatestOrders",
        CategoryResKey = "Admin.Dashboard.StoreStatistics",
        IconName = "card-checklist",
        CssClass = "report-latest-orders",
        Order = 600,
        AllowMultipleInstances = false,
        SupportsRefresh = false,
        DefaultSize = new DashboardWidgetSize(6, 1),
        MinSize = new DashboardWidgetSize(4, 1),
        MaxSize = new DashboardWidgetSize(12, 1),
        AllowedSizes =
        [
            new DashboardWidgetSize(6, 1),
            new DashboardWidgetSize(10, 1),
            new DashboardWidgetSize(12, 1)
        ]
    };

    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}

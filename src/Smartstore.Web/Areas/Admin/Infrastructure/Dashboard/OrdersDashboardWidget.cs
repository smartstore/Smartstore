#nullable enable

using Smartstore.Admin.Components;
using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the orders report.
/// </summary>
public sealed class OrdersDashboardWidget : DashboardViewComponentWidget<DashboardOrdersViewComponent>
{
    /// <summary>
    /// Identifies the orders dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.Orders";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, "Admin.Orders")
    {
        DescriptionResKey = "Admin.Orders",
        CategoryResKey = "Admin.Dashboard.StoreStatistics",
        IconName = "graph-up",
        CssClass = "report-orders",
        Order = 200,
        DefaultSize = new DashboardWidgetSize(7, 1),
        MinSize = new DashboardWidgetSize(4, 1),
        MaxSize = new DashboardWidgetSize(12, 1),
        AllowedSizes =
        [
            new DashboardWidgetSize(7, 1),
            new DashboardWidgetSize(10, 1),
            new DashboardWidgetSize(12, 1)
        ],
        Policy = new DashboardWidgetPolicy
        {
            AllowConfigure = false,
            AllowRefresh = false
        }
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}

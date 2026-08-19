#nullable enable

using Smartstore.Admin.Components;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the orders report.
/// </summary>
[SystemName(SystemName)]
public sealed class OrdersDashboardWidget : DashboardViewComponentWidget<DashboardOrdersViewComponent>
{
    /// <summary>
    /// Identifies the orders dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.Orders";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.Orders"))
    {
        Description = ResolvableText.Resource("Admin.Orders"),
        Group = KnownDashboardWidgetGroups.Sales,
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
        ]
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}

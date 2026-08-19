#nullable enable

using Smartstore.Admin.Components;
using Smartstore.Core.Localization;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the bestsellers report.
/// </summary>
[SystemName(SystemName)]
public sealed class BestsellersDashboardWidget : DashboardViewComponentWidget<DashboardBestsellersViewComponent>
{
    /// <summary>
    /// Identifies the bestsellers dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.Bestsellers";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.SalesReport.BestSellers"))
    {
        Description = ResolvableText.Resource("Admin.SalesReport.BestSellers"),
        Group = KnownDashboardWidgetGroups.Sales,
        IconName = "trophy",
        CssClass = "report-bestsellers",
        Order = 300,
        DefaultSize = new DashboardWidgetSize(3, 1),
        MinSize = new DashboardWidgetSize(3, 1),
        MaxSize = new DashboardWidgetSize(12, 1),
        AllowedSizes =
        [
            new DashboardWidgetSize(3, 1),
            new DashboardWidgetSize(4, 1),
            new DashboardWidgetSize(6, 1),
            new DashboardWidgetSize(12, 1)
        ]
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}

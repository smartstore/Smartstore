#nullable enable

using Smartstore.Admin.Components;
using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the bestsellers report.
/// </summary>
public sealed class BestsellersDashboardWidget : DashboardViewComponentWidget<DashboardBestsellersViewComponent>
{
    /// <summary>
    /// Identifies the bestsellers dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.Bestsellers";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, "Admin.SalesReport.BestSellers")
    {
        DescriptionResKey = "Admin.SalesReport.BestSellers",
        CategoryResKey = "Admin.Dashboard.StoreStatistics",
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

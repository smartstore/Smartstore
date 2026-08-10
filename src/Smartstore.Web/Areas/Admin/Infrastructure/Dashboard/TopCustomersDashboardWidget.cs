#nullable enable

using Smartstore.Admin.Components;
using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the top customers report.
/// </summary>
public sealed class TopCustomersDashboardWidget : DashboardViewComponentWidget<DashboardTopCustomersViewComponent>
{
    /// <summary>
    /// Identifies the top customers dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.TopCustomers";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, "Admin.SalesReport.TopCustomers")
    {
        DescriptionResKey = "Admin.SalesReport.TopCustomers",
        CategoryResKey = "Admin.Dashboard.StoreStatistics",
        IconName = "people",
        CssClass = "report-top-customers",
        Order = 400,
        DefaultSize = new DashboardWidgetSize(3, 1),
        MinSize = new DashboardWidgetSize(3, 1),
        MaxSize = new DashboardWidgetSize(12, 1),
        AllowedSizes =
        [
            new DashboardWidgetSize(3, 1),
            new DashboardWidgetSize(6, 1),
            new DashboardWidgetSize(12, 1)
        ]
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}

#nullable enable

using Smartstore.Admin.Components;
using Smartstore.Core.Widgets.Dashboard;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders customer registrations.
/// </summary>
public sealed class RegisteredCustomersDashboardWidget : DashboardViewComponentWidget<DashboardRegisteredCustomersViewComponent>
{
    /// <summary>
    /// Identifies the registered customers dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.RegisteredCustomers";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, "Admin.Report.CustomerRegistrations")
    {
        DescriptionResKey = "Admin.Report.CustomerRegistrations",
        CategoryResKey = "Admin.Dashboard.StoreStatistics",
        IconName = "person-plus",
        CssClass = "report-customer-registrations",
        Order = 500,
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

using Smartstore.Admin.Components;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that ranks payment methods by order count and order total.
/// </summary>
[SystemName(SystemName)]
public sealed class PaymentMethodsDashboardWidget(IPermissionService permissions)
    : DashboardViewComponentWidget<DashboardPaymentMethodsViewComponent>(permissions, DashboardPaymentMethodsViewComponent.RequiredPermission)
{
    /// <summary>
    /// Identifies the payment methods dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.PaymentMethods";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.Dashboard.PaymentMethods"))
    {
        Description = ResolvableText.Resource("Admin.Dashboard.PaymentMethods.Description"),
        Group = KnownDashboardWidgetGroups.Sales,
        IconName = "creditcard",
        Order = 650,
        DefaultSize = new DashboardWidgetSize(4),
        MinSize = new DashboardWidgetSize(3),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}

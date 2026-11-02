using Smartstore.Admin.Components;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the widget showing recently recorded contact messages.
/// </summary>
[SystemName(SystemName)]
public sealed class ContactRequestsDashboardWidget(IPermissionService permissions)
    : DashboardViewComponentWidget<DashboardContactRequestsViewComponent>(permissions, DashboardContactRequestsViewComponent.RequiredPermission)
{
    /// <summary>
    /// Identifies the contact requests dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.ContactRequests";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.Dashboard.ContactRequests"))
    {
        Description = ResolvableText.Resource("Admin.Dashboard.ContactRequests.Description"),
        Group = KnownDashboardWidgetGroups.Customers,
        IconName = "mail",
        Order = 575,
        DefaultSize = new DashboardWidgetSize(6),
        MinSize = new DashboardWidgetSize(4),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}

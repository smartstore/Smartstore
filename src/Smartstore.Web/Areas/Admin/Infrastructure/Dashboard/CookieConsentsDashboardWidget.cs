using Smartstore.Admin.Components;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the widget reporting current saved cookie consent decisions.
/// </summary>
[SystemName(SystemName)]
public sealed class CookieConsentsDashboardWidget(IPermissionService permissions)
    : DashboardViewComponentWidget<DashboardCookieConsentsViewComponent>(permissions, DashboardCookieConsentsViewComponent.RequiredPermission)
{
    /// <summary>
    /// Identifies the cookie consent dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.CookieConsents";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.Dashboard.CookieConsents"))
    {
        Description = ResolvableText.Resource("Admin.Dashboard.CookieConsents.Description"),
        Group = KnownDashboardWidgetGroups.Customers,
        IconName = "cookie",
        Order = 550,
        DefaultSize = new DashboardWidgetSize(6),
        MinSize = new DashboardWidgetSize(4),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;
}

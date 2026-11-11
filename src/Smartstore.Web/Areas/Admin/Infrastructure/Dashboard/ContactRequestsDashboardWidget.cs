using FluentValidation;
using Smartstore.Admin.Components;
using HomeController = Smartstore.Admin.Controllers.HomeController;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;
using Smartstore.Http;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the widget showing recently recorded contact messages.
/// </summary>
[SystemName(SystemName)]
public sealed class ContactRequestsDashboardWidget(
    IPermissionService permissions,
    IValidator<ContactRequestsDashboardWidgetSettings> validator) : DashboardViewComponentWidget<DashboardContactRequestsViewComponent, ContactRequestsDashboardWidgetSettings>(
        permissions,
        DashboardContactRequestsViewComponent.RequiredPermission,
        validator,
        "Components/DashboardContactRequests/Configure")
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
        Policy = new() { AllowConfigure = true },
        DefaultSize = new DashboardWidgetSize(6, 1),
        MinSize = new DashboardWidgetSize(4, 1),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;

    /// <inheritdoc />
    public override RouteInfo GetConfigurationRoute()
        => new(nameof(HomeController.DashboardConfigure), "Home", new { area = "Admin", widgetSystemName = SystemName });

    /// <inheritdoc />
    protected override object GetWidgetArguments(DashboardWidgetContext context, DashboardWidgetInstance instance, ContactRequestsDashboardWidgetSettings settings)
        => new { count = settings.Count };
}

/// <summary>Contains the settings of the ContactRequests dashboard widget.</summary>
[LocalizedDisplay("Admin.Dashboard.Widget.")]
public class ContactRequestsDashboardWidgetSettings
{
    /// <summary>Gets or sets the maximum number of items to display.</summary>
    [LocalizedDisplay("*Count")]
    public int Count { get; set; } = 5;
}

/// <summary>Validates settings of the ContactRequests dashboard widget.</summary>
public class ContactRequestsDashboardWidgetValidator : AbstractValidator<ContactRequestsDashboardWidgetSettings>
{
    public ContactRequestsDashboardWidgetValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50)
            .WithMessage("The number of displayed items must be between 1 and 50.");
    }
}

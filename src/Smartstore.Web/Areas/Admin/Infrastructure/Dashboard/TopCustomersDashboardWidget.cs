#nullable enable

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
/// Describes and creates the dashboard widget that renders the top customers report.
/// </summary>
[SystemName(SystemName)]
public sealed class TopCustomersDashboardWidget(
    IPermissionService permissions,
    IValidator<TopCustomersDashboardWidgetSettings> validator) : DashboardViewComponentWidget<DashboardTopCustomersViewComponent, TopCustomersDashboardWidgetSettings>(
        permissions,
        DashboardTopCustomersViewComponent.RequiredPermission,
        validator,
        "Components/DashboardTopCustomers/Configure")
{
    /// <summary>
    /// Identifies the top customers dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.TopCustomers";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.SalesReport.TopCustomers"))
    {
        Group = KnownDashboardWidgetGroups.Customers,
        IconName = "user-group",
        CssClass = "report-top-customers",
        Order = 400,
        Policy = new() { AllowConfigure = true },
        DefaultSize = new DashboardWidgetSize(4, 1),
        MinSize = new DashboardWidgetSize(3, 1),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;

    /// <inheritdoc />
    public override RouteInfo GetConfigurationRoute()
        => new(nameof(HomeController.DashboardConfigure), "Home", new { area = "Admin", widgetSystemName = SystemName });

    /// <inheritdoc />
    protected override object GetWidgetArguments(DashboardWidgetContext context, DashboardWidgetInstance instance, TopCustomersDashboardWidgetSettings settings)
        => new { count = settings.Count };
}

/// <summary>Contains the settings of the TopCustomers dashboard widget.</summary>
[LocalizedDisplay("Admin.Dashboard.Widget.")]
public class TopCustomersDashboardWidgetSettings
{
    /// <summary>Gets or sets the maximum number of items to display.</summary>
    [LocalizedDisplay("*Count")]
    public int Count { get; set; } = 7;
}

/// <summary>Validates settings of the TopCustomers dashboard widget.</summary>
public class TopCustomersDashboardWidgetValidator : AbstractValidator<TopCustomersDashboardWidgetSettings>
{
    public TopCustomersDashboardWidgetValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50)
            .WithMessage("The number of displayed items must be between 1 and 50.");
    }
}

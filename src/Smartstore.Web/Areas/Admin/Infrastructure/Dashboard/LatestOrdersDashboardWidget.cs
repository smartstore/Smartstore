#nullable enable

using FluentValidation;
using Smartstore.Admin.Components;
using HomeController = Smartstore.Admin.Controllers.HomeController;
using Smartstore.Admin.Models.Dashboard;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the latest orders report.
/// </summary>
[SystemName(SystemName)]
public sealed class LatestOrdersDashboardWidget(
    IPermissionService permissions,
    IValidator<LatestOrdersDashboardWidgetSettings> validator) : DashboardViewComponentWidget<DashboardLatestOrdersViewComponent, LatestOrdersDashboardWidgetSettings>(
        permissions,
        DashboardLatestOrdersViewComponent.RequiredPermission,
        validator,
        "Components/Dashboard/ConfigureCount")
{
    public const string SystemName = "Smartstore.Admin.Dashboard.LatestOrders";

    /// <summary>
    /// Contains the immutable metadata and capabilities of the latest orders widget.
    /// </summary>
    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.SalesReport.LatestOrders"))
    {
        Group = KnownDashboardWidgetGroups.Sales,
        IconName = "clipboard",
        CssClass = "report-latest-orders",
        Order = 600,
        AllowMultipleInstances = false,
        SupportsRefresh = false,
        Policy = new() { AllowConfigure = true },
        DefaultSize = new DashboardWidgetSize(6, 1),
        MinSize = new DashboardWidgetSize(5, 1),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;

    /// <inheritdoc />
    public override RouteInfo GetConfigurationRoute()
        => new(nameof(HomeController.DashboardConfigure), "Home", new { area = "Admin", widgetSystemName = SystemName });

    /// <inheritdoc />
    protected override object GetWidgetArguments(DashboardWidgetContext context, DashboardWidgetInstance instance, LatestOrdersDashboardWidgetSettings settings)
        => new { count = settings.Count };

    /// <inheritdoc />
    protected override object CreateConfigurationModel(DashboardWidgetInstance instance, LatestOrdersDashboardWidgetSettings settings)
        => new DashboardWidgetCountConfigurationModel
        {
            Count = instance.Settings.ContainsKey("count") ? settings.Count : null,
            DefaultCount = 7
        };
}

/// <summary>Contains the settings of the LatestOrders dashboard widget.</summary>
public class LatestOrdersDashboardWidgetSettings
{
    /// <summary>Gets or sets the maximum number of items to display.</summary>
    public int Count { get; set; } = 7;
}

/// <summary>Validates settings of the LatestOrders dashboard widget.</summary>
public class LatestOrdersDashboardWidgetValidator : AbstractValidator<LatestOrdersDashboardWidgetSettings>
{
    public LatestOrdersDashboardWidgetValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50)
            .WithMessage("The number of latest orders must be between 1 and 50.");
    }
}

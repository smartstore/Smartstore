#nullable enable

using FluentValidation;
using Smartstore.Admin.Components;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that renders the bestsellers report.
/// </summary>
[SystemName(SystemName)]
public sealed class BestsellersDashboardWidget(
    IPermissionService permissions,
    IValidator<BestsellersDashboardWidgetSettings> validator) : DashboardViewComponentWidget<DashboardBestsellersViewComponent, BestsellersDashboardWidgetSettings>(
        permissions,
        DashboardBestsellersViewComponent.RequiredPermission,
        validator)
{
    /// <summary>
    /// Identifies the bestsellers dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.Bestsellers";

    private static readonly DashboardWidgetDescriptor _descriptor = new(SystemName, ResolvableText.Resource("Admin.SalesReport.BestSellers"))
    {
        Group = KnownDashboardWidgetGroups.Sales,
        IconName = "trophy",
        CssClass = "report-bestsellers",
        Order = 300,
        Policy = new() { AllowConfigure = true },
        DefaultSize = new DashboardWidgetSize(4, 1),
        MinSize = new DashboardWidgetSize(3, 1),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;

    /// <inheritdoc />
    public override Widget CreateConfigurationWidget(DashboardWidgetContext context, DashboardWidgetInstance instance)
        => new PartialViewWidget("Components/DashboardBestsellers/Configure", LoadSettings(instance.Settings));

    /// <inheritdoc />
    protected override object GetWidgetArguments(DashboardWidgetContext context, DashboardWidgetInstance instance, BestsellersDashboardWidgetSettings settings)
        => new { count = settings.Count };
}

/// <summary>Contains the settings of the Bestsellers dashboard widget.</summary>
[LocalizedDisplay("Admin.Dashboard.Widget.")]
public class BestsellersDashboardWidgetSettings
{
    /// <summary>Gets or sets the maximum number of items to display.</summary>
    [LocalizedDisplay("*Count")]
    public int Count { get; set; } = 7;
}

/// <summary>Validates settings of the Bestsellers dashboard widget.</summary>
public class BestsellersDashboardWidgetValidator : AbstractValidator<BestsellersDashboardWidgetSettings>
{
    public BestsellersDashboardWidgetValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50)
            .WithMessage("The number of displayed items must be between 1 and 50.");
    }
}

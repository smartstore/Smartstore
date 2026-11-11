using System.Text.Json.Nodes;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Components;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Web;
using Smartstore.Core.Widgets;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Modelling;
using Smartstore.Web.Models;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that ranks payment methods by order count and order total.
/// </summary>
[SystemName(SystemName)]
public sealed class PaymentMethodsDashboardWidget(
    IPermissionService permissions,
    IValidator<PaymentMethodsDashboardWidgetSettings> validator,
    IPaymentService paymentService,
    ModuleManager moduleManager,
    IViewDataAccessor viewDataAccessor) : DashboardViewComponentWidget<DashboardPaymentMethodsViewComponent, PaymentMethodsDashboardWidgetSettings>(
        permissions,
        DashboardPaymentMethodsViewComponent.RequiredPermission,
        validator)
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
        Policy = new() { AllowConfigure = true },
        DefaultSize = new DashboardWidgetSize(4, 1),
        MinSize = new DashboardWidgetSize(3, 1),
        MaxSize = new DashboardWidgetSize(12)
    };

    /// <inheritdoc />
    public override DashboardWidgetDescriptor Descriptor => _descriptor;

    /// <inheritdoc />
    public override JsonObject CreateDefaultSettings()
        => new() { ["count"] = 10 };

    /// <inheritdoc />
    protected override object GetWidgetArguments(DashboardWidgetContext context, DashboardWidgetInstance instance, PaymentMethodsDashboardWidgetSettings settings)
        => new
        {
            fees = settings.Fees,
            count = settings.Count,
            refresh = context.Refresh
        };

    /// <inheritdoc />
    public override async Task<Widget> CreateConfigurationWidgetAsync(DashboardWidgetContext context, DashboardWidgetInstance instance)
    {
        var providers = await paymentService.LoadAllPaymentProvidersAsync(onlyEnabled: true);
        viewDataAccessor.ViewData["PaymentProviders"] = providers
            .Select(x => new SelectListItem
            {
                Value = x.Metadata.SystemName,
                Text = moduleManager.GetLocalizedFriendlyName(x.Metadata)
            })
            .OrderBy(x => x.Text, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(x => x.Value, StringComparer.Ordinal)
            .ToArray();

        return new PartialViewWidget("Components/DashboardPaymentMethods/Configure", LoadSettings(instance.Settings));
    }
}

/// <summary>Contains the settings of the PaymentMethods dashboard widget.</summary>
[LocalizedDisplay("Admin.Dashboard.PaymentMethods.")]
public class PaymentMethodsDashboardWidgetSettings
{
    /// <summary>Gets or sets the maximum number of items to display.</summary>
    [LocalizedDisplay("Admin.Dashboard.Widget.Count")]
    public int Count { get; set; } = 10;

    /// <summary>Gets or sets the fee amount and unit for each payment provider.</summary>
    public Dictionary<string, Amount> Fees { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>Validates settings of the PaymentMethods dashboard widget.</summary>
public class PaymentMethodsDashboardWidgetValidator : AbstractValidator<PaymentMethodsDashboardWidgetSettings>
{
    public PaymentMethodsDashboardWidgetValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50)
            .WithMessage("The number of payment methods must be between 1 and 50.");

        RuleFor(x => x.Fees).NotNull();
        RuleFor(x => x.Fees).Custom((fees, context) =>
        {
            if (fees == null)
            {
                return;
            }

            foreach (var fee in fees)
            {
                var field = $"Fees[{fee.Key}]";
                if (!fee.Key.HasValue() || fee.Value == null ||
                    fee.Value.Value < 0 || fee.Value.Value > (fee.Value.IsPercentage() ? 100 : 1000000))
                {
                    context.AddFailure(field + ".Value", "Invalid payment fee value.");
                }

                if (fee.Value != null && !fee.Value.Unit.HasValue())
                {
                    context.AddFailure(field + ".Unit", "A payment fee unit is required.");
                }
            }
        });
    }
}

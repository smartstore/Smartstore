using System.Text.Json.Nodes;
using FluentValidation;
using Smartstore.Admin.Components;
using HomeController = Smartstore.Admin.Controllers.HomeController;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Widgets;
using Smartstore.Core.Widgets.Dashboard;
using Smartstore.Engine.Modularity;
using Smartstore.Http;

namespace Smartstore.Admin.Infrastructure.Dashboard;

/// <summary>
/// Describes and creates the dashboard widget that ranks payment methods by order count and order total.
/// </summary>
[SystemName(SystemName)]
public sealed class PaymentMethodsDashboardWidget(
    IPermissionService permissions,
    IValidator<PaymentMethodsDashboardWidgetSettings> validator) : DashboardViewComponentWidget<DashboardPaymentMethodsViewComponent, PaymentMethodsDashboardWidgetSettings>(
        permissions,
        DashboardPaymentMethodsViewComponent.RequiredPermission,
        validator)
{
    /// <summary>
    /// Identifies the payment methods dashboard widget type.
    /// </summary>
    public const string SystemName = "Smartstore.Admin.Dashboard.PaymentMethods";

    /// <summary>Prefixes the per-provider fee values.</summary>
    public const string FeeValuePrefix = "feeValue.";

    /// <summary>Prefixes the Boolean indicating percentage fees.</summary>
    public const string FeeIsPercentagePrefix = "feeIsPercentage.";

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
    public override RouteInfo GetConfigurationRoute()
        => new(nameof(HomeController.DashboardConfigure), "Home", new { area = "Admin", widgetSystemName = SystemName });

    /// <inheritdoc />
    public override JsonObject CreateDefaultSettings()
        => new() { ["count"] = 10 };

    /// <inheritdoc />
    protected override object GetWidgetArguments(DashboardWidgetContext context, DashboardWidgetInstance instance, PaymentMethodsDashboardWidgetSettings settings)
        => new
        {
            feeValues = settings.FeeValues,
            feeIsPercentage = settings.FeeIsPercentage,
            count = settings.Count
        };

    /// <inheritdoc />
    protected override Widget CreateConfigurationWidget(DashboardWidgetContext context, DashboardWidgetInstance instance, PaymentMethodsDashboardWidgetSettings settings)
        => new ComponentWidget<DashboardPaymentMethodsViewComponent>(new
        {
            feeValues = settings.FeeValues,
            feeIsPercentage = settings.FeeIsPercentage,
            configure = true,
            count = settings.Count
        });

    /// <inheritdoc />
    protected override PaymentMethodsDashboardWidgetSettings LoadSettings(JsonObject settings)
    {
        // Form field names use provider-prefixed keys; deserialize their values through the shared settings loader.
        var values = new JsonObject();
        var modes = new JsonObject();
        foreach (var setting in settings)
        {
            if (setting.Key.StartsWith(FeeValuePrefix, StringComparison.Ordinal))
            {
                values[setting.Key[FeeValuePrefix.Length..]] = setting.Value?.DeepClone();
            }
            else if (setting.Key.StartsWith(FeeIsPercentagePrefix, StringComparison.Ordinal))
            {
                modes[setting.Key[FeeIsPercentagePrefix.Length..]] = setting.Value?.DeepClone();
            }
        }

        var data = new JsonObject { ["feeValues"] = values, ["feeIsPercentage"] = modes };
        if (settings.ContainsKey("count"))
        {
            data["count"] = settings["count"]?.DeepClone();
        }

        return base.LoadSettings(data);
    }
}

/// <summary>Contains the settings of the PaymentMethods dashboard widget.</summary>
public class PaymentMethodsDashboardWidgetSettings
{
    /// <summary>Gets or sets the maximum number of items to display.</summary>
    public int Count { get; set; } = 10;

    /// <summary>Gets or sets the fee amount for each payment provider.</summary>
    public Dictionary<string, decimal> FeeValues { get; set; } = new(StringComparer.Ordinal);

    /// <summary>Gets or sets whether a provider's fee is a percentage.</summary>
    public Dictionary<string, bool> FeeIsPercentage { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>Validates settings of the PaymentMethods dashboard widget.</summary>
public class PaymentMethodsDashboardWidgetValidator : AbstractValidator<PaymentMethodsDashboardWidgetSettings>
{
    public PaymentMethodsDashboardWidgetValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 50)
            .WithMessage("The number of payment methods must be between 1 and 50.");

        RuleForEach(x => x.FeeIsPercentage).Must(fee => fee.Key.HasValue())
            .WithMessage("A payment provider is required for each fee mode.");

        RuleForEach(x => x.FeeValues).Must((settings, fee) =>
        {
            var percentage = !settings.FeeIsPercentage.TryGetValue(fee.Key, out var mode) || mode;
            return fee.Key.HasValue() && fee.Value >= 0 && fee.Value <= (percentage ? 100 : 1000000);
        }).WithMessage("Invalid payment fee value.");
    }
}

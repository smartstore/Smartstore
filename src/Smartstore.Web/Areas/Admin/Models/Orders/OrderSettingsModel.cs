using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Localization;
using Smartstore.Json;

namespace Smartstore.Admin.Models.Orders;

[LocalizedDisplay("Admin.Configuration.Settings.Order.")]
public class OrderSettingsModel : TabbableModel, ILocalizedModel<OrderSettingsLocalizedModel>
{
    [LocalizedDisplay("*IsReOrderAllowed")]
    public bool IsReOrderAllowed { get; set; }

    [LocalizedDisplay("*MinOrderTotal")]
    public decimal? OrderTotalMinimum { get; set; }

    [LocalizedDisplay("*MaxOrderTotal")]
    public decimal? OrderTotalMaximum { get; set; }

    [LocalizedDisplay("*MultipleOrderTotalRestrictionsExpandRange")]
    public bool MultipleOrderTotalRestrictionsExpandRange { get; set; }

    [LocalizedDisplay("*AnonymousCheckoutAllowed")]
    public bool AnonymousCheckoutAllowed { get; set; }

    [LocalizedDisplay("*DisableOrderCompletedPage")]
    public bool DisableOrderCompletedPage { get; set; }

    [LocalizedDisplay("*ReturnRequestsEnabled")]
    public bool ReturnRequestsEnabled { get; set; }

    [LocalizedDisplay("*ReturnRequestReasons")]
    [UIHint("StringList"), AdditionalMetadata("sortable", true)]
    public string[] ReturnRequestReasons { get; set; } = [];

    [LocalizedDisplay("*ReturnRequestActions")]
    [UIHint("StringList"), AdditionalMetadata("sortable", true)]
    public string[] ReturnRequestActions { get; set; } = [];

    [LocalizedDisplay("*NumberOfDaysReturnRequestAvailable")]
    public int NumberOfDaysReturnRequestAvailable { get; set; }

    [LocalizedDisplay("*GiftCards_Activated")]
    public int? GiftCardsActivatedOrderStatusId { get; set; }

    [LocalizedDisplay("*GiftCards_Deactivated")]
    public int? GiftCardsDeactivatedOrderStatusId { get; set; }

    [LocalizedDisplay("*MaxMessageOrderAgeInDays")]
    public int MaxMessageOrderAgeInDays { get; set; }

    public string PrimaryStoreCurrencyCode { get; set; }
    public int StoreCount { get; set; }

    [LocalizedDisplay("*OrderIdent")]
    [AdditionalMetadata("invariant", true)]
    public int? OrderIdent { get; set; }

    [LocalizedDisplay("*DisplayOrdersOfAllStores")]
    public bool DisplayOrdersOfAllStores { get; set; }

    [LocalizedDisplay("*OrderListPageSize")]
    public int OrderListPageSize { get; set; } = 10;

    public List<OrderSettingsLocalizedModel> Locales { get; set; } = new();
}

[LocalizedDisplay("Admin.Configuration.Settings.Order.")]
public class OrderSettingsLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [LocalizedDisplay("*ReturnRequestReasons")]
    [UIHint("StringList"), AdditionalMetadata("sortable", true)]
    public string[] ReturnRequestReasons { get; set; } = [];

    [LocalizedDisplay("*ReturnRequestActions")]
    [UIHint("StringList"), AdditionalMetadata("sortable", true)]
    public string[] ReturnRequestActions { get; set; } = [];
}

public partial class OrderSettingsValidator : SettingModelValidator<OrderSettingsModel, OrderSettings>
{
    public OrderSettingsValidator(Localizer T)
    {
        RuleFor(x => x.GiftCardsActivatedOrderStatusId).NotEqual((int)OrderStatus.Pending)
            .WithMessage(T("Admin.Configuration.Settings.RewardPoints.PointsForPurchases_Awarded.Pending"));

        RuleFor(x => x.GiftCardsDeactivatedOrderStatusId).NotEqual((int)OrderStatus.Pending)
            .WithMessage(T("Admin.Configuration.Settings.RewardPoints.PointsForPurchases_Canceled.Pending"));

        RuleFor(x => x.OrderListPageSize).GreaterThan(0);
        RuleFor(x => x.OrderTotalMaximum).GreaterThan(x => x.OrderTotalMinimum ?? 0);
        RuleFor(x => x.OrderTotalMinimum).GreaterThan(0);
        RuleFor(x => x.OrderIdent).GreaterThan(0);
        RuleFor(x => x.NumberOfDaysReturnRequestAvailable).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxMessageOrderAgeInDays).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// Maps return option lists between the editor model and their JSON settings representation.
/// </summary>
[Mapper(Lifetime = ServiceLifetime.Singleton)]
public class OrderSettingsMapper :
    IMapper<OrderSettings, OrderSettingsModel>,
    IMapper<OrderSettingsModel, OrderSettings>
{
    /// <inheritdoc/>
    public Task MapAsync(OrderSettings from, OrderSettingsModel to, dynamic parameters = null)
    {
        MiniMapper.Map(from, to);

        to.ReturnRequestReasons = from.ReturnRequestReasons.HasValue()
            ? JsonSerializer.Deserialize<string[]>(from.ReturnRequestReasons, SmartJsonOptions.Default) ?? []
            : [];
        to.ReturnRequestActions = from.ReturnRequestActions.HasValue()
            ? JsonSerializer.Deserialize<string[]>(from.ReturnRequestActions, SmartJsonOptions.Default) ?? []
            : [];

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task MapAsync(OrderSettingsModel from, OrderSettings to, dynamic parameters = null)
    {
        MiniMapper.Map(from, to);

        var returnRequestReasons = from.ReturnRequestReasons
            ?.Where(x => x.HasValue())
            .Select(x => x.Trim())
            .ToArray() ?? [];
        var returnRequestActions = from.ReturnRequestActions
            ?.Where(x => x.HasValue())
            .Select(x => x.Trim())
            .ToArray() ?? [];

        to.ReturnRequestReasons = returnRequestReasons.Length > 0
            ? JsonSerializer.Serialize(returnRequestReasons, SmartJsonOptions.Default)
            : string.Empty;
        to.ReturnRequestActions = returnRequestActions.Length > 0
            ? JsonSerializer.Serialize(returnRequestActions, SmartJsonOptions.Default)
            : string.Empty;

        return Task.CompletedTask;
    }
}

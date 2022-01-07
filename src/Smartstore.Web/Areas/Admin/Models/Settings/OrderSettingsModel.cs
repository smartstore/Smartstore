using FluentValidation;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.Order.")]
    public class OrderSettingsModel : ModelBase, ILocalizedModel<OrderSettingsLocalizedModel>
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

        [LocalizedDisplay("*TermsOfServiceEnabled")]
        public bool TermsOfServiceEnabled { get; set; }

        [LocalizedDisplay("*DisableOrderCompletedPage")]
        public bool DisableOrderCompletedPage { get; set; }

        [LocalizedDisplay("*ReturnRequestsEnabled")]
        public bool ReturnRequestsEnabled { get; set; }

        [LocalizedDisplay("*ReturnRequestReasons")]
        public string ReturnRequestReasons { get; set; }

        [LocalizedDisplay("*ReturnRequestActions")]
        public string ReturnRequestActions { get; set; }

        [LocalizedDisplay("*NumberOfDaysReturnRequestAvailable")]
        public int NumberOfDaysReturnRequestAvailable { get; set; }

        [LocalizedDisplay("*GiftCards_Activated")]
        public int? GiftCards_Activated_OrderStatusId { get; set; }
        
        [LocalizedDisplay("*GiftCards_Deactivated")]
        public int? GiftCards_Deactivated_OrderStatusId { get; set; }

        public string PrimaryStoreCurrencyCode { get; set; }
        public int StoreCount { get; set; }

        [LocalizedDisplay("*OrderIdent")]
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
        public string ReturnRequestReasons { get; set; }

        [LocalizedDisplay("*ReturnRequestActions")]
        public string ReturnRequestActions { get; set; }
    }

    public partial class OrderSettingsValidator : AbstractValidator<OrderSettingsModel>
    {
        public OrderSettingsValidator(Localizer T)
        {
            RuleFor(x => x.GiftCards_Activated_OrderStatusId).NotEqual((int)OrderStatus.Pending)
                .WithMessage(T("Admin.Configuration.Settings.RewardPoints.PointsForPurchases_Awarded.Pending"));

            RuleFor(x => x.GiftCards_Deactivated_OrderStatusId).NotEqual((int)OrderStatus.Pending)
                .WithMessage(T("Admin.Configuration.Settings.RewardPoints.PointsForPurchases_Canceled.Pending"));

            RuleFor(x => x.OrderListPageSize)
                .GreaterThan(0);

            RuleFor(x => x.OrderTotalMaximum)
                .GreaterThan(x => x.OrderTotalMinimum ?? 0);
        }
    }
}

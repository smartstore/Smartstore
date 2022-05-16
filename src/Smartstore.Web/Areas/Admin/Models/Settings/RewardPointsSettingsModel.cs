using FluentValidation;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.RewardPoints.")]
    public class RewardPointsSettingsModel
    {
        [LocalizedDisplay("*Enabled")]
        public bool Enabled { get; set; }

        [LocalizedDisplay("*ExchangeRate")]
        public decimal ExchangeRate { get; set; }

        [LocalizedDisplay("*RoundDownRewardPoints")]
        public bool RoundDownRewardPoints { get; set; }

        [LocalizedDisplay("*PointsForRegistration")]
        public int PointsForRegistration { get; set; }

        [LocalizedDisplay("*PointsForProductReview")]
        public int PointsForProductReview { get; set; }

        [LocalizedDisplay("*PointsForPurchases_Amount")]
        public decimal PointsForPurchases_Amount { get; set; }
        public int PointsForPurchases_Points { get; set; }

        [LocalizedDisplay("*PointsForPurchases_Awarded")]
        public OrderStatus PointsForPurchases_Awarded { get; set; }

        [LocalizedDisplay("*PointsForPurchases_Canceled")]
        public OrderStatus PointsForPurchases_Canceled { get; set; }

        public string PrimaryStoreCurrencyCode { get; set; }
    }

    public partial class RewardPointsSettingsValidator : SettingModelValidator<RewardPointsSettingsModel, RewardPointsSettings>
    {
        public RewardPointsSettingsValidator(Localizer T)
        {
            RuleFor(x => x.PointsForPurchases_Awarded).NotEqual(OrderStatus.Pending)
                .WithMessage(T("Admin.Configuration.Settings.RewardPoints.PointsForPurchases_Awarded.Pending"));

            RuleFor(x => x.PointsForPurchases_Canceled).NotEqual(OrderStatus.Pending)
                .WithMessage(T("Admin.Configuration.Settings.RewardPoints.PointsForPurchases_Canceled.Pending"));
        }
    }
}

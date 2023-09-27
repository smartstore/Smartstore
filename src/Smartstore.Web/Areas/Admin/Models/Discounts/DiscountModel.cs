using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Rules;

namespace Smartstore.Admin.Models.Discounts
{
    [LocalizedDisplay("Admin.Promotions.Discounts.Fields.")]
    public class DiscountModel : EntityModelBase, ILocalizedModel<DiscountLocalizedModel>
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*DiscountType")]
        public int DiscountTypeId { get; set; }

        [LocalizedDisplay("*DiscountType")]
        public string DiscountTypeName { get; set; }

        [LocalizedDisplay("*UsePercentage")]
        public bool UsePercentage { get; set; }

        [LocalizedDisplay("*DiscountPercentage")]
        public decimal DiscountPercentage { get; set; }

        [LocalizedDisplay("*DiscountPercentage")]
        public string FormattedDiscountPercentage
        {
            get => UsePercentage ? (DiscountPercentage / 100).ToString("P2") : string.Empty;
        }

        [LocalizedDisplay("*DiscountAmount")]
        public decimal DiscountAmount { get; set; }

        [LocalizedDisplay("*DiscountAmount")]
        public string FormattedDiscountAmount { get; set; }

        [LocalizedDisplay("*StartDate")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? StartDateUtc { get; set; }

        [LocalizedDisplay("*StartDate")]
        public string StartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        [AdditionalMetadata("pickTime", true)]
        public DateTime? EndDateUtc { get; set; }

        [LocalizedDisplay("*EndDate")]
        public string EndDate { get; set; }

        [LocalizedDisplay("*RequiresCouponCode")]
        public bool RequiresCouponCode { get; set; }

        [LocalizedDisplay("*CouponCode")]
        public string CouponCode { get; set; }

        [LocalizedDisplay("*DiscountLimitation")]
        public int DiscountLimitationId { get; set; }

        [LocalizedDisplay("*LimitationTimes")]
        public int LimitationTimes { get; set; }

        [LocalizedDisplay("*ShowCountdownRemainingHours")]
        public int? ShowCountdownRemainingHours { get; set; }

        [LocalizedDisplay("*OfferBadgeLabel")]
        public string OfferBadgeLabel { get; set; }

        [UIHint("RuleSets")]
        [AdditionalMetadata("multiple", true)]
        [AdditionalMetadata("scope", RuleScope.Cart)]
        [LocalizedDisplay("Admin.Promotions.Discounts.RuleSetRequirements")]
        public int[] SelectedRuleSetIds { get; set; }

        [LocalizedDisplay("Admin.Rules.NumberOfRules")]
        public int NumberOfRules { get; set; }

        public string EditUrl { get; set; }
        public List<DiscountLocalizedModel> Locales { get; set; } = new();
    }

    [LocalizedDisplay("Admin.Promotions.Discounts.Fields.")]
    public class DiscountLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*OfferBadgeLabel")]
        public string OfferBadgeLabel { get; set; }
    }

    public partial class DiscountValidator : AbstractValidator<DiscountModel>
    {
        public DiscountValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.OfferBadgeLabel).MaximumLength(50);
            RuleFor(x => x.ShowCountdownRemainingHours)
                .GreaterThan(0)
                .When(x => x.ShowCountdownRemainingHours != null);
        }
    }

    public class DiscountMapper :
        IMapper<Discount, DiscountModel>,
        IMapper<DiscountModel, Discount>
    {
        private readonly IUrlHelper _urlHelper;
        private readonly ICommonServices _services;

        public DiscountMapper(IUrlHelper urlHelper, ICommonServices services)
        {
            _urlHelper = urlHelper;
            _services = services;
        }

        public Task MapAsync(Discount from, DiscountModel to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            MiniMapper.Map(from, to);

            to.NumberOfRules = from.RuleSets?.Count ?? 0;
            to.DiscountTypeName = _services.Localization.GetLocalizedEnum(from.DiscountType);
            to.FormattedDiscountAmount = !from.UsePercentage
                ? _services.CurrencyService.CreateMoney(from.DiscountAmount, _services.CurrencyService.PrimaryCurrency).ToString(true)
                : string.Empty;

            if (from.StartDateUtc.HasValue)
            {
                to.StartDate = _services.DateTimeHelper.ConvertToUserTime(from.StartDateUtc.Value, DateTimeKind.Utc).ToShortDateString();
            }
            if (from.EndDateUtc.HasValue)
            {
                to.EndDate = _services.DateTimeHelper.ConvertToUserTime(from.EndDateUtc.Value, DateTimeKind.Utc).ToShortDateString();
            }

            to.EditUrl = _urlHelper.Action("Edit", "Discount", new { id = from.Id, area = "Admin" });

            return Task.CompletedTask;
        }

        public Task MapAsync(DiscountModel from, Discount to, dynamic parameters = null)
        {
            Guard.NotNull(from);
            Guard.NotNull(to);

            MiniMapper.Map(from, to);

            if (from.StartDateUtc.HasValue && from.StartDateUtc.Value.Kind != DateTimeKind.Utc)
            {
                to.StartDateUtc = _services.DateTimeHelper.ConvertToUtcTime(from.StartDateUtc.Value);
            }

            if (from.EndDateUtc.HasValue && from.EndDateUtc.Value.Kind != DateTimeKind.Utc)
            {
                to.EndDateUtc = _services.DateTimeHelper.ConvertToUtcTime(from.EndDateUtc.Value);
            }

            return Task.CompletedTask;
        }
    }
}

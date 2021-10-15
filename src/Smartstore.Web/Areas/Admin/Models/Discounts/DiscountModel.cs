using System;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Rules;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Discounts
{
    [LocalizedDisplay("Admin.Promotions.Discounts.List.")]
    public class DiscountListModel
    {
        [LocalizedDisplay("*Name")]
        public string SearchName { get; set; }

        [LocalizedDisplay("*DiscountType")]
        public int? SearchDiscountTypeId { get; set; }

        [LocalizedDisplay("*UsePercentage")]
        public bool? SearchUsePercentage { get; set; }

        [LocalizedDisplay("*RequiresCouponCode")]
        public bool? SearchRequiresCouponCode { get; set; }
    }

    [LocalizedDisplay("Admin.Promotions.Discounts.Fields.")]
    public class DiscountModel : EntityModelBase
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
        public DateTime? StartDateUtc { get; set; }

        [LocalizedDisplay("*StartDate")]
        public DateTime? StartDate { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? EndDateUtc { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? EndDate { get; set; }

        [LocalizedDisplay("*RequiresCouponCode")]
        public bool RequiresCouponCode { get; set; }

        [LocalizedDisplay("*CouponCode")]
        public string CouponCode { get; set; }

        [LocalizedDisplay("*DiscountLimitation")]
        public int DiscountLimitationId { get; set; }

        [LocalizedDisplay("*LimitationTimes")]
        public int LimitationTimes { get; set; }

        [UIHint("RuleSets")]
        [AdditionalMetadata("multiple", true)]
        [AdditionalMetadata("scope", RuleScope.Cart)]
        [LocalizedDisplay("Admin.Promotions.Discounts.RuleSetRequirements")]
        public int[] SelectedRuleSetIds { get; set; }

        [LocalizedDisplay("Admin.Rules.NumberOfRules")]
        public int NumberOfRules { get; set; }

        public string EditUrl { get; set; }
    }

    public class DiscountAppliedToEntityModel : EntityModelBase
    {
        public string Name { get; set; }
    }

    public class DiscountUsageHistoryModel : EntityModelBase
    {
        public int DiscountId { get; set; }
        public string OrderEditUrl { get; set; }
        public string OrderEditLinkText { get; set; }

        [LocalizedDisplay("Admin.Promotions.Discounts.History.Order")]
        public int OrderId { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOnUtc { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }
    }

    public partial class DiscountValidator : AbstractValidator<DiscountModel>
    {
        public DiscountValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}

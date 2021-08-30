using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Rules;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Discounts
{
    [LocalizedDisplay("Admin.Promotions.Discounts.Fields.")]
    public class DiscountModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*DiscountType")]
        public int DiscountTypeId { get; set; }
        public string DiscountTypeName { get; set; }

        [LocalizedDisplay("*UsePercentage")]
        public bool UsePercentage { get; set; }

        [LocalizedDisplay("*DiscountPercentage")]
        public decimal DiscountPercentage { get; set; }

        [LocalizedDisplay("*DiscountPercentage")]
        public string FormattedDiscountPercentage
        {
            get
            {
                if (UsePercentage)
                {
                    return string.Format("{0:0.##}", DiscountPercentage);
                }

                return string.Empty;
            }
        }

        [LocalizedDisplay("*DiscountAmount")]
        public decimal DiscountAmount { get; set; }

        [LocalizedDisplay("*DiscountAmount")]
        public string FormattedDiscountAmount { get; set; }

        [LocalizedDisplay("*StartDate")]
        public DateTime? StartDateUtc { get; set; }

        [LocalizedDisplay("*EndDate")]
        public DateTime? EndDateUtc { get; set; }

        [LocalizedDisplay("*RequiresCouponCode")]
        public bool RequiresCouponCode { get; set; }

        [LocalizedDisplay("*CouponCode")]
        public string CouponCode { get; set; }

        [LocalizedDisplay("*DiscountLimitation")]
        public int DiscountLimitationId { get; set; }

        [LocalizedDisplay("*LimitationTimes")]
        public int LimitationTimes { get; set; }


        [LocalizedDisplay("*AppliedToCategories")]
        public IList<AppliedToEntityModel> AppliedToCategories { get; set; }

        [LocalizedDisplay("*AppliedToManufacturers")]
        public IList<AppliedToEntityModel> AppliedToManufacturers { get; set; }

        [LocalizedDisplay("*AppliedToProducts")]
        public IList<AppliedToEntityModel> AppliedToProducts { get; set; }


        [UIHint("RuleSets")]
        [AdditionalMetadata("multiple", true)]
        [AdditionalMetadata("scope", RuleScope.Cart)]
        [LocalizedDisplay("Admin.Promotions.Discounts.RuleSetRequirements")]
        public int[] SelectedRuleSetIds { get; set; }

        [LocalizedDisplay("Admin.Rules.NumberOfRules")]
        public int NumberOfRules { get; set; }

        public string EditUrl { get; set; }

        #region Nested classes

        public class DiscountUsageHistoryModel : EntityModelBase
        {
            public int DiscountId { get; set; }

            [LocalizedDisplay("Admin.Promotions.Discounts.History.Order")]
            public int OrderId { get; set; }

            [LocalizedDisplay("Common.CreatedOn")]
            public DateTime CreatedOn { get; set; }
        }

        public class AppliedToEntityModel : EntityModelBase
        {
            public string Name { get; set; }
        }

        #endregion
    }

    public partial class DiscountValidator : AbstractValidator<DiscountModel>
    {
        public DiscountValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}

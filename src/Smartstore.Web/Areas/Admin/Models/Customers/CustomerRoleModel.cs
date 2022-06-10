using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules;

namespace Smartstore.Admin.Models.Customers
{
    [LocalizedDisplay("Admin.Customers.CustomerRoles.Fields.")]
    public class CustomerRoleModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*FreeShipping")]
        public bool FreeShipping { get; set; }

        [LocalizedDisplay("*TaxExempt")]
        public bool TaxExempt { get; set; }

        [LocalizedDisplay("*TaxDisplayType")]
        public int? TaxDisplayType { get; set; }

        [LocalizedDisplay("*Active")]
        public bool Active { get; set; }

        [LocalizedDisplay("*IsSystemRole")]
        public bool IsSystemRole { get; set; }

        [LocalizedDisplay("*SystemName")]
        public string SystemName { get; set; }

        [LocalizedDisplay("*MinOrderTotal")]
        public decimal? OrderTotalMinimum { get; set; }

        [LocalizedDisplay("*MaxOrderTotal")]
        public decimal? OrderTotalMaximum { get; set; }

        [UIHint("RuleSets")]
        [AdditionalMetadata("multiple", true)]
        [AdditionalMetadata("scope", RuleScope.Customer)]
        [LocalizedDisplay("Admin.Customers.CustomerRoles.AutomatedAssignmentRules")]
        public int[] SelectedRuleSetIds { get; set; }

        public string EditUrl { get; set; }
    }

    public partial class CustomerRoleValidator : AbstractValidator<CustomerRoleModel>
    {
        public CustomerRoleValidator(Localizer _)
        {
            RuleFor(x => x.Name).NotNull();

            RuleFor(x => x.OrderTotalMaximum)
                .GreaterThan(x => x.OrderTotalMinimum ?? 0);
        }
    }
}

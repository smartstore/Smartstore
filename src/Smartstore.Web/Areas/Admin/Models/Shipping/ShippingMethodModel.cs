using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Rules;

namespace Smartstore.Admin.Models.Shipping
{
    [LocalizedDisplay("Admin.Configuration.Shipping.Methods.Fields.")]
    public class ShippingMethodModel : TabbableModel, ILocalizedModel<ShippingMethodLocalizedModel>
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*Description")]
        public string Description { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*IgnoreCharges")]
        public bool IgnoreCharges { get; set; }

        public List<ShippingMethodLocalizedModel> Locales { get; set; } = new();

        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        [UIHint("RuleSets")]
        [AdditionalMetadata("multiple", true)]
        [AdditionalMetadata("scope", RuleScope.Cart)]
        [LocalizedDisplay("*Requirements")]
        public int[] SelectedRuleSetIds { get; set; }

        [LocalizedDisplay("Admin.Rules.NumberOfRules")]
        public int NumberOfRules { get; set; }

        public string EditUrl { get; set; }
    }

    [LocalizedDisplay("Admin.Configuration.Shipping.Methods.Fields.")]
    public class ShippingMethodLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [UIHint("Html")]
        [LocalizedDisplay("*Description")]
        public string Description { get; set; }
    }

    public partial class ShippingMethodValidator : AbstractValidator<ShippingMethodModel>
    {
        public ShippingMethodValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}

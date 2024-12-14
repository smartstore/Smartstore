using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Rules;

namespace Smartstore.Admin.Models.Payments
{
    [LocalizedDisplay("Admin.Configuration.Payment.Methods.")]
    public class PaymentMethodEditModel : TabbableModel, ILocalizedModel<PaymentMethodLocalizedModel>
    {
        public Type GetEntityType() => typeof(PaymentMethod);
        public List<PaymentMethodLocalizedModel> Locales { get; set; } = [];
        public string IconUrl { get; set; }

        [LocalizedDisplay("Common.SystemName")]
        public string SystemName { get; set; }

        [LocalizedDisplay("Common.FriendlyName")]
        public string FriendlyName { get; set; }

        [LocalizedDisplay("*ShortDescription")]
        [UIHint("Textarea"), AdditionalMetadata("rows", 3)]
        public string Description { get; set; }

        [LocalizedDisplay("*FullDescription")]
        [UIHint("Html")]
        public string FullDescription { get; set; }

        [LocalizedDisplay("*RoundOrderTotalEnabled")]
        public bool RoundOrderTotalEnabled { get; set; }

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
    }

    [LocalizedDisplay("Admin.Configuration.Payment.Methods.")]
    public class PaymentMethodLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Common.FriendlyName")]
        public string FriendlyName { get; set; }

        [LocalizedDisplay("*ShortDescription")]
        [UIHint("Textarea"), AdditionalMetadata("rows", 3)]
        public string Description { get; set; }

        [LocalizedDisplay("*FullDescription")]
        [UIHint("Html")]
        public string FullDescription { get; set; }
    }
}

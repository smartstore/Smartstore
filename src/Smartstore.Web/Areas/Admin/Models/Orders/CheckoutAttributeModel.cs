using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Catalog.Attributes.CheckoutAttributes.Fields.")]
    public class CheckoutAttributeModel : EntityModelBase, ILocalizedModel<CheckoutAttributeLocalizedModel>
    {
        [LocalizedDisplay("Common.IsActive")]
        public bool IsActive { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*TextPrompt")]
        public string TextPrompt { get; set; }

        [LocalizedDisplay("*IsRequired")]
        public bool IsRequired { get; set; }

        [LocalizedDisplay("*ShippableProductRequired")]
        public bool ShippableProductRequired { get; set; }

        [LocalizedDisplay("*IsTaxExempt")]
        public bool IsTaxExempt { get; set; }

        [LocalizedDisplay("*TaxCategory")]
        public int? TaxCategoryId { get; set; }

        [LocalizedDisplay("Admin.Catalog.Attributes.AttributeControlType")]
        public int AttributeControlTypeId { get; set; }

        [LocalizedDisplay("Admin.Catalog.Attributes.AttributeControlType")]
        public string AttributeControlTypeName { get; set; }

        public bool IsListTypeAttribute { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        public List<CheckoutAttributeLocalizedModel> Locales { get; set; } = new();

        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        [LocalizedDisplay("Admin.Catalog.Attributes.CheckoutAttributes.Values")]
        public int NumberOfOptions { get; set; }

        public string EditUrl { get; set; }
    }

    [LocalizedDisplay("Admin.Catalog.Attributes.CheckoutAttributes.Fields.")]
    public class CheckoutAttributeLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*TextPrompt")]
        public string TextPrompt { get; set; }
    }

    public partial class CheckoutAttributeValidator : AbstractValidator<CheckoutAttributeModel>
    {
        public CheckoutAttributeValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}

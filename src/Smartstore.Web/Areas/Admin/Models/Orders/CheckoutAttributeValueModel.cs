using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.Admin.Models.Orders
{
    [LocalizedDisplay("Admin.Catalog.Attributes.CheckoutAttributes.Values.Fields.")]
    public class CheckoutAttributeValueModel : EntityModelBase, ILocalizedModel<CheckoutAttributeValueLocalizedModel>
    {
        public int CheckoutAttributeId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }
        public string NameString { get; set; }

        [LocalizedDisplay("*PriceAdjustment")]
        public decimal PriceAdjustment { get; set; }
        public string PrimaryStoreCurrencyCode { get; set; }

        [LocalizedDisplay("*WeightAdjustment")]
        public decimal WeightAdjustment { get; set; }
        public string BaseWeight { get; set; }

        [LocalizedDisplay("*IsPreSelected")]
        public bool IsPreSelected { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [UIHint("Media"), AdditionalMetadata("album", "catalog"), AdditionalMetadata("entityType", "CheckoutAttributeValue")]
        [LocalizedDisplay("*MediaFile")]
        public int? MediaFileId { get; set; }

        [LocalizedDisplay("*Color")]
        [UIHint("Color")]
        public string Color { get; set; }

        public List<CheckoutAttributeValueLocalizedModel> Locales { get; set; } = new();
    }

    [LocalizedDisplay("Admin.Catalog.Attributes.CheckoutAttributes.Values.Fields.")]
    public class CheckoutAttributeValueLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }
    }

    public partial class CheckoutAttributeValueValidator : AbstractValidator<CheckoutAttributeValueModel>
    {
        public CheckoutAttributeValueValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}

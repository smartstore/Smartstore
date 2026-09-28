using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Checkout.Attributes;

namespace Smartstore.Admin.Models.Orders;

[LocalizedDisplay("Admin.Catalog.Attributes.CheckoutAttributes.Fields.")]
public class CheckoutAttributeModel : EntityModelBase, ILocalizedModel<CheckoutAttributeLocalizedModel>
{
    public Type GetEntityType() => typeof(CheckoutAttribute);

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

    [UIHint("Range"), Range(0, 50)]
    [AdditionalMetadata("min", 0)]
    [AdditionalMetadata("max", 50)]
    [AdditionalMetadata("step", 10)]
    [AdditionalMetadata("format", "")]
    [LocalizedDisplay("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchSize")]
    public int? SwatchSize { get; set; }

    [UIHint("AspectRatio"), Range(0.1, 3.0)]
    [AdditionalMetadata("min", 0.1)]
    [AdditionalMetadata("max", 3.0)]
    [AdditionalMetadata("step", 0.01)]
    [AdditionalMetadata("format", "{0:F2}")]
    [AdditionalMetadata("ticks", "0.1|10:1, 1|1:1, 2|1:2, 3|1:3")]
    [LocalizedDisplay("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchAspectRatio")]
    public decimal SwatchAspectRatio { get; set; } = 1m;

    [LocalizedDisplay("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchShape")]
    public SwatchShape? SwatchShape { get; set; }

    [LocalizedDisplay("Admin.Catalog.Attributes.ProductAttributes.Fields.ShowValueNameInSwatch")]
    public bool ShowValueNameInSwatch { get; set; }

    [LocalizedDisplay("Admin.Catalog.Attributes.ProductAttributes.Fields.SwatchPriceDisplay")]
    public SwatchPriceDisplayMode SwatchPriceDisplay { get; set; }

    public List<CheckoutAttributeLocalizedModel> Locales { get; set; } = [];

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
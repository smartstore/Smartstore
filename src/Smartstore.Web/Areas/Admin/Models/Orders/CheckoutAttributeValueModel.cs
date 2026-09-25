using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Admin.Models.Common;
using Smartstore.Core.Checkout.Attributes;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Orders;

[LocalizedDisplay("Admin.Catalog.Attributes.CheckoutAttributes.Values.Fields.")]
public class CheckoutAttributeValueModel : EntityModelBase, ILocalizedModel<CheckoutAttributeValueLocalizedModel>
{
    public Type GetEntityType() => typeof(CheckoutAttributeValue);
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

    [UIHint("ColorPalette")]
    [AdditionalMetadata("maxColors", ColorPaletteModel.DefaultMaxColors)]
    [LocalizedDisplay("*Color")]
    public ColorPaletteModel Colors { get; set; } = new();
    public string Color => Colors?.Color;
    public bool HasColor => Color.HasValue();

    public List<CheckoutAttributeValueLocalizedModel> Locales { get; set; } = [];
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
    public CheckoutAttributeValueValidator(Localizer T)
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Colors)
            .Must(x => x == null || (x.AdditionalColors?.Count(y => y.HasValue()) ?? 0) < ColorPaletteModel.DefaultMaxColors)
            .WithMessage(T("Admin.Common.ColorPalette.TooManyColors", ColorPaletteModel.DefaultMaxColors));
        RuleFor(x => x.Colors)
            .Must(x => x?.AdditionalColors?.Any(y => y.HasValue()) != true || x.Color.HasValue())
            .WithMessage(T("Admin.Common.ColorPalette.PrimaryColorRequired"));
    }
}
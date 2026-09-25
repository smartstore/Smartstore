using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Admin.Models.Common;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Localization;

namespace Smartstore.Admin.Models.Catalog;

[LocalizedDisplay("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.Fields.")]
public class ProductAttributeOptionModel : EntityModelBase, ILocalizedModel<ProductAttributeOptionLocalizedModel>
{
    public Type GetEntityType() => typeof(ProductAttributeOption);
    public int ProductId { get; set; }
    public int ProductVariantAttributeId { get; set; }
    public int ProductAttributeOptionsSetId { get; set; }

    [LocalizedDisplay("*Name")]
    public string Name { get; set; }
    public string NameString { get; set; }

    [LocalizedDisplay("*Alias")]
    public string Alias { get; set; }

    [UIHint("ColorPalette")]
    [AdditionalMetadata("maxColors", ColorPaletteModel.DefaultMaxColors)]
    [LocalizedDisplay("*ColorSquaresRgb")]
    public ColorPaletteModel Colors { get; set; } = new();
    public string Color => Colors?.Color;
    public bool HasColor => Color.HasValue();

    [LocalizedDisplay("*Picture")]
    [UIHint("Media")]
    [AdditionalMetadata("album", "catalog"), AdditionalMetadata("transientUpload", true), AdditionalMetadata("entityType", "ProductAttributeOption")]
    public int PictureId { get; set; }

    [LocalizedDisplay("*PriceAdjustment")]
    public decimal PriceAdjustment { get; set; }
    [LocalizedDisplay("*PriceAdjustment")]
    public string PriceAdjustmentString { get; set; }

    [LocalizedDisplay("*WeightAdjustment")]
    public decimal WeightAdjustment { get; set; }
    [LocalizedDisplay("*WeightAdjustment")]
    public string WeightAdjustmentString { get; set; }

    [LocalizedDisplay("*IsPreSelected")]
    public bool IsPreSelected { get; set; }

    [LocalizedDisplay("Common.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [LocalizedDisplay("*ValueTypeId")]
    public int ValueTypeId { get; set; }
    [LocalizedDisplay("*ValueTypeId")]
    public string TypeName { get; set; }
    public string TypeNameClass { get; set; }

    [LocalizedDisplay("*LinkedProduct")]
    public int LinkedProductId { get; set; }
    [LocalizedDisplay("*LinkedProduct")]
    public string LinkedProductName { get; set; }
    public string LinkedProductTypeName { get; set; }
    public string LinkedProductTypeLabelHint { get; set; }
    public string LinkedProductEditUrl { get; set; }

    [LocalizedDisplay("*Quantity")]
    public int Quantity { get; set; }
    public string QuantityInfo { get; set; }

    public List<ProductAttributeOptionLocalizedModel> Locales { get; set; } = new();
}

[LocalizedDisplay("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.Fields.")]
public class ProductAttributeOptionLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [LocalizedDisplay("*Name")]
    public string Name { get; set; }

    [LocalizedDisplay("*Alias")]
    public string Alias { get; set; }
}

public partial class ProductAttributeOptionModelValidator : SmartValidator<ProductAttributeOptionModel>
{
    public ProductAttributeOptionModelValidator(SmartDbContext db, Localizer T)
    {
        ApplyEntityRules<ProductAttributeOption>(db, nameof(ProductAttributeOption.Color));

        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.ValueTypeId == (int)ProductVariantAttributeValueType.ProductLinkage);
        RuleFor(x => x.Colors)
            .Must(x => x == null || (x.AdditionalColors?.Count(y => y.HasValue()) ?? 0) < ColorPaletteModel.DefaultMaxColors)
            .WithMessage(T("Admin.Common.ColorPalette.TooManyColors", ColorPaletteModel.DefaultMaxColors));
        RuleFor(x => x.Colors)
            .Must(x => x?.AdditionalColors?.Any(y => y.HasValue()) != true || x.Color.HasValue())
            .WithMessage(T("Admin.Common.ColorPalette.PrimaryColorRequired"));
    }
}

[Mapper(Lifetime = ServiceLifetime.Singleton)]
public class ProductAttributeOptionMapper :
    IMapper<ProductAttributeOption, ProductAttributeOptionModel>,
    IMapper<ProductAttributeOptionModel, ProductAttributeOption>
{
    public Task MapAsync(ProductAttributeOption from, ProductAttributeOptionModel to, dynamic parameters = null)
    {
        MiniMapper.Map(from, to);
        to.PictureId = from.MediaFileId;
        to.Colors = ColorPaletteModel.Create(from.Color, from.AdditionalColors);

        return Task.CompletedTask;
    }

    public Task MapAsync(ProductAttributeOptionModel from, ProductAttributeOption to, dynamic parameters = null)
    {
        MiniMapper.Map(from, to);
        to.MediaFileId = from.PictureId;
        to.LinkedProductId = to.ValueType == ProductVariantAttributeValueType.Simple ? 0 : from.LinkedProductId;
        to.Color = from.Colors?.Color.NullEmpty();
        to.AdditionalColors = from.Colors?.GetAdditionalColors();

        return Task.CompletedTask;
    }
}

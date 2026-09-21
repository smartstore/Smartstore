using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Admin.Models.Catalog;

[LocalizedDisplay("Admin.Catalog.Attributes.ProductAttributes.List.")]
public class ProductAttributeListModel
{
    [LocalizedDisplay("*SearchName")]
    public string SearchName { get; set; }

    [LocalizedDisplay("*SearchAlias")]
    public string SearchAlias { get; set; }

    [LocalizedDisplay("*SearchAllowFiltering")]
    public bool? SearchAllowFiltering { get; set; }
}

[LocalizedDisplay("Admin.Catalog.Attributes.ProductAttributes.Fields.")]
public class ProductAttributeModel : EntityModelBase, ILocalizedModel<ProductAttributeLocalizedModel>
{
    public Type GetEntityType() => typeof(ProductAttribute);

    [LocalizedDisplay("*Alias")]
    public string Alias { get; set; }

    [LocalizedDisplay("*Name")]
    public string Name { get; set; }

    [UIHint("Html")]
    [LocalizedDisplay("*Description")]
    public string Description { get; set; }

    [LocalizedDisplay("*AllowFiltering")]
    public bool AllowFiltering { get; set; }

    [LocalizedDisplay("Common.DisplayOrder")]
    public int DisplayOrder { get; set; }

    [LocalizedDisplay("*FacetTemplateHint")]
    public FacetTemplateHint FacetTemplateHint { get; set; }
    [LocalizedDisplay("*FacetTemplateHint")]
    public string LocalizedFacetTemplateHint { get; set; }

    [LocalizedDisplay("*IndexOptionNames")]
    public bool IndexOptionNames { get; set; }

    [UIHint("Textarea")]
    [AdditionalMetadata("rows", 6)]
    [LocalizedDisplay("*ExportMappings")]
    public string ExportMappings { get; set; }

    [LocalizedDisplay("Admin.Catalog.Attributes.OptionsSets")]
    public string OptionsSetsInfo { get; set; }
    public int NumberOfOptionsSets { get; set; }

    [UIHint("Range"), Range(0, 50)]
    [AdditionalMetadata("min", 0)]
    [AdditionalMetadata("max", 50)]
    [AdditionalMetadata("step", 10)]
    [AdditionalMetadata("format", "")]
    [LocalizedDisplay("*SwatchSize")]
    public int? SwatchSize { get; set; }

    [UIHint("Range"), Range(0.1, 3.0)]
    [AdditionalMetadata("min", 0.1)]
    [AdditionalMetadata("max", 3.0)]
    [AdditionalMetadata("step", 0.05)]
    [AdditionalMetadata("format", "{0:F2}")]
    [AdditionalMetadata("ticks", "0.1|1:10, 1|1:1, 2|2:1, 3|3:1")]
    [LocalizedDisplay("*SwatchAspectRatio")]
    public decimal SwatchAspectRatio { get; set; } = 1m;

    [LocalizedDisplay("*SwatchShape")]
    public SwatchShape? SwatchShape { get; set; }

    [LocalizedDisplay("*ShowValueNameInSwatch")]
    public bool ShowValueNameInSwatch { get; set; }

    [LocalizedDisplay("*SwatchPriceDisplay")]
    public SwatchPriceDisplayMode SwatchPriceDisplay { get; set; }

    public List<ProductAttributeLocalizedModel> Locales { get; set; } = [];
    public string EditUrl { get; set; }
}

[LocalizedDisplay("Admin.Catalog.Attributes.ProductAttributes.Fields.")]
public class ProductAttributeLocalizedModel : ILocalizedLocaleModel
{
    public int LanguageId { get; set; }

    [LocalizedDisplay("*Alias")]
    public string Alias { get; set; }

    [LocalizedDisplay("*Name")]
    public string Name { get; set; }

    [UIHint("Html")]
    [LocalizedDisplay("*Description")]
    public string Description { get; set; }
}

public partial class ProductAttributeModelValidator : SmartValidator<ProductAttributeModel>
{
    public ProductAttributeModelValidator(SmartDbContext db)
    {
        ApplyEntityRules<ProductAttribute>(db);
    }
}

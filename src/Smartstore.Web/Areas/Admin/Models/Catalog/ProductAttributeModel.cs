using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Admin.Models.Catalog
{
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
}
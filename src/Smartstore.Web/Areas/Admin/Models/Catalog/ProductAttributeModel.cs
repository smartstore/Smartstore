using System.Collections.Generic;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data;
using Smartstore.Core.Search.Facets;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.Validation;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Attributes.ProductAttributes.Fields.")]
    public class ProductAttributeModel : EntityModelBase, ILocalizedModel<ProductAttributeLocalizedModel>
    {
        public List<ProductAttributeLocalizedModel> Locales { get; set; } = new();

        [LocalizedDisplay("*Alias")]
        public string Alias { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Description")]
        public string Description { get; set; }

        [LocalizedDisplay("*AllowFiltering")]
        public bool AllowFiltering { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*FacetTemplateHint")]
        public FacetTemplateHint FacetTemplateHint { get; set; }

        [LocalizedDisplay("*IndexOptionNames")]
        public bool IndexOptionNames { get; set; }

        [LocalizedDisplay("*ExportMappings")]
        public string ExportMappings { get; set; } 
    }

    [LocalizedDisplay("Admin.Catalog.Attributes.ProductAttributes.Fields.")]
    public class ProductAttributeLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Alias")]
        public string Alias { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Description")]
        public string Description { get; set; }
    }

    public partial class ProductAttributeModelValidator : SmartValidator<ProductAttributeModel>
    {
        public ProductAttributeModelValidator(SmartDbContext db)
        {
            ApplyDefaultRules<ProductAttribute>(db);
        }
    }
}
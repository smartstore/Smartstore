using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Attributes.SpecificationAttributes.List.")]
    public class SpecificationAttributeListModel
    {
        [LocalizedDisplay("*SearchName")]
        public string SearchName { get; set; }

        [LocalizedDisplay("*SearchAlias")]
        public string SearchAlias { get; set; }

        [LocalizedDisplay("Admin.Catalog.Attributes.SpecificationAttributes.Fields.CollectionGroup")]
        public string SearchCollectionGroupName { get; set; }

        [LocalizedDisplay("*SearchAllowFiltering")]
        public bool? SearchAllowFiltering { get; set; }

        [LocalizedDisplay("*SearchShowOnProductPage")]
        public bool? SearchShowOnProductPage { get; set; }

        [LocalizedDisplay("*SearchEssential")]
        public bool? SearchEssential { get; set; }
    }

    [LocalizedDisplay("Admin.Catalog.Attributes.SpecificationAttributes.Fields.")]
    public class SpecificationAttributeModel : EntityModelBase, ILocalizedModel<SpecificationAttributeLocalizedModel>
    {
        public Type GetEntityType() => typeof(SpecificationAttribute);

        [LocalizedDisplay("*Name")]
        public string SearchName { get; set; }

        [LocalizedDisplay("*Alias")]
        public string SearchAlias { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Alias")]
        public string Alias { get; set; }

        [LocalizedDisplay("*CollectionGroup")]
        public string CollectionGroupName { get; set; }

        [LocalizedDisplay("*Essential")]
        public bool Essential { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*AllowFiltering")]
        public bool AllowFiltering { get; set; } = true;

        [LocalizedDisplay("*ShowOnProductPage")]
        public bool ShowOnProductPage { get; set; } = true;

        [LocalizedDisplay("*FacetSorting")]
        public FacetSorting FacetSorting { get; set; }
        [LocalizedDisplay("*FacetSorting")]
        public string LocalizedFacetSorting { get; set; }

        [LocalizedDisplay("*FacetTemplateHint")]
        public FacetTemplateHint FacetTemplateHint { get; set; }
        [LocalizedDisplay("*FacetTemplateHint")]
        public string LocalizedFacetTemplateHint { get; set; }

        [LocalizedDisplay("*IndexOptionNames")]
        public bool IndexOptionNames { get; set; }

        [LocalizedDisplay("Admin.Catalog.Attributes.SpecificationAttributes.Options")]
        public int NumberOfOptions { get; set; }

        public List<SpecificationAttributeLocalizedModel> Locales { get; set; } = [];

        public string EditUrl { get; set; }
    }

    [LocalizedDisplay("Admin.Catalog.Attributes.SpecificationAttributes.Fields.")]
    public class SpecificationAttributeLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Alias")]
        public string Alias { get; set; }
    }

    public partial class SpecificationAttributeValidator : SmartValidator<SpecificationAttributeModel>
    {
        public SpecificationAttributeValidator(SmartDbContext db)
        {
            ApplyEntityRules<SpecificationAttribute>(db);
        }
    }

    public class SpecificationAttributeMapper(SmartDbContext db) :
        IMapper<SpecificationAttribute, SpecificationAttributeModel>,
        IMapper<SpecificationAttributeModel, SpecificationAttribute>
    {
        private readonly SmartDbContext _db = db;

        public async Task MapAsync(SpecificationAttribute from, SpecificationAttributeModel to, dynamic parameters = null)
        {
            await _db.LoadReferenceAsync(from, x => x.CollectionGroupMapping, false, q => q.Include(x => x.CollectionGroup));

            MiniMapper.Map(from, to);
            to.CollectionGroupName = from.CollectionGroupMapping?.CollectionGroup?.Name;
        }

        public Task MapAsync(SpecificationAttributeModel from, SpecificationAttribute to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);

            return Task.CompletedTask;
        }
    }
}

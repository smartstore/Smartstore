using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Common.Services;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.")]
    public class SpecificationAttributeOptionModel : EntityModelBase, ILocalizedModel<SpecificationAttributeOptionLocalizedModel>
    {
        public Type GetEntityType() => typeof(SpecificationAttributeOption);

        public int SpecificationAttributeId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Alias")]
        public string Alias { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*Multiple")]
        public bool Multiple { get; set; }

        [LocalizedDisplay("*NumberValue")]
        public decimal NumberValue { get; set; }

        [LocalizedDisplay("*ColorSquaresRgb")]
        [UIHint("Color")]
        public string Color { get; set; } = string.Empty;

        [UIHint("Media")]
        [AdditionalMetadata("album", "catalog"), AdditionalMetadata("transientUpload", true), AdditionalMetadata("entityType", "SpecificationAttributeOption")]
        [LocalizedDisplay("*Picture")]
        public int PictureId { get; set; }

        [LocalizedDisplay("*CollectionGroup")]
        public string CollectionGroupName { get; set; }

        public List<SpecificationAttributeOptionLocalizedModel> Locales { get; set; } = [];
    }

    [LocalizedDisplay("Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.")]
    public class SpecificationAttributeOptionLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Alias")]
        public string Alias { get; set; }
    }

    public partial class SpecificationAttributeOptionValidator : AbstractValidator<SpecificationAttributeOptionModel>
    {
        public SpecificationAttributeOptionValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    public class SpecificationAttributeOptionMapper :
        IMapper<SpecificationAttributeOption, SpecificationAttributeOptionModel>,
        IMapper<SpecificationAttributeOptionModel, SpecificationAttributeOption>
    {
        private readonly ICollectionGroupService _collectionGroupService;

        public SpecificationAttributeOptionMapper(ICollectionGroupService collectionGroupService)
        {
            _collectionGroupService = collectionGroupService;
        }

        public Task MapAsync(SpecificationAttributeOption from, SpecificationAttributeOptionModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.PictureId = from.MediaFileId;
            to.CollectionGroupName = from.CollectionGroup?.Name;

            return Task.CompletedTask;
        }

        public async Task MapAsync(SpecificationAttributeOptionModel from, SpecificationAttributeOption to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.MediaFileId = from.PictureId;

            await _collectionGroupService.ApplyCollectionGroupNameAsync(to, from.CollectionGroupName);
        }
    }
}

using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Attributes.SpecificationAttributes.Options.Fields.")]
    public class SpecificationAttributeOptionModel : EntityModelBase, ILocalizedModel<SpecificationAttributeOptionLocalizedModel>
    {
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

        public List<SpecificationAttributeOptionLocalizedModel> Locales { get; set; } = new();
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

    [Mapper(Lifetime = ServiceLifetime.Singleton)]
    public class SpecificationAttributeOptionMapper :
        IMapper<SpecificationAttributeOption, SpecificationAttributeOptionModel>,
        IMapper<SpecificationAttributeOptionModel, SpecificationAttributeOption>
    {
        public Task MapAsync(SpecificationAttributeOptionModel from, SpecificationAttributeOption to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.MediaFileId = from.PictureId;

            return Task.CompletedTask;
        }

        public Task MapAsync(SpecificationAttributeOption from, SpecificationAttributeOptionModel to, dynamic parameters = null)
        {
            MiniMapper.Map(from, to);
            to.PictureId = from.MediaFileId;

            return Task.CompletedTask;
        }
    }
}

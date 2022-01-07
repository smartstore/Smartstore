using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.Admin.Models.Common
{
    [LocalizedDisplay("Admin.Configuration.Measures.Dimensions.Fields.")]
    public class MeasureDimensionModel : EntityModelBase, ILocalizedModel<MeasureDimensionLocalizedModel>
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*SystemKeyword")]
        public string SystemKeyword { get; set; }

        [LocalizedDisplay("*Ratio")]
        [UIHint("Decimal")]
        [AdditionalMetadata("decimals", 8)]
        public decimal Ratio { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*IsPrimaryWeight")]
        public bool IsPrimaryDimension { get; set; }

        public List<MeasureDimensionLocalizedModel> Locales { get; set; } = new();
    }

    public class MeasureDimensionLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Measures.Weights.Fields.Name")]
        public string Name { get; set; }
    }

    public partial class MeasureDimensionValidator : AbstractValidator<MeasureDimensionModel>
    {
        public MeasureDimensionValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.SystemKeyword).NotEmpty();
        }
    }
}

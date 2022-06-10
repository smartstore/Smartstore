using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.Admin.Models.Common
{
    [LocalizedDisplay("Admin.Configuration.Measures.Weights.Fields.")]
    public class MeasureWeightModel : EntityModelBase, ILocalizedModel<MeasureWeightLocalizedModel>
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
        public bool IsPrimaryWeight { get; set; }

        public List<MeasureWeightLocalizedModel> Locales { get; set; } = new();
    }

    public class MeasureWeightLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Measures.Weights.Fields.Name")]
        public string Name { get; set; }
    }

    public partial class MeasureWeightValidator : AbstractValidator<MeasureWeightModel>
    {
        public MeasureWeightValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.SystemKeyword).NotEmpty();
        }
    }
}

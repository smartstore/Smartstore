using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Models.Directory
{
    public class MeasureDimensionModel : EntityModelBase, ILocalizedModel<MeasureDimensionLocalizedModel>
    {
        [LocalizedDisplay("Admin.Configuration.Measures.Dimensions.Fields.Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Admin.Configuration.Measures.Dimensions.Fields.SystemKeyword")]
        public string SystemKeyword { get; set; }

        [LocalizedDisplay("Admin.Configuration.Measures.Dimensions.Fields.Ratio")]
        [UIHint("Decimal")]
        [AdditionalMetadata("decimals", 8)]
        public decimal Ratio { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Admin.Configuration.Measures.Dimensions.Fields.IsPrimaryWeight")]
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

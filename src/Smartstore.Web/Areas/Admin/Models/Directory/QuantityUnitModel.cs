using FluentValidation;

namespace Smartstore.Admin.Models.Common
{
    [LocalizedDisplay("Admin.Configuration.QuantityUnit.Fields.")]
    public class QuantityUnitModel : EntityModelBase, ILocalizedModel<QuantityUnitLocalizedModel>
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*NamePlural")]
        public string NamePlural { get; set; }

        [LocalizedDisplay("Common.Description")]
        public string Description { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("*IsDefault")]
        public bool IsDefault { get; set; }

        public List<QuantityUnitLocalizedModel> Locales { get; set; } = new();
    }

    [LocalizedDisplay("Admin.Configuration.QuantityUnit.Fields.")]
    public class QuantityUnitLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*NamePlural")]
        public string NamePlural { get; set; }

        [LocalizedDisplay("Common.Description")]
        public string Description { get; set; }
    }

    public partial class QuantityUnitValidator : AbstractValidator<QuantityUnitModel>
    {
        public QuantityUnitValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.NamePlural).NotEmpty();
        }
    }
}

using FluentValidation;

namespace Smartstore.Admin.Models.Common
{
    [LocalizedDisplay("Admin.Configuration.PriceLabel.Fields.")]
    public class PriceLabelModel : EntityModelBase, ILocalizedModel<PriceLabelLocalizedModel>
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*ShortName")]
        public string ShortName { get; set; }

        [LocalizedDisplay("*Description")]
        public string Description { get; set; }

        [LocalizedDisplay("*IsRetailPrice")]
        public bool IsRetailPrice { get; set; } = false;

        [LocalizedDisplay("*DisplayShortNameInLists")]
        public bool DisplayShortNameInLists { get; set; } = false;

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Admin.Configuration.PriceLabel.IsDefaultComparePriceLabel")]
        public bool IsDefaultComparePriceLabel { get; set; } = false;

        [LocalizedDisplay("Admin.Configuration.PriceLabel.IsDefaultRegularPriceLabel")]
        public bool IsDefaultRegularPriceLabel { get; set; } = false;
        public List<PriceLabelLocalizedModel> Locales { get; set; } = new();
    }

    [LocalizedDisplay("Admin.Configuration.PriceLabel.Fields.")]
    public class PriceLabelLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*ShortName")]
        public string ShortName { get; set; }

        [LocalizedDisplay("*Description")]
        public string Description { get; set; }
    }

    public partial class PriceLabelValidator : AbstractValidator<PriceLabelModel>
    {
        public PriceLabelValidator()
        {
            RuleFor(x => x.ShortName).NotEmpty().MaximumLength(16);
            RuleFor(x => x.Name).MaximumLength(50);
            RuleFor(x => x.Description).MaximumLength(400);
        }
    }
}

using FluentValidation;

namespace Smartstore.Admin.Models.Catalog
{
    [LocalizedDisplay("Admin.Catalog.Products.GroupedProductConfiguration.")]
    public class GroupedProductConfigurationModel : EntityModelBase, ILocalizedModel<GroupedProductConfigurationLocalizedModel>
    {
        [LocalizedDisplay("*Title")]
        public string Title { get; set; }
        public string DefaultTitle { get; set; }

        [LocalizedDisplay("*PageSize")]
        public int? PageSize { get; set; }

        [LocalizedDisplay("*SearchMinAssociatedCount")]
        public int? SearchMinAssociatedCount { get; set; }

        [LocalizedDisplay("*Collapsible")]
        public bool? Collapsible { get; set; }

        [LocalizedDisplay("*HeaderFields")]
        public string[] HeaderFields { get; set; }

        public List<GroupedProductConfigurationLocalizedModel> Locales { get; set; } = [];
    }

    [LocalizedDisplay("Admin.Catalog.Products.GroupedProductConfiguration.")]
    public class GroupedProductConfigurationLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Title")]
        public string Title { get; set; }
        public string DefaultTitle { get; set; }
    }

    public partial class GroupedProductConfigurationModelValidator : SmartValidator<GroupedProductConfigurationModel>
    {
        public GroupedProductConfigurationModelValidator()
        {
            RuleFor(x => x.PageSize)
                .GreaterThan(0)
                .When(x => x.PageSize != null);

            RuleFor(x => x.SearchMinAssociatedCount)
                .GreaterThanOrEqualTo(0)
                .When(x => x.SearchMinAssociatedCount != null);
        }
    }
}

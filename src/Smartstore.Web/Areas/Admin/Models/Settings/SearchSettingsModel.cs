using FluentValidation;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.Search.")]
    public partial class SearchSettingsModel : TabbableModel
    {
        public string SearchFieldsNote { get; set; }
        public bool IsMegaSearchInstalled { get; set; }

        [LocalizedDisplay("*SearchMode")]
        public SearchMode SearchMode { get; set; }

        [LocalizedDisplay("*SearchFields")]
        public List<string> SearchFields { get; set; }

        [LocalizedDisplay("*InstantSearchEnabled")]
        public bool InstantSearchEnabled { get; set; }

        [LocalizedDisplay("*ShowProductImagesInInstantSearch")]
        public bool ShowProductImagesInInstantSearch { get; set; }

        [LocalizedDisplay("*InstantSearchNumberOfHits")]
        public int InstantSearchNumberOfProducts { get; set; }

        [LocalizedDisplay("*InstantSearchTermMinLength")]
        public int InstantSearchTermMinLength { get; set; }

        [LocalizedDisplay("*FilterMinHitCount")]
        public int FilterMinHitCount { get; set; }

        [LocalizedDisplay("*FilterMaxChoicesCount")]
        public int FilterMaxChoicesCount { get; set; }

        [LocalizedDisplay("*DefaultSortOrder")]
        public ProductSortingEnum DefaultSortOrder { get; set; }

        [LocalizedDisplay("*SearchProductByIdentificationNumber")]
        public bool SearchProductByIdentificationNumber { get; set; }

        public CommonFacetSettingsModel CategoryFacet { get; set; } = new();
        public CommonFacetSettingsModel BrandFacet { get; set; } = new();
        public CommonFacetSettingsModel PriceFacet { get; set; } = new();
        public CommonFacetSettingsModel RatingFacet { get; set; } = new();
        public CommonFacetSettingsModel DeliveryTimeFacet { get; set; } = new();
        public CommonFacetSettingsModel AvailabilityFacet { get; set; } = new();
        public CommonFacetSettingsModel NewArrivalsFacet { get; set; } = new();
    }

    public class CommonFacetSettingsModel : ModelBase, ILocalizedModel<CommonFacetSettingsLocalizedModel>
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Common.Deactivated")]
        public bool Disabled { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Admin.Configuration.Settings.Search.IncludeNotAvailable")]
        public bool IncludeNotAvailable { get; set; }

        public List<CommonFacetSettingsLocalizedModel> Locales { get; set; } = new();
    }

    public class CommonFacetSettingsLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Settings.Search.CommonFacet.Alias")]
        public string Alias { get; set; }
    }

    public partial class SearchSettingValidator : SettingModelValidator<SearchSettingsModel, SearchSettings>
    {
        const int MaxInstantSearchItems = 16;

        public SearchSettingValidator(Localizer T)
        {
            RuleFor(x => x.InstantSearchNumberOfProducts)
                .Must(x => x >= 1 && x <= MaxInstantSearchItems)
                //.When(x => (StoreScope == 0 && x.InstantSearchEnabled) || (StoreScope > 0 && IsOverrideChecked("InstantSearchNumberOfProducts")))
                .WhenSettingOverriden((m, x) => m.InstantSearchEnabled)
                .WithMessage(T("Admin.Validation.ValueRange", 1, MaxInstantSearchItems));

            RuleFor(x => x.InstantSearchTermMinLength).GreaterThan(0);
            RuleFor(x => x.FilterMinHitCount).GreaterThanOrEqualTo(0);
            RuleFor(x => x.FilterMaxChoicesCount).GreaterThan(0);
        }
    }
}

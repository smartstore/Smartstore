using FluentValidation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Web.Modelling;
using System;
using System.Collections.Generic;

namespace Smartstore.Admin.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.Search.")]
    public partial class SearchSettingsModel : ModelBase
    {
        public string SearchFieldsNote { get; set; }
        public bool IsMegaSearchInstalled { get; set; }

        [LocalizedDisplay("*SearchMode")]
        public SearchMode SearchMode { get; set; }
        public List<SelectListItem> AvailableSearchModes { get; set; }

        [LocalizedDisplay("*SearchFields")]
        public List<string> SearchFields { get; set; }
        public List<SelectListItem> AvailableSearchFields { get; set; } = new();

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

        // TODO: (mh) (core) Must be injected by Forum module.
        // Property name must equal settings class name.
        //public ForumSearchSettingsModel ForumSearchSettings { get; set; }
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

    public class SearchSettingValidator : AbstractValidator<SearchSettingsModel>
    {
        public static int MaxInstantSearchItems => 16;

        // TODO: (mh) (core) Throws with "Cannot resolve parameter addRule"
        // RE: Because "Func<string, bool>" is an unknown IoC dependency. We need to find a way to instantiate this class manually.
        public SearchSettingValidator(Localizer T/*, Func<string, bool> addRule*/)
        {
            //if (addRule("InstantSearchNumberOfProducts"))
            //{
                RuleFor(x => x.InstantSearchNumberOfProducts)
                    .Must(x => x >= 1 && x <= MaxInstantSearchItems)
                    .When(x => x.InstantSearchEnabled)
                    .WithMessage(T("Admin.Validation.ValueRange", 1, MaxInstantSearchItems));
            //}
        }
    }
}

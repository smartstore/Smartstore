using System.Collections.Generic;
using FluentValidation;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Forum.Domain;
using Smartstore.Web.Modelling;

namespace Smartstore.Forum.Models
{
    [CustomModelPart]
    [LocalizedDisplay("Admin.Configuration.Settings.Search.")]
    public class ForumSearchSettingsModel : ModelBase
    {
        [LocalizedDisplay("*SearchMode")]
        public SearchMode SearchMode { get; set; }

        [LocalizedDisplay("*Forum.SearchFields")]
        public List<string> SearchFields { get; set; }

        [LocalizedDisplay("*DefaultSortOrder")]
        public ForumTopicSorting DefaultSortOrder { get; set; }

        [LocalizedDisplay("*InstantSearchEnabled")]
        public bool InstantSearchEnabled { get; set; }

        [LocalizedDisplay("*InstantSearchNumberOfHits")]
        public int InstantSearchNumberOfHits { get; set; }

        [LocalizedDisplay("*InstantSearchTermMinLength")]
        public int InstantSearchTermMinLength { get; set; }

        [LocalizedDisplay("*FilterMinHitCount")]
        public int FilterMinHitCount { get; set; }

        [LocalizedDisplay("*FilterMaxChoicesCount")]
        public int FilterMaxChoicesCount { get; set; }

        public ForumFacetSettingsModel ForumFacet { get; set; } = new();
        public ForumFacetSettingsModel CustomerFacet { get; set; } = new();
        public ForumFacetSettingsModel DateFacet { get; set; } = new();
    }

    public class ForumFacetSettingsModel : ModelBase, ILocalizedModel<ForumFacetSettingsLocalizedModel>
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Common.Deactivated")]
        public bool Disabled { get; set; }

        [LocalizedDisplay("Common.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [LocalizedDisplay("Admin.Configuration.Settings.Search.IncludeNotAvailable")]
        public bool IncludeNotAvailable { get; set; }

        public List<ForumFacetSettingsLocalizedModel> Locales { get; set; } = new();
    }

    public class ForumFacetSettingsLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Settings.Search.CommonFacet.Alias")]
        public string Alias { get; set; }
    }

    //public class ForumSearchSettingValidator : AbstractValidator<ForumSearchSettingsModel>
    //{
    //    private const int MAX_INSTANT_SEARCH_ITEMS = 16;

    //    public ForumSearchSettingValidator(Localizer T)
    //    {
    //        RuleFor(x => x.InstantSearchNumberOfHits)
    //            .Must(x => x >= 1 && x <= MAX_INSTANT_SEARCH_ITEMS)
    //            .When(x => x.InstantSearchEnabled)
    //            .WithMessage(T("Admin.Validation.ValueRange", 1, MAX_INSTANT_SEARCH_ITEMS));
    //    }
    //}
}

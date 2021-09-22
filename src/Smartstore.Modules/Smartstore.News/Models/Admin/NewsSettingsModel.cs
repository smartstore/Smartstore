using System.Collections.Generic;
using Smartstore.Web.Modelling;

namespace Smartstore.News.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.News.")]
    public class NewsSettingsModel : ModelBase
    {
        [LocalizedDisplay("*Enabled")]
        public bool Enabled { get; set; }

        [LocalizedDisplay("*AllowNotRegisteredUsersToLeaveComments")]
        public bool AllowNotRegisteredUsersToLeaveComments { get; set; }

        [LocalizedDisplay("*NotifyAboutNewNewsComments")]
        public bool NotifyAboutNewNewsComments { get; set; }

        [LocalizedDisplay("*ShowNewsOnMainPage")]
        public bool ShowNewsOnMainPage { get; set; }

        [LocalizedDisplay("*MainPageNewsCount")]
        public int MainPageNewsCount { get; set; }

        [LocalizedDisplay("*NewsArchivePageSize")]
        public int NewsArchivePageSize { get; set; }

        [LocalizedDisplay("*MaxAgeInDays")]
        public int MaxAgeInDays { get; set; }

        [LocalizedDisplay("*ShowHeaderRSSUrl")]
        public bool ShowHeaderRssUrl { get; set; }

        public SeoModel SeoModel { get; set; } = new();
    }

    // INFO: Had to do it this way to call <editor asp-for="SeoModel" asp-template="SeoModel" />
    // TODO: (mh) (core) If this really is the way to do it better place this class once globally.
    public class SeoModel : ISeoModel
    {
        public string MetaTitle { get; set; }

        public string MetaDescription { get; set; }

        public string MetaKeywords { get; set; }

        public List<SeoModelLocal> Locales { get; set; } = new();
    }
}

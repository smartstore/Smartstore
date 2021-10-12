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
}

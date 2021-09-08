using System.Collections.Generic;
using Smartstore.Web.Modelling;

namespace Smartstore.Blog.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.Blog.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*Enabled")]
        public bool Enabled { get; set; }

        [LocalizedDisplay("*PostsPageSize")]
        public int PostsPageSize { get; set; }

        [LocalizedDisplay("*AllowNotRegisteredUsersToLeaveComments")]
        public bool AllowNotRegisteredUsersToLeaveComments { get; set; }

        [LocalizedDisplay("*NotifyAboutNewBlogComments")]
        public bool NotifyAboutNewBlogComments { get; set; }

        [LocalizedDisplay("*NumberOfTags")]
        public int NumberOfTags { get; set; }

        [LocalizedDisplay("*MaxAgeInDays")]
        public int MaxAgeInDays { get; set; }

        [LocalizedDisplay("*ShowHeaderRSSUrl")]
        public bool ShowHeaderRssUrl { get; set; }

        public SeoModel SeoModel { get; set; }
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

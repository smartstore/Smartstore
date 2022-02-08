namespace Smartstore.Blog.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.Blog.")]
    public class BlogSettingsModel : ModelBase, ISeoModel
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

        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public List<SeoModelLocal> Locales { get; set; } = new();
    }
}
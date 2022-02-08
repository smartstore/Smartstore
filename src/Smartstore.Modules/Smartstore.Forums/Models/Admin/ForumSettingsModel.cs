namespace Smartstore.Forums.Models
{
    [LocalizedDisplay("Admin.Configuration.Settings.Forums.")]
    public class ForumSettingsModel : ModelBase, ISeoModel
    {
        [LocalizedDisplay("*ForumsEnabled")]
        public bool ForumsEnabled { get; set; }

        [LocalizedDisplay("*RelativeDateTimeFormattingEnabled")]
        public bool RelativeDateTimeFormattingEnabled { get; set; }

        [LocalizedDisplay("*ShowCustomersPostCount")]
        public bool ShowCustomersPostCount { get; set; }

        [LocalizedDisplay("*AllowGuestsToCreatePosts")]
        public bool AllowGuestsToCreatePosts { get; set; }

        [LocalizedDisplay("*AllowGuestsToCreateTopics")]
        public bool AllowGuestsToCreateTopics { get; set; }

        [LocalizedDisplay("*AllowCustomersToEditPosts")]
        public bool AllowCustomersToEditPosts { get; set; }

        [LocalizedDisplay("*AllowCustomersToDeletePosts")]
        public bool AllowCustomersToDeletePosts { get; set; }

        [LocalizedDisplay("*AllowCustomersToManageSubscriptions")]
        public bool AllowCustomersToManageSubscriptions { get; set; }

        [LocalizedDisplay("*TopicsPageSize")]
        public int TopicsPageSize { get; set; }

        [LocalizedDisplay("*PostsPageSize")]
        public int PostsPageSize { get; set; }

        [LocalizedDisplay("*SearchResultsPageSize")]
        public int SearchResultsPageSize { get; set; }

        [LocalizedDisplay("*AllowSorting")]
        public bool AllowSorting { get; set; }

        [LocalizedDisplay("*AllowCustomersToVoteOnPosts")]
        public bool AllowCustomersToVoteOnPosts { get; set; }

        [LocalizedDisplay("*AllowGuestsToVoteOnPosts")]
        public bool AllowGuestsToVoteOnPosts { get; set; }

        [LocalizedDisplay("*ForumEditor")]
        public EditorType ForumEditor { get; set; }

        [LocalizedDisplay("*SignaturesEnabled")]
        public bool SignaturesEnabled { get; set; }

        [LocalizedDisplay("*AllowPrivateMessages")]
        public bool AllowPrivateMessages { get; set; }

        [LocalizedDisplay("*ShowAlertForPM")]
        public bool ShowAlertForPM { get; set; }

        [LocalizedDisplay("*NotifyAboutPrivateMessages")]
        public bool NotifyAboutPrivateMessages { get; set; }

        [LocalizedDisplay("*ActiveDiscussionsFeedEnabled")]
        public bool ActiveDiscussionsFeedEnabled { get; set; }

        [LocalizedDisplay("*ActiveDiscussionsFeedCount")]
        public int ActiveDiscussionsFeedCount { get; set; }

        [LocalizedDisplay("*ForumFeedsEnabled")]
        public bool ForumFeedsEnabled { get; set; }

        [LocalizedDisplay("*ForumFeedCount")]
        public int ForumFeedCount { get; set; }
        public string MetaTitle { get; set; }
        public string MetaDescription { get; set; }
        public string MetaKeywords { get; set; }
        public List<SeoModelLocal> Locales { get; set; } = new();
    }
}

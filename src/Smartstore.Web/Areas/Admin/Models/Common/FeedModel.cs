namespace Smartstore.Admin.Models.Common
{
    [Serializable]
    public class FeedModel : ModelBase
    {
        public List<NewsFeedChannelModel> NewsFeedCannels { get; set; } = new();
        public string ErrorMessage { get; set; }
        public bool IsError { get; set; }
    }

    [Serializable]
    public class NewsFeedChannelModel
    {
        public string Name { get; set; }
        public string ButtonText { get; set; }
        public string ButtonLink { get; set; }
        public List<NewsFeedItemModel> NewsFeedItems { get; set; } = new();
    }

    [Serializable]
    public class NewsFeedItemModel
    {
        public string Title { get; set; }
        public string Short { get; set; }
        public string Link { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime PublishDate { get; set; }
        public bool IsPinned { get; set; }
    }
}

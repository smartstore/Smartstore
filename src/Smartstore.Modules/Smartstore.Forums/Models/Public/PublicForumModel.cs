using Smartstore.Collections;

namespace Smartstore.Forums.Models.Public
{
    public partial class PublicForumModel : EntityModelBase
    {
        public LocalizedValue<string> Name { get; set; }
        public LocalizedValue<string> Description { get; set; }
        public string Slug { get; set; }
        public int NumTopics { get; set; }
        public int NumPosts { get; set; }
        public int LastPostId { get; set; }

        public PublicForumPostModel LastPost { get; set; }
    }

    public partial class PublicForumPageModel : EntityModelBase
    {
        public LocalizedValue<string> Name { get; set; }
        public LocalizedValue<string> Description { get; set; }
        public string Slug { get; set; }

        public bool CanSubscribe { get; set; }
        public bool IsSubscribed { get; set; }
        public bool ForumFeedsEnabled { get; set; }

        public IPagedList<PublicForumTopicModel> ForumTopics { get; set; }
    }
}

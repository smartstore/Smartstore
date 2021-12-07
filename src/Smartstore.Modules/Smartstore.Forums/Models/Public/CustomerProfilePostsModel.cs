using Smartstore.Collections;

namespace Smartstore.Forums.Models.Public
{
    public partial class CustomerProfilePostsModel : EntityModelBase
    {
        public PagedList<CustomerProfilePostModel> LatestPosts { get; set; }
    }

    public partial class CustomerProfilePostModel : EntityModelBase
    {
        public int ForumTopicId { get; set; }
        public string ForumTopicSubject { get; set; }
        public string ForumTopicSlug { get; set; }
        public string ForumPostText { get; set; }
        public string PostCreatedOnStr { get; set; }
    }
}

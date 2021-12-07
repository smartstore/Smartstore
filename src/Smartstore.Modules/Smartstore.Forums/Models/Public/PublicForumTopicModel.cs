using Smartstore.Collections;
using Smartstore.Web.Models.Customers;

namespace Smartstore.Forums.Models.Public
{
    public partial class PublicForumTopicModel : EntityModelBase
    {
        public string Subject { get; set; }
        public string Slug { get; set; }
        public int FirstPostId { get; set; }
        public int LastPostId { get; set; }
        public bool Published { get; set; }

        public ForumTopicType ForumTopicType { get; set; }
        public int Views { get; set; }
        public int NumPosts { get; set; }
        public int NumReplies { get; set; }

        /// <remarks>
        /// Perf: no query, no data. Just paging information for a paging-like link list.
        /// </remarks>
        public IPageable PostsPages { get; set; }

        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public bool HasCustomerProfile { get; set; }

        public PublicForumPostModel LastPost { get; set; }
        public CustomerAvatarModel Avatar { get; set; }

        public string AnchorTag 
            => FirstPostId == 0 ? string.Empty : "#" + FirstPostId;
    }

    public partial class PublicForumTopicPageModel : EntityModelBase
    {
        public string Subject { get; set; }
        public string Slug { get; set; }

        public ForumModerationPermissionFlags ModerationPermissions { get; set; }
        public bool CanCreatePosts
        {
            get => ModerationPermissions.HasFlag(ForumModerationPermissionFlags.CanCreatePosts);
        }
        public bool CanEditTopic
        {
            get => ModerationPermissions.HasFlag(ForumModerationPermissionFlags.CanEditTopic);
        }
        public bool CanMoveTopic
        {
            get => ModerationPermissions.HasFlag(ForumModerationPermissionFlags.CanMoveTopic);
        }
        public bool CanDeleteTopic
        {
            get => ModerationPermissions.HasFlag(ForumModerationPermissionFlags.CanDeleteTopic);
        }

        public bool CanSubscribe { get; set; }
        public bool IsSubscribed { get; set; }

        public IPagedList<PublicForumPostModel> ForumPosts { get; set; }
    }
}

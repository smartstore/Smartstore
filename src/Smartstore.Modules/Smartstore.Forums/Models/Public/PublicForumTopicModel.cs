using System.Collections.Generic;
using Smartstore.Collections;
using Smartstore.Forums.Domain;
using Smartstore.Web.Modelling;
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
        public int NumPosts { get; set; }
        public int Views { get; set; }
        public int NumReplies { get; set; }

        public int PostsPageSize { get; set; }
        public int TotalPostPages 
            => PostsPageSize != 0 ? (NumPosts / PostsPageSize) + 1 : 1;

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

        public ForumModerationPermits ModerationPermits { get; set; }
        public bool CanEditTopic
        {
            get => ModerationPermits.HasFlag(ForumModerationPermits.CanEditTopic);
        }
        public bool CanMoveTopic
        {
            get => ModerationPermits.HasFlag(ForumModerationPermits.CanMoveTopic);
        }
        public bool CanDeleteTopic
        {
            get => ModerationPermits.HasFlag(ForumModerationPermits.CanDeleteTopic);
        }

        public bool CanSubscribe { get; set; }
        public bool IsSubscribed { get; set; }

        public IPagedList<PublicForumPostModel> ForumPosts { get; set; }
    }
}

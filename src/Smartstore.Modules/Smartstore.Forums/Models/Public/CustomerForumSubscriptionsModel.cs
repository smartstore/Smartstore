using Smartstore.Collections;
using Smartstore.Web.Modelling;

namespace Smartstore.Forums.Models.Public
{
    public partial class CustomerForumSubscriptionsModel : ModelBase
    {
        public PagedList<CustomerForumSubscriptionModel> ForumSubscriptions { get; set; }
    }

    public partial class CustomerForumSubscriptionModel : EntityModelBase
    {
        public int ForumId { get; set; }
        public int ForumTopicId { get; set; }
        public bool TopicSubscription { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
    }
}

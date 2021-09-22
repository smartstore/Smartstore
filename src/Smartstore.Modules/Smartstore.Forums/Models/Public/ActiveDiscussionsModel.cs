using System.Collections.Generic;

namespace Smartstore.Forums.Models.Public
{
    public partial class ActiveDiscussionsModel
    {
        public bool IsForumGroupsPage { get; set; }
        public bool ActiveDiscussionsFeedEnabled { get; set; }
        public int PostsPageSize { get; set; }

        public List<PublicForumTopicModel> ForumTopics { get; set; }
    }
}

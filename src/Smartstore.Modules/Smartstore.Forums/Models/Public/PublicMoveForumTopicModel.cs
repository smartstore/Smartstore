using Smartstore.Forums.Domain;
using Smartstore.Web.Modelling;

namespace Smartstore.Forums.Models.Public
{
    public partial class PublicMoveForumTopicModel : EntityModelBase
    {
        public int CustomerId { get; set; }
        public int SelectedForumId { get; set; }
        public string TopicSlug { get; set; }

        public ForumModerationPermits ModerationPermits { get; set; }
        public bool CanMoveTopic
        {
            get => ModerationPermits.HasFlag(ForumModerationPermits.CanMoveTopic);
        }
    }
}

namespace Smartstore.Forums.Models.Public
{
    public partial class PublicMoveForumTopicModel : EntityModelBase
    {
        public int CustomerId { get; set; }
        public int SelectedForumId { get; set; }
        public string TopicSlug { get; set; }

        public ForumModerationPermissionFlags ModerationPermissions { get; set; }
        public bool CanMoveTopic
        {
            get => ModerationPermissions.HasFlag(ForumModerationPermissionFlags.CanMoveTopic);
        }
    }
}

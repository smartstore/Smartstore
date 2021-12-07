namespace Smartstore.Forums.Models.Public
{
    public partial class PublicEditForumPostModel : EntityModelBase
    {
        public int ForumTopicId { get; set; }
        public bool Published { get; set; }
        public bool DisplayCaptcha { get; set; }
        public bool IsFirstPost { get; set; }

        [SanitizeHtml]
        public string Text { get; set; }
        public EditorType ForumEditor { get; set; }

        public LocalizedValue<string> ForumName { get; set; }
        public int ForumId { get; set; }
        public string ForumSlug { get; set; }
        public string ForumTopicSubject { get; set; }
        public string ForumTopicSlug { get; set; }

        public int CustomerId { get; set; }
        public bool IsModerator { get; set; }
        public bool CanSubscribe { get; set; }
        public bool IsSubscribed { get; set; }

        public ForumModerationPermissionFlags ModerationPermissions { get; set; }

        public bool CanCreatePosts
        {
            get => ModerationPermissions.HasFlag(ForumModerationPermissionFlags.CanCreatePosts);
        }
        public bool CanEditPost
        {
            get => ModerationPermissions.HasFlag(ForumModerationPermissionFlags.CanEditPost);
        }
        public bool CanDeletePost
        {
            get => ModerationPermissions.HasFlag(ForumModerationPermissionFlags.CanDeletePost);
        }
    }

    public class EditForumPostValidator : AbstractValidator<PublicEditForumPostModel>
    {
        public EditForumPostValidator()
        {
            RuleFor(x => x.Text).NotEmpty();
        }
    }
}

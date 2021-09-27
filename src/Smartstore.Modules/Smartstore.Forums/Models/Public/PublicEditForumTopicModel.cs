using FluentValidation;
using Smartstore.Core.Localization;
using Smartstore.Forums.Domain;
using Smartstore.Web.Modelling;

namespace Smartstore.Forums.Models.Public
{
    public partial class PublicEditForumTopicModel : EntityModelBase
    {
        public bool DisplayCaptcha { get; set; }
        public bool Published { get; set; }
        public string Slug { get; set; }

        public int ForumId { get; set; }
        public LocalizedValue<string> ForumName { get; set; }
        public string ForumSlug { get; set; }

        public int TopicTypeId { get; set; }
        public EditorType ForumEditor { get; set; }

        public string Subject { get; set; }

        [SanitizeHtml]
        public string Text { get; set; }

        public int CustomerId { get; set; }
        public bool IsModerator { get; set; }
        public bool CanSubscribe { get; set; }
        public bool IsSubscribed { get; set; }

        public ForumModerationPermits ModerationPermits { get; set; }
        public bool CanEditTopic
        {
            get => ModerationPermits.HasFlag(ForumModerationPermits.CanEditTopic);
        }
    }

    public class EditForumTopicValidator : AbstractValidator<PublicEditForumTopicModel>
    {
        public EditForumTopicValidator()
        {
            RuleFor(x => x.Subject).NotEmpty();
            RuleFor(x => x.Text).NotEmpty();
        }
    }
}

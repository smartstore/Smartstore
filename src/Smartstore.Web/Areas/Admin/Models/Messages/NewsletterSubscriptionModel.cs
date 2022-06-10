using FluentValidation;

namespace Smartstore.Admin.Models.Messages
{
    public class NewsletterSubscriptionModel : EntityModelBase
    {
        [LocalizedDisplay("Admin.Promotions.NewsletterSubscriptions.Fields.Email")]
        public string Email { get; set; }

        [LocalizedDisplay("Admin.Promotions.NewsletterSubscriptions.Fields.Active")]
        public bool Active { get; set; }

        [LocalizedDisplay("Common.CreatedOn")]
        public DateTime CreatedOn { get; set; }

        [LocalizedDisplay("Admin.Common.Store")]
        public string StoreName { get; set; }
    }

    public partial class NewsletterSubscriptionValidator : AbstractValidator<NewsletterSubscriptionModel>
    {
        public NewsletterSubscriptionValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }
}

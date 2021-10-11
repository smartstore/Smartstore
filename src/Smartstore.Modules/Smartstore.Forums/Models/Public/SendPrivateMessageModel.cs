using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Web.Modelling;

namespace Smartstore.Forums.Models.Public
{
    [LocalizedDisplay("Admin.Customers.Customers.SendPM.")]
    public partial class SendPrivateMessageModel : EntityModelBase
    {
        public int ToCustomerId { get; set; }

        [LocalizedDisplay("PrivateMessages.Send.To")]
        public string CustomerToName { get; set; }
        public bool HasCustomerProfile { get; set; }

        public int ReplyToMessageId { get; set; }

        [LocalizedDisplay("*Subject")]
        public string Subject { get; set; }

        [SanitizeHtml]
        [LocalizedDisplay("*Message")]
        [UIHint("Textarea")]
        [AdditionalMetadata("rows", 6)]
        public string Message { get; set; }
    }

    public class SendPrivateMessageValidator : AbstractValidator<SendPrivateMessageModel>
    {
        public SendPrivateMessageValidator()
        {
            RuleFor(x => x.Subject).NotEmpty();
            RuleFor(x => x.Message).NotEmpty();
        }
    }
}

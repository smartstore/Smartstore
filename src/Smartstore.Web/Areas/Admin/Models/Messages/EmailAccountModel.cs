using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Net.Mail;

namespace Smartstore.Admin.Models.Messages
{
    [LocalizedDisplay("Admin.Configuration.EmailAccounts.Fields.")]
    public class EmailAccountModel : EntityModelBase
    {
        [LocalizedDisplay("*Email")]
        public string Email { get; set; }

        [LocalizedDisplay("*DisplayName")]
        public string DisplayName { get; set; }

        [LocalizedDisplay("*Host")]
        public string Host { get; set; }

        [LocalizedDisplay("*Port")]
        [AdditionalMetadata("invariant", true)]
        [AdditionalMetadata("min", 0)]
        [AdditionalMetadata("max", 65535)]
        public int Port { get; set; }

        [LocalizedDisplay("*Username")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Username { get; set; }

        [LocalizedDisplay("*Password")]
        [DataType(DataType.Password)]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Password { get; set; }

        [LocalizedDisplay("*MailSecureOption")]
        public MailSecureOption MailSecureOption { get; set; } = MailSecureOption.Auto;

        [LocalizedDisplay("*UseDefaultCredentials")]
        public bool UseDefaultCredentials { get; set; }

        [LocalizedDisplay("*IsDefaultEmailAccount")]
        public bool IsDefaultEmailAccount { get; set; }

        [LocalizedDisplay("*SendTestEmailTo")]
        public string SendTestEmailTo { get; set; }
        public string EditUrl { get; set; }
        public string TestEmailShortErrorMessage { get; set; }
        public string TestEmailFullErrorMessage { get; set; }
    }

    public partial class EmailAccountValidator : AbstractValidator<EmailAccountModel>
    {
        public EmailAccountValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddressStrict();
            RuleFor(x => x.DisplayName).NotEmpty();
            RuleFor(x => x.Host).NotEmpty();

            // INFO: do not validate Username or Password. A server sometimes does not need them even if UseDefaultCredentials is disabled.
        }
    }
}
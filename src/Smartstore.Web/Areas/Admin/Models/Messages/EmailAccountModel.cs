using System.ComponentModel.DataAnnotations;
using FluentValidation;

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
        public int Port { get; set; }

        [LocalizedDisplay("*Username")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Username { get; set; }

        [LocalizedDisplay("*Password")]
        [DataType(DataType.Password)]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        public string Password { get; set; }

        [LocalizedDisplay("*EnableSsl")]
        public bool EnableSsl { get; set; }

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
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.DisplayName).NotEmpty();
            RuleFor(x => x.Host).NotEmpty();
        }
    }
}
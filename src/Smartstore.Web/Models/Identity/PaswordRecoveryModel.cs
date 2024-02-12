using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.Web.Models.Identity
{
    public partial class PasswordRecoveryModel : ModelBase
    {
        [LocalizedDisplay("Account.PasswordRecovery.Email")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        public string ResultMessage { get; set; }
        public PasswordRecoveryResultState ResultState { get; set; }

        public bool DisplayCaptcha { get; set; }
    }

    public enum PasswordRecoveryResultState
    {
        Success,
        Error
    }

    public class PasswordRecoveryValidator : AbstractValidator<PasswordRecoveryModel>
    {
        public PasswordRecoveryValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
        }
    }
}

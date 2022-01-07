using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Identity
{
    public partial class PasswordRecoveryConfirmModel : ModelBase
    {
        [DataType(DataType.Password)]
        [LocalizedDisplay("Account.PasswordRecovery.NewPassword")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [LocalizedDisplay("Account.PasswordRecovery.ConfirmNewPassword")]
        public string ConfirmNewPassword { get; set; }

        public string Token { get; set; }

        public string Email { get; set; }

        public bool SuccessfullyChanged { get; set; }
        public string Result { get; set; }
    }

    public class PasswordRecoveryConfirmValidator : AbstractValidator<PasswordRecoveryConfirmModel>
    {
        public PasswordRecoveryConfirmValidator(Localizer T, CustomerSettings customerSettings)
        {
            RuleFor(x => x.NewPassword)
                .NotEmpty()
                .Length(customerSettings.PasswordMinLength, 999);

            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty()
                .Equal(x => x.NewPassword)
                .WithMessage(T("Identity.Error.PasswordMismatch"));
        }
    }
}

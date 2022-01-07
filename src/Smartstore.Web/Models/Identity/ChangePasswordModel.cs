using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Models.Identity
{
    [LocalizedDisplay("Account.ChangePassword.Fields.")]
    public partial class ChangePasswordModel : ModelBase
    {
        [DataType(DataType.Password)]
        [LocalizedDisplay("*OldPassword")]
        public string OldPassword { get; set; }

        [DataType(DataType.Password)]
        [LocalizedDisplay("*NewPassword")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [LocalizedDisplay("*ConfirmNewPassword")]
        public string ConfirmNewPassword { get; set; }

        public string Result { get; set; }
    }

    public class ChangePasswordValidator : AbstractValidator<ChangePasswordModel>
    {
        public ChangePasswordValidator(Localizer T)
        {
            RuleFor(x => x.OldPassword).NotEmpty();
            RuleFor(x => x.ConfirmNewPassword)
                .NotEmpty()
                .Equal(x => x.NewPassword)
                .WithMessage(T("Identity.Error.PasswordMismatch"));
        }
    }
}

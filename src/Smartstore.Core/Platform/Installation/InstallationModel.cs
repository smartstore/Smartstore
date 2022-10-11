using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace Smartstore.Core.Installation
{
    public partial class InstallationModel
    {
        [DataType(DataType.EmailAddress)]
        public string AdminEmail { get; set; }

        [DataType(DataType.Password)]
        public string AdminPassword { get; set; }

        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        public string DataProvider { get; set; } // sqlserver | mysql

        public string PrimaryLanguage { get; set; } // ISO code

        public string MediaStorage { get; set; } // db | fs

        public bool CreateDatabase { get; set; } = true;

        public string DbServer { get; set; }

        public string DbName { get; set; }

        public string DbUserId { get; set; }

        [DataType(DataType.Password)]
        public string DbPassword { get; set; }

        public bool UseRawConnectionString { get; set; }

        public string DbRawConnectionString { get; set; }

        public string DbAuthType { get; set; } = "sqlserver"; // sqlserver | windows

        public bool InstallSampleData { get; set; }

        #region AutoInstall

        public bool IsAutoInstall { get; set; }

        /// <summary>
        /// For auto-install
        /// </summary>
        public string CallbackUrl { get; set; }

        /// <summary>
        /// For auto-install. Passed to <see cref="CallbackUrl"/> to identify client.
        /// </summary>
        public string TenantId { get; set; }

        #endregion
    }

    public class InstallationModelValidator : AbstractValidator<InstallationModel>
    {
        public InstallationModelValidator(IInstallationService installService)
        {
            RuleFor(x => x.AdminEmail).NotEmpty().WithMessage(Res("AdminEmailRequired")).EmailAddress();
            RuleFor(x => x.AdminPassword).NotEmpty().WithMessage(Res("AdminPasswordRequired"));
            RuleFor(x => x.ConfirmPassword).NotEmpty().WithMessage(Res("ConfirmPasswordRequired"));
            RuleFor(x => x.AdminPassword).Equal(x => x.ConfirmPassword).WithMessage(Res("PasswordsDoNotMatch"));
            RuleFor(x => x.DataProvider).NotEmpty();
            RuleFor(x => x.PrimaryLanguage).NotEmpty();

            RuleFor(x => x.DbRawConnectionString).NotEmpty().When(x => x.UseRawConnectionString).WithMessage(Res("DbRawConnectionStringRequired"));
            RuleFor(x => x.DbServer).NotEmpty().When(x => !x.UseRawConnectionString).WithMessage(Res("DbServerRequired"));
            RuleFor(x => x.DbName).NotEmpty().When(x => !x.UseRawConnectionString).WithMessage(Res("DbNameRequired"));

            RuleFor(x => x.DbUserId).NotEmpty()
                .When(x => !x.UseRawConnectionString && x.DbAuthType != "windows")
                .WithMessage(Res("DbUserIdRequired"));

            string Res(string key)
            {
                return installService.GetResource(key);
            }
        }
    }
}

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

        public string DataProvider { get; set; } = "sqlserver";

        public string DatabaseConnectionString { get; set; }

        public string PrimaryLanguage { get; set; }

        public string MediaStorage { get; set; }

        public bool CreateDatabase { get; set; } = true;

        public bool UseCustomCollation { get; set; }

        public string Collation { get; set; } = "SQL_Latin1_General_CP1_CI_AS";

        public bool InstallSampleData { get; set; }

        #region SqlServer

        public string SqlConnectionInfo { get; set; } = "sqlconnectioninfo_values";

        public string SqlServerName { get; set; }

        public string SqlDatabaseName { get; set; }

        public string SqlServerUsername { get; set; }

        [DataType(DataType.Password)]
        public string SqlServerPassword { get; set; }

        public string SqlAuthenticationType { get; set; } = "sqlauthentication";

        #endregion

        #region MySql

        // ...

        #endregion
    }

    public class InstallationModelValidator : AbstractValidator<InstallationModel>
    {
        public InstallationModelValidator(IInstallationService installService)
        {
            RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress();
            RuleFor(x => x.AdminPassword).NotEmpty();
            RuleFor(x => x.ConfirmPassword).NotEmpty();
            RuleFor(x => x.AdminPassword).Equal(x => x.ConfirmPassword).WithMessage(installService.GetResource("PasswordsDoNotMatch"));
            RuleFor(x => x.DataProvider).NotEmpty();
            RuleFor(x => x.PrimaryLanguage).NotEmpty();
        }
    }
}

using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.DataExchange;
using Smartstore.Core.Localization;
using Smartstore.IO;

namespace Smartstore.Admin.Models.Export
{
    [LocalizedDisplay("Admin.DataExchange.Export.Deployment.")]
    public class ExportDeploymentModel : EntityModelBase
    {
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("Common.Enabled")]
        public bool Enabled { get; set; }

        [LocalizedDisplay("Common.Image")]
        public string ThumbnailUrl { get; set; }

        [LocalizedDisplay("*DeploymentType")]
        public ExportDeploymentType DeploymentType { get; set; }

        [LocalizedDisplay("*DeploymentType")]
        public string DeploymentTypeName { get; set; }

        [LocalizedDisplay("*Username")]
        public string Username { get; set; }

        [LocalizedDisplay("*Password")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [LocalizedDisplay("*Url")]
        public string Url { get; set; }

        [LocalizedDisplay("*HttpTransmissionType")]
        public ExportHttpTransmissionType HttpTransmissionType { get; set; }

        [LocalizedDisplay("*FileSystemPath")]
        public string FileSystemPath { get; set; }

        [LocalizedDisplay("*SubFolder")]
        public string SubFolder { get; set; }

        [LocalizedDisplay("*EmailAddresses")]
        public string[] EmailAddresses { get; set; }

        [LocalizedDisplay("*EmailSubject")]
        public string EmailSubject { get; set; }

        [LocalizedDisplay("*EmailAccountId")]
        public int EmailAccountId { get; set; }

        [LocalizedDisplay("*UseSsl")]
        public bool UseSsl { get; set; }

        public int ProfileId { get; set; }
        public bool CreateZip { get; set; }
        public string PublicFolderUrl { get; set; }
        public int FileCount { get; set; }
        public LastResultInfo LastResult { get; set; }

        public class LastResultInfo
        {
            public DateTime Execution { get; set; }
            public string ExecutionPretty { get; set; }
            public string Error { get; set; }

            public bool Succeeded => Error.IsEmpty();
        }
    }

    public partial class ExportDeploymentValidator : AbstractValidator<ExportDeploymentModel>
    {
        public ExportDeploymentValidator(Localizer T)
        {
            RuleFor(x => x.Name).NotEmpty();

            RuleFor(x => x.EmailAddresses)
                .NotEmpty()
                .When(x => x.DeploymentType == ExportDeploymentType.Email);

            RuleFor(x => x.Url)
                .NotEmpty()
                .When(x => x.DeploymentType == ExportDeploymentType.Http || x.DeploymentType == ExportDeploymentType.Ftp);

            RuleFor(x => x.Username)
                .NotEmpty()
                .When(x => x.DeploymentType == ExportDeploymentType.Ftp);

            //RuleFor(x => x.Password)
            //	.NotEmpty()
            //	.When(x => x.DeploymentType == ExportDeploymentType.Ftp);

            RuleFor(x => x.FileSystemPath)
                .Must(x =>
                {
                    var isValidPath =
                        x.HasValue() &&
                        !x.EqualsNoCase("con") &&
                        x != "~/" &&
                        x != "~" &&
                        !PathUtility.HasInvalidPathChars(x.AsSpan());

                    return isValidPath;
                })
                .When(x => x.DeploymentType == ExportDeploymentType.FileSystem)
                .WithMessage(x => T("Admin.Validation.InvalidPath").Value.FormatInvariant(x.FileSystemPath.EmptyNull()));
        }
    }
}

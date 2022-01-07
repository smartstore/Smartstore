using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smartstore.Core.DataExchange
{
    internal class ExportDeploymentMap : IEntityTypeConfiguration<ExportDeployment>
    {
        public void Configure(EntityTypeBuilder<ExportDeployment> builder)
        {
            builder.HasOne(c => c.Profile)
                .WithMany(c => c.Deployments)
                .HasForeignKey(c => c.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents an export deployment.
    /// </summary>
    public partial class ExportDeployment : BaseEntity, ICloneable<ExportDeployment>
    {
        public ExportDeployment()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ExportDeployment(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// The export profile identifier.
        /// </summary>
        public int ProfileId { get; set; }

        private ExportProfile _profile;
        /// <summary>
        /// Gets or sets the export profile.
        /// </summary>
        public ExportProfile Profile
        {
            get => _profile ?? LazyLoader?.Load(this, ref _profile);
            set => _profile = value;
        }

        /// <summary>
        /// Name of the deployment.
        /// </summary>
        [Required, StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// A value indicating whether the deployment is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// XML formatted data with information about the last deployment result.
        /// </summary>
        [MaxLength]
        public string ResultInfo { get; set; }

        /// <summary>
        /// The deployment type identifier.
        /// </summary>
        public int DeploymentTypeId { get; set; }

        /// <summary>
        /// The deployment type.
        /// </summary>
        [NotMapped]
        public ExportDeploymentType DeploymentType
        {
            get => (ExportDeploymentType)DeploymentTypeId;
            set => DeploymentTypeId = (int)value;
        }

        [StringLength(400)]
        public string Username { get; set; }

        [StringLength(400)]
        public string Password { get; set; }

        /// <summary>
        /// Deployment URL.
        /// </summary>
        [StringLength(4000)]
        public string Url { get; set; }

        /// <summary>
        /// The type identifier of how to transmit via HTTP.
        /// </summary>
        public int HttpTransmissionTypeId { get; set; }

        // INFO: Mistakenly mapped. Keep it that way for now to avoid new migration.
        /// <summary>
        /// The type of how to transmit via HTTP.
        /// </summary>
        public ExportHttpTransmissionType HttpTransmissionType
        {
            get => (ExportHttpTransmissionType)HttpTransmissionTypeId;
            set => HttpTransmissionTypeId = (int)value;
        }

        /// <summary>
        /// The file system path.
        /// </summary>
        [StringLength(400)]
        public string FileSystemPath { get; set; }

        /// <summary>
        /// Path of a subfolder.
        /// </summary>
        [StringLength(400)]
        public string SubFolder { get; set; }

        /// <summary>
        /// Multiple email addresses can be separated by commas.
        /// </summary>
        [StringLength(4000)]
        public string EmailAddresses { get; set; }

        /// <summary>
        /// Subject of the email.
        /// </summary>
        [StringLength(400)]
        public string EmailSubject { get; set; }

        /// <summary>
        /// Identifier of the email account.
        /// </summary>
        public int EmailAccountId { get; set; }

        /// <summary>
        /// A value indicating whether to use FTP active or passive mode.
        /// </summary>
        public bool PassiveMode { get; set; }

        /// <summary>
        /// A value indicating whether to use SSL.
        /// </summary>
        public bool UseSsl { get; set; }

        public ExportDeployment Clone()
        {
            return new ExportDeployment
            {
                Name = Name,
                Enabled = Enabled,
                DeploymentTypeId = DeploymentTypeId,
                Username = Username,
                Password = Password,
                Url = Url,
                HttpTransmissionTypeId = HttpTransmissionTypeId,
                FileSystemPath = FileSystemPath,
                SubFolder = SubFolder,
                EmailAddresses = EmailAddresses,
                EmailSubject = EmailSubject,
                EmailAccountId = EmailAccountId,
                PassiveMode = PassiveMode,
                UseSsl = UseSsl
            };
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }
    }
}

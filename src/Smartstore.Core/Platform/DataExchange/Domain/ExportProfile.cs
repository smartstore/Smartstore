using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange
{
    internal class ExportProfileMap : IEntityTypeConfiguration<ExportProfile>
    {
        public void Configure(EntityTypeBuilder<ExportProfile> builder)
        {
            builder.HasOne(c => c.Task)
                .WithMany()
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    /// <summary>
    /// Represents an export profile.
    /// </summary>
    [DebuggerDisplay("{Name} (provider: {ProviderSystemName})")]
    public partial class ExportProfile : BaseEntity, ICloneable<ExportProfile>
    {
        public ExportProfile()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ExportProfile(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// The name of the profile.
        /// </summary>
        [Required, StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// The name of the export folder, e.g. 'smartstoreproductcsv'.
        /// All export files created for a profile are saved in this folder.
        /// </summary>
        [Required, StringLength(400)]
        public string FolderName { get; set; }

        /// <summary>
        /// The pattern for file names.
        /// </summary>
        [StringLength(400)]
        public string FileNamePattern { get; set; }

        /// <summary>
        /// The system name of the profile.
        /// </summary>
        [StringLength(400)]
        public string SystemName { get; set; }

        /// <summary>
        /// The system name of the export provider.
        /// </summary>
        [Required, StringLength(4000)]
        public string ProviderSystemName { get; set; }

        /// <summary>
        /// A value indicating whether the profile is an unremovable system profile.
        /// </summary>
        public bool IsSystemProfile { get; set; }

        /// <summary>
        /// A value indicating whether the export profile is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// A value indicating whether the export related data.
        /// </summary>
        public bool ExportRelatedData { get; set; }

        /// <summary>
        /// XML formatted data with filtering information.
        /// </summary>
        [MaxLength]
        public string Filtering { get; set; }

        /// <summary>
        /// XML formatted data with projection information.
        /// </summary>
        [MaxLength]
        public string Projection { get; set; }

        /// <summary>
        /// XML formatted data with provider specific configuration data.
        /// </summary>
        [MaxLength]
        public string ProviderConfigData { get; set; }

        /// <summary>
        /// XML formatted data with information about the last export.
        /// </summary>
        [MaxLength]
        public string ResultInfo { get; set; }

        /// <summary>
        /// The number of records to be skipped.
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Number of records to be loaded per database round-trip.
        /// </summary>
        public int Limit { get; set; }

        /// <summary>
        /// The maximum number of records of one processed batch.
        /// </summary>
        public int BatchSize { get; set; }

        /// <summary>
        /// A value indicating whether to start a separate run-through for each store.
        /// </summary>
        public bool PerStore { get; set; } = true;

        /// <summary>
        /// Email Account identifier used to send a notification message when an export completes.
        /// </summary>
        public int EmailAccountId { get; set; }

        /// <summary>
        /// Email addresses where to send the notification message.
        /// </summary>
        [StringLength(400)]
        public string CompletedEmailAddresses { get; set; }

        /// <summary>
        /// A value indicating whether to combine and compress the export files in a ZIP archive.
        /// </summary>
        public bool CreateZipArchive { get; set; }

        /// <summary>
        /// A value indicating whether to delete unneeded files after deployment.
        /// </summary>
        public bool Cleanup { get; set; } = true;

        /// <summary>
        /// The task identifier.
        /// </summary>
        [Column("SchedulingTaskId")]
        public int TaskId { get; set; }

        private TaskDescriptor _task;
        /// <summary>
        /// Gets or sets the task descriptor.
        /// </summary>
        public TaskDescriptor Task
        {
            get => _task ?? LazyLoader?.Load(this, ref _task);
            set => _task = value;
        }

        private ICollection<ExportDeployment> _deployments;
        /// <summary>
        /// Gets or sets the export deployments.
        /// </summary>
        public ICollection<ExportDeployment> Deployments
        {
            get => LazyLoader?.Load(this, ref _deployments) ?? (_deployments ??= new HashSet<ExportDeployment>());
            protected set => _deployments = value;
        }

        /// <inheritdoc/>
        public string GetDisplayName() => Name;

        /// <inheritdoc/>
        public string GetDisplayNameMemberName() => nameof(Name);

        public ExportProfile Clone()
        {
            return new ExportProfile
            {
                Name = Name,
                FolderName = null,
                FileNamePattern = FileNamePattern,
                ProviderSystemName = ProviderSystemName,
                Enabled = Enabled,
                TaskId = 0,
                Filtering = Filtering,
                Projection = Projection,
                ProviderConfigData = ProviderConfigData,
                ResultInfo = null,
                Offset = Offset,
                Limit = Limit,
                BatchSize = BatchSize,
                PerStore = PerStore,
                EmailAccountId = EmailAccountId,
                CompletedEmailAddresses = CompletedEmailAddresses,
                CreateZipArchive = CreateZipArchive,
                Cleanup = Cleanup
            };
        }

        object ICloneable.Clone()
        {
            return this.Clone();
        }
    }
}

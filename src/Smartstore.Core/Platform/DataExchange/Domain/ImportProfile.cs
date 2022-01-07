using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Scheduling;

namespace Smartstore.Core.DataExchange
{
    internal class ImportProfileMap : IEntityTypeConfiguration<ImportProfile>
    {
        public void Configure(EntityTypeBuilder<ImportProfile> builder)
        {
            builder.HasOne(c => c.Task)
                .WithMany()
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    /// <summary>
    /// Represents an import profile.
    /// </summary>
    public partial class ImportProfile : BaseEntity
    {
        public ImportProfile()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ImportProfile(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// The name of the profile.
        /// </summary>
        [Required, StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// The name of the folder (file system).
        /// </summary>
        [Required, StringLength(100)]
        public string FolderName { get; set; }

        /// <summary>
        /// The identifier of the file type.
        /// </summary>
        public int FileTypeId { get; set; }

        /// <summary>
        /// The file type.
        /// </summary>
        [NotMapped]
        public ImportFileType FileType
        {
            get => (ImportFileType)FileTypeId;
            set => FileTypeId = (int)value;
        }

        /// <summary>
        /// The identifier of the entity type.
        /// </summary>
        public int EntityTypeId { get; set; }

        /// <summary>
        /// The entity type.
        /// </summary>
        [NotMapped]
        public ImportEntityType EntityType
        {
            get => (ImportEntityType)EntityTypeId;
            set => EntityTypeId = (int)value;
        }

        /// <summary>
        /// A value indicating whether the profile is enabled.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// A value indicating whether to import related data.
        /// </summary>
        public bool ImportRelatedData { get; set; }

        /// <summary>
        /// Number of records to bypass.
        /// </summary>
        public int Skip { get; set; }

        /// <summary>
        /// Maximum number of records to return.
        /// </summary>
        public int Take { get; set; }

        /// <summary>
        /// Whether to only update existing data.
        /// </summary>
        public bool UpdateOnly { get; set; }

        /// <summary>
        /// Name of key fields to identify existing records during import.
        /// </summary>
        [StringLength(1000)]
        public string KeyFieldNames { get; set; }

        /// <summary>
        /// File type specific configuration.
        /// </summary>
        [MaxLength]
        public string FileTypeConfiguration { get; set; }

        /// <summary>
        /// XML formatted data with extra data.
        /// </summary>
        [MaxLength]
        public string ExtraData { get; set; }

        /// <summary>
        /// Mapping of import columns.
        /// </summary>
        [MaxLength]
        public string ColumnMapping { get; set; }

        /// <summary>
        /// XML formatted data with information about the last import.
        /// </summary>
        [MaxLength]
        public string ResultInfo { get; set; }

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

        /// <inheritdoc/>
        public string GetDisplayName() => Name;

        /// <inheritdoc/>
        public string GetDisplayNameMemberName() => nameof(Name);
    }
}

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Smartstore.Core.Localization
{
    internal class LocaleStringResourceMap : IEntityTypeConfiguration<LocaleStringResource>
    {
        public void Configure(EntityTypeBuilder<LocaleStringResource> builder)
        {
            builder.HasOne(x => x.Language)
                .WithMany(x => x.LocaleStringResources)
                .HasForeignKey(x => x.LanguageId);
        }
    }

    /// <summary>
    /// Represents a locale string resource
    /// </summary>
    [DebuggerDisplay("{ResourceName} - {ResourceValue}")]
    [Index(nameof(ResourceName), nameof(LanguageId), Name = "IX_LocaleStringResource")]
    public partial class LocaleStringResource : BaseEntity
    {
        public LocaleStringResource()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private LocaleStringResource(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the language identifier
        /// </summary>
        public int LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the resource name
        /// </summary>
        [Required, StringLength(200)]
        public string ResourceName { get; set; }

        /// <summary>
        /// Gets or sets the resource value
        /// </summary>
        [Required, MaxLength]
        public string ResourceValue { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this resource was installed by a plugin
        /// </summary>
        public bool? IsFromPlugin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this resource was modified by the user
        /// </summary>
        public bool? IsTouched { get; set; }

        /// <summary>
        /// Gets or sets the date and time of entity creation
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the date and time of entity update
        /// </summary>
        public DateTime? UpdatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the name of the user who created the entity.
        /// Usually the login name, but may also be any external caller name
        /// (like a translation service for example).
        /// </summary>
        [StringLength(100)]
        public string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the name of the user who updated the entity.
        /// Usually the login name, but may also be any external caller name
        /// (like a translation service for example).
        /// </summary>
        [StringLength(100)]
        public string UpdatedBy { get; set; }

        /// <summary>
        /// For future use
        /// </summary>
        [StringLength(64)]
        public string MasterChecksum { get; set; }

        private Language _language;
        /// <summary>
        /// Gets or sets the language
        /// </summary>
        [IgnoreDataMember]
        public Language Language
        {
            get => _language ?? LazyLoader.Load(this, ref _language);
            set => _language = value;
        }
    }
}

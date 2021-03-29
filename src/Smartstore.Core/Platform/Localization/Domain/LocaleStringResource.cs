using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Newtonsoft.Json;
using Smartstore.Domain;

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
        private readonly ILazyLoader _lazyLoader;

        public LocaleStringResource()
        {
        }

        public LocaleStringResource(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
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

        private Language _language;
        /// <summary>
        /// Gets or sets the language
        /// </summary>
        [JsonIgnore]
        public Language Language
        {
            get => _language ?? _lazyLoader?.Load(this, ref _language);
            set => _language = value;
        }
    }
}

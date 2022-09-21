using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Represents a localized property
    /// </summary>
    [Index(nameof(LocaleKeyGroup), Name = "IX_LocalizedProperty_LocaleKeyGroup")]
    [Index(nameof(EntityId), nameof(LocaleKey), nameof(LocaleKeyGroup), nameof(LanguageId), Name = "IX_LocalizedProperty_Compound")]
    public partial class LocalizedProperty : BaseEntity
    {
        public LocalizedProperty()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private LocalizedProperty(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the entity identifier
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Gets or sets the language identifier
        /// </summary>
        public int LanguageId { get; set; }

        /// <summary>
        /// Gets or sets the locale key group
        /// </summary>
        [Required, StringLength(150)]
        public string LocaleKeyGroup { get; set; }

        /// <summary>
        /// Gets or sets the locale key
        /// </summary>
        [Required, StringLength(255)]
        public string LocaleKey { get; set; }

        /// <summary>
        /// Gets or sets the locale value
        /// </summary>
        [Required, MaxLength]
        public string LocaleValue { get; set; }

        /// <summary>
        /// Hidden entities are treated like they did not exist.
        /// They neither appear in the UI nor are they cached.
        /// Hiding <see cref="LocalizedProperty"/> entities can be
        /// very helpful for external services though (like translation services).
        /// </summary>
        public bool IsHidden { get; set; }

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
        /// Can be used by external translation services to
        /// save the date of last translation.
        /// </summary>
        public DateTime? TranslatedOnUtc { get; set; }

        /// <summary>
        /// For future use.
        /// </summary>
        [StringLength(64)]
        public string MasterChecksum { get; set; }

        private Language _language;
        /// <summary>
        /// Gets the language
        /// </summary>
        public Language Language
        {
            get => _language ?? LazyLoader.Load(this, ref _language);
            set => _language = value;
        }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Domain;

namespace Smartstore.Core.Localization
{
    internal class LocalizedPropertyMap : IEntityTypeConfiguration<LocalizedProperty>
    {
        public void Configure(EntityTypeBuilder<LocalizedProperty> builder)
        {
            builder
                .HasOne(x => x.Language)
                .WithMany(navigationName: null)
                .HasForeignKey(x => x.LanguageId);
        }
    }

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
        [Required, StringLength(400)]
        public string LocaleKeyGroup { get; set; }

        /// <summary>
        /// Gets or sets the locale key
        /// </summary>
        [Required, StringLength(400)]
        public string LocaleKey { get; set; }

        /// <summary>
        /// Gets or sets the locale value
        /// </summary>
        [Required, MaxLength]
        public string LocaleValue { get; set; }

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

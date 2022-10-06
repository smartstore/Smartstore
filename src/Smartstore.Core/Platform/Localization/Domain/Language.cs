using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Stores;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Localization
{
    internal class LanguageMap : IEntityTypeConfiguration<Language>
    {
        public void Configure(EntityTypeBuilder<Language> builder)
        {
            builder
                .HasMany(x => x.LocaleStringResources)
                .WithOne(x => x.Language)
                .HasForeignKey(x => x.LanguageId);
        }
    }

    /// <summary>
    /// Represents a language
    /// </summary>
    [DebuggerDisplay("{LanguageCulture}")]
    [Index(nameof(DisplayOrder), Name = "IX_Language_DisplayOrder")]
    [CacheableEntity]
    public partial class Language : EntityWithAttributes, IStoreRestricted
    {
        public Language()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private Language(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [Required, StringLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the language culture (e.g. "en-US")
        /// </summary>
        [Required, StringLength(20)]
        public string LanguageCulture { get; set; }

        /// <summary>
        /// Gets or sets the unique SEO code (e.g. "en")
        /// </summary>
        [StringLength(2)]
        public string UniqueSeoCode { get; set; }

        /// <summary>
        /// Gets or sets the flag image file name
        /// </summary>
        [StringLength(50)]
        public string FlagImageFileName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the language supports "Right-to-left"
        /// </summary>
        public bool Rtl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the language is published
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        private ICollection<LocaleStringResource> _localeStringResources;
        /// <summary>
        /// Gets or sets locale string resources
        /// </summary>
        [IgnoreDataMember]
        public ICollection<LocaleStringResource> LocaleStringResources
        {
            get => _localeStringResources ?? LazyLoader.Load(this, ref _localeStringResources) ?? (_localeStringResources ??= new HashSet<LocaleStringResource>());
            protected set => _localeStringResources = value;
        }

        public string GetTwoLetterISOLanguageName()
        {
            if (UniqueSeoCode.HasValue())
            {
                return UniqueSeoCode;
            }

            try
            {
                return new CultureInfo(LanguageCulture).TwoLetterISOLanguageName;
            }
            catch
            {
                return null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Seo;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Domain;

namespace Smartstore.Core.Content.News
{
    public class NewsItemMap : IEntityTypeConfiguration<NewsItem>
    {
        public void Configure(EntityTypeBuilder<NewsItem> builder)
        {
            builder.HasOne(c => c.Language)
                .WithMany()
                .HasForeignKey(c => c.LanguageId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.MediaFile)
                .WithMany()
                .HasForeignKey(c => c.MediaFileId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(c => c.PreviewMediaFile)
                .WithMany()
                .HasForeignKey(c => c.PreviewMediaFileId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }

    /// <summary>
    /// Represents a news item.
    /// </summary>
    // TODO: (mh) (core) not neccessary because of mapping configuration. REMOVE THIS COMMENT.
    //[Index(nameof(LanguageId), Name = "IX_LanguageId")]
    //[Index(nameof(MediaFileId), Name = "IX_MediaFileId")]
    //[Index(nameof(PreviewMediaFileId), Name = "IX_PreviewMediaFileId")]
    public partial class NewsItem : BaseEntity, ISlugSupported, IStoreRestricted, ILocalizedEntity
    {
        #region static

        private static readonly List<string> _visibilityAffectingProps = new()
        {
            nameof(Published),
            nameof(StartDateUtc),
            nameof(EndDateUtc),
            nameof(LimitedToStores)
        };

        public static IReadOnlyCollection<string> GetVisibilityAffectingPropertyNames()
        {
            return _visibilityAffectingProps;
        }

        #endregion

        private readonly ILazyLoader _lazyLoader;

        public NewsItem()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private NewsItem(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        /// <summary>
        /// Gets or sets the news title.
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the short text.
        /// </summary>
        [Required]
        public string Short { get; set; }

        /// <summary>
        /// Gets or sets the full text.
        /// </summary>
        [Required, MaxLength]
        public string Full { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the news item is published.
        /// </summary>
        public bool Published { get; set; }

        /// <summary>
        /// Gets or sets the media file identifier.
        /// </summary>
        public int? MediaFileId { get; set; }

        private MediaFile _mediaFile;
        /// <summary>
        /// Gets or sets the media file.
        /// </summary>
        public MediaFile MediaFile
        {
            get => _lazyLoader?.Load(this, ref _mediaFile) ?? _mediaFile;
            set => _mediaFile = value;
        }

        /// <summary>
        /// Gets or sets the preview media file identifier.
        /// </summary>
        public int? PreviewMediaFileId { get; set; }

        private MediaFile _previewMediaFile;
        /// <summary>
        /// Gets or sets the preview media file.
        /// </summary>
        public MediaFile PreviewMediaFile
        {
            get => _lazyLoader?.Load(this, ref _previewMediaFile) ?? _previewMediaFile;
            set => _previewMediaFile = value;
        }

        /// <summary>
        /// Gets or sets the news item start date and time.
        /// </summary>
        public DateTime? StartDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the news item end date and time.
        /// </summary>
        public DateTime? EndDateUtc { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the news post comments are allowed.
        /// </summary>
        public bool AllowComments { get; set; }

        /// <summary>
        /// Gets or sets the total number of approved comments.
        /// <remarks>The same as if we run newsItem.NewsComments.Where(n => n.IsApproved).Count().
        /// We use this property for performance optimization (no SQL command executed).
        /// </remarks>
        /// </summary>
        public int ApprovedCommentCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of not approved comments.
        /// <remarks>The same as if we run newsItem.NewsComments.Where(n => !n.IsApproved).Count().
        /// We use this property for performance optimization (no SQL command executed).
        /// </remarks>
        /// </summary>
        public int NotApprovedCommentCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores.
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets the date and time of entity creation.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets the meta keywords.
        /// </summary>
        [StringLength(400)]
        public string MetaKeywords { get; set; }

        /// <summary>
        /// Gets or sets the meta description.
        /// </summary>
        public string MetaDescription { get; set; }

        /// <summary>
        /// Gets or sets the meta title.
        /// </summary>
        [StringLength(400)]
        public string MetaTitle { get; set; }

        /// <summary>
        /// Gets or sets a language identifier for which the news item should be displayed.
        /// </summary>
        public int? LanguageId { get; set; }

        private Language _language;
        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        [NotMapped]
        public virtual Language Language {
            get => _lazyLoader?.Load(this, ref _language) ?? _language;
            set => _language = value;
        }

        private ICollection<NewsComment> _newsComments;
        /// <summary>
        /// Gets or sets the news comments.
        /// </summary>
        [NotMapped]
        public virtual ICollection<NewsComment> NewsComments
        {
            get => _lazyLoader?.Load(this, ref _newsComments) ?? _newsComments;
            set => _newsComments = value;
        }

        /// <inheritdoc/>
        public string GetDisplayName()
        {
            return Title;
        }

        /// <inheritdoc/>
        public string GetDisplayNameMemberName()
        {
            return nameof(Title);
        }
    }
}

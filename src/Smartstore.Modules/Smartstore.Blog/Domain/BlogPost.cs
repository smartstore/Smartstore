using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Seo;
using Smartstore.Core.Localization;
using Smartstore.Core.Stores;
using Smartstore.Domain;

namespace Smartstore.Blog.Domain
{
    internal class BlogPostMap : IEntityTypeConfiguration<BlogPost>
    {
        public void Configure(EntityTypeBuilder<BlogPost> builder)
        {
            builder.HasOne(c => c.Language)
                .WithMany()
                .HasForeignKey(c => c.LanguageId)
                .OnDelete(DeleteBehavior.NoAction);

            // TODO: SetNull and test this!!!
            builder.HasOne(c => c.MediaFile)
                .WithMany()
                .HasForeignKey(c => c.MediaFileId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(c => c.PreviewMediaFile)
                .WithMany()
                .HasForeignKey(c => c.PreviewMediaFileId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }

    /// <summary>
    /// Represents a blog post.
    /// </summary>
    [Index(nameof(Title), Name = "IX_Title")]
    public partial class BlogPost : BaseEntity, ILocalizedEntity, ISlugSupported, IStoreRestricted
    {
        #region static

        private static readonly List<string> _visibilityAffectingProps = new List<string>
        {
            nameof(IsPublished),
            nameof(StartDateUtc),
            nameof(EndDateUtc),
            nameof(LimitedToStores)
        };

        public static IReadOnlyCollection<string> GetVisibilityAffectingPropertyNames()
        {
            return _visibilityAffectingProps;
        }

        #endregion

        public BlogPost()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private BlogPost(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the blog post comments are allowed.
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// Gets or sets the blog post title.
        /// </summary>
        [Required]
        [StringLength(450)]
        public string Title { get; set; }

        /// <summary>
        /// Defines the preview display type of the picture.
        /// </summary>
        public PreviewDisplayType PreviewDisplayType { get; set; }

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
            get => _mediaFile ?? LazyLoader.Load(this, ref _mediaFile);
            set => _mediaFile = value;
        }

        /// <summary>
        /// Gets or sets the preview media file identifier.
        /// </summary>
        public int? PreviewMediaFileId { get; set; }

        /// <summary>
        /// Gets or sets the preview media file.
        /// </summary>
        private MediaFile _previewMediaFile;
        /// <summary>
        /// Gets or sets the media file.
        /// </summary>
        public MediaFile PreviewMediaFile
        {
            get => _previewMediaFile ?? LazyLoader.Load(this, ref _previewMediaFile);
            set => _previewMediaFile = value;
        }

        /// <summary>
        /// Gets or sets background for the blog post.
        /// </summary>
        public string SectionBg { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the blog post has a background image.
        /// </summary>
        public bool DisplayTagsInPreview { get; set; }

        /// <summary>
        /// Gets or sets the blog post intro.
        /// </summary>
        public string Intro { get; set; }

        /// <summary>
        /// Gets or sets the blog post title
        /// </summary>
        [Required, MaxLength]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the blog post comments are allowed.
        /// </summary>
        public bool AllowComments { get; set; }

        /// <summary>
        /// Gets or sets the total number of approved comments.
        /// <remarks>The same as if we run blogItem.Comments.Where(n => n.IsApproved).Count().
        /// We use this property for performance optimization (no SQL command executed).
        /// </remarks>
        /// </summary>
        public int ApprovedCommentCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of not approved comments.
        /// <remarks>The same as if we run blogItem.Comments.Where(n => !n.IsApproved).Count().
        /// We use this property for performance optimization (no SQL command executed).</remarks>
        /// </summary>
        public int NotApprovedCommentCount { get; set; }

        /// <summary>
        /// Gets or sets the blog tags.
        /// </summary>
        public string Tags { get; set; }

        /// <summary>
        /// Gets or sets the blog post start date and time.
        /// </summary>
        public DateTime? StartDateUtc { get; set; }

        /// <summary>
        /// Gets or sets the blog post end date and time.
        /// </summary>
        public DateTime? EndDateUtc { get; set; }

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
        /// Gets or sets a value indicating whether the entity is limited/restricted to certain stores.
        /// </summary>
        public bool LimitedToStores { get; set; }

        /// <summary>
        /// Gets or sets the date and time of entity creation.
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }

        /// <summary>
        /// Gets or sets a language identifier for which the blog post should be displayed.
        /// </summary>
        public int? LanguageId { get; set; }

        private Language _language;
        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        [NotMapped]
        public Language Language
        {
            get => _language ?? LazyLoader.Load(this, ref _language);
            set => _language = value;
        }

        private ICollection<BlogComment> _blogComments;
        /// <summary>
        /// Gets or sets the blog comments.
        /// </summary>
        public ICollection<BlogComment> BlogComments
        {
            get => _blogComments ?? LazyLoader.Load(this, ref _blogComments) ?? (_blogComments ??= new HashSet<BlogComment>());
            protected set => _blogComments = value;
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

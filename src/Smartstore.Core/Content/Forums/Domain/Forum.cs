//using System;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Diagnostics.CodeAnalysis;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Infrastructure;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using Smartstore.Core.Content.Forums.Domain;
//using Smartstore.Core.Seo;
//using Smartstore.Core.Localization;
//using Smartstore.Domain;

//namespace Smartstore.Core.Content.Forums
//{
//    internal class ForumMap : IEntityTypeConfiguration<Forum>
//    {
//        public void Configure(EntityTypeBuilder<Forum> builder)
//        {
//            builder.HasOne(c => c.ForumGroup)
//                .WithMany()
//                .HasForeignKey(c => c.ForumGroupId);
//        }
//    }

//    /// <summary>
//    /// Represents a forum.
//    /// </summary>
//    [Table("Forums_Forum")]
//    [Index(nameof(ForumGroupId), Name = "IX_Forums_Forum_ForumGroupId")]
//    [Index(nameof(DisplayOrder), Name = "IX_Forums_Forum_DisplayOrder")]
//    [Index(nameof(ForumGroupId), nameof(DisplayOrder), Name = "IX_ForumGroupId_DisplayOrder")]
//    public partial class Forum : BaseEntity, IAuditable, ILocalizedEntity, ISlugSupported
//    {
//        private readonly ILazyLoader _lazyLoader;

//        public Forum()
//        {
//        }

//        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
//        private Forum(ILazyLoader lazyLoader)
//        {
//            _lazyLoader = lazyLoader;
//        }

//        /// <summary>
//        /// Gets or sets the forum group identifier.
//        /// </summary>
//        public int ForumGroupId { get; set; }

//        /// <summary>
//        /// Gets or sets the name.
//        /// </summary>
//        [Required, StringLength(200)]
//        public string Name { get; set; }

//        /// <summary>
//        /// Gets or sets the description.
//        /// </summary>
//        [MaxLength]
//        public string Description { get; set; }

//        /// <summary>
//        /// Gets or sets the number of topics.
//        /// </summary>
//        public int NumTopics { get; set; }

//        /// <summary>
//        /// Gets or sets the number of posts.
//        /// </summary>
//        public int NumPosts { get; set; }

//        /// <summary>
//        /// Gets or sets the last topic identifier.
//        /// </summary>
//        public int LastTopicId { get; set; }

//        /// <summary>
//        /// Gets or sets the last post identifier.
//        /// </summary>
//        public int LastPostId { get; set; }

//        /// <summary>
//        /// Gets or sets the last post customer identifier.
//        /// </summary>
//        public int LastPostCustomerId { get; set; }

//        /// <summary>
//        /// Gets or sets the last post date and time.
//        /// </summary>
//        public DateTime? LastPostTime { get; set; }

//        /// <summary>
//        /// Gets or sets the display order.
//        /// </summary>
//        public int DisplayOrder { get; set; }

//        /// <summary>
//        /// Gets or sets the date and time of instance creation.
//        /// </summary>
//        public DateTime CreatedOnUtc { get; set; }

//        /// <summary>
//        /// Gets or sets the date and time of instance update.
//        /// </summary>
//        public DateTime UpdatedOnUtc { get; set; }

//        private ForumGroup _forumGroup;
//        /// <summary>
//        /// Gets the ForumGroup.
//        /// </summary>
//        public virtual ForumGroup ForumGroup {
//            get => _lazyLoader?.Load(this, ref _forumGroup) ?? _forumGroup;
//            set => _forumGroup = value;
//        }

//        /// <inheritdoc/>
//        public string GetDisplayName()
//        {
//            return Name;
//        }

//        /// <inheritdoc/>
//        public string GetDisplayNameMemberName()
//        {
//            return nameof(Name);
//        }
//    }
//}

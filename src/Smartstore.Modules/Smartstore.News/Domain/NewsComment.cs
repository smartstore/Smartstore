using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Identity;

namespace Smartstore.News.Domain
{
    internal class NewsCommentMap : IEntityTypeConfiguration<NewsComment>
    {
        public void Configure(EntityTypeBuilder<NewsComment> builder)
        {
            builder.HasOne(c => c.NewsItem)
                .WithMany(c => c.NewsComments)          // INFO: Important! Must be set in this case else CustomerContent retrieval of type NewsComment will fail.
                .HasForeignKey(c => c.NewsItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a news comment.
    /// </summary>
    [Table("NewsComment")] // Enables EF TPT inheritance
    public partial class NewsComment : CustomerContent
    {
        public NewsComment()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private NewsComment(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Gets or sets the comment title.
        /// </summary>
        [StringLength(450)]
        public string CommentTitle { get; set; }

        /// <summary>
        /// Gets or sets the comment text.
        /// </summary>
        [MaxLength]
        public string CommentText { get; set; }

        /// <summary>
        /// Gets or sets the news item identifier.
        /// </summary>
        public int NewsItemId { get; set; }

        private NewsItem _newsItem;
        /// <summary>
        /// Gets or sets the news item.
        /// </summary>
        public NewsItem NewsItem
        {
            get => _newsItem ?? LazyLoader.Load(this, ref _newsItem);
            set => _newsItem = value;
        }
    }
}

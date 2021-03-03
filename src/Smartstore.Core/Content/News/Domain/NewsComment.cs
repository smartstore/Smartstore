//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Diagnostics.CodeAnalysis;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Infrastructure;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using Smartstore.Core.Identity;

//namespace Smartstore.Core.Content.News
//{
//    internal class NewsCommentMap : IEntityTypeConfiguration<NewsComment>
//    {
//        public void Configure(EntityTypeBuilder<NewsComment> builder)
//        {
//            builder.HasOne(c => c.NewsItem)
//                .WithMany()
//                .HasForeignKey(c => c.NewsItemId);
//        }
//    }

//    /// <summary>
//    /// Represents a news comment.
//    /// </summary>
//    [Table("NewsComment")] // Enables EF TPT inheritance
//    public partial class NewsComment : CustomerContent
//    {

//        private readonly ILazyLoader _lazyLoader;

//        public NewsComment()
//        {
//        }

//        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
//        private NewsComment(ILazyLoader lazyLoader)
//        {
//            _lazyLoader = lazyLoader;
//        }

//        /// <summary>
//        /// Gets or sets the comment title.
//        /// </summary>
//        public string CommentTitle { get; set; }

//        /// <summary>
//        /// Gets or sets the comment text.
//        /// </summary>
//        [MaxLength]
//        public string CommentText { get; set; }

//        /// <summary>
//        /// Gets or sets the news item identifier.
//        /// </summary>
//        public int NewsItemId { get; set; }

//        /// <summary>
//        /// Gets or sets the news item.
//        /// </summary>

//        private NewsItem _newsItem;
//        /// <summary>
//        /// Gets or sets the language.
//        /// </summary>
//        [NotMapped]
//        public virtual NewsItem NewsItem
//        {
//            get => _lazyLoader?.Load(this, ref _newsItem) ?? _newsItem;
//            set => _newsItem = value;
//        }
//    }
//}

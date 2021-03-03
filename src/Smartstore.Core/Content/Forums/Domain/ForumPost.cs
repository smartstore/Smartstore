//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Diagnostics.CodeAnalysis;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Infrastructure;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using Smartstore.Core.Content.Forums.Domain;
//using Smartstore.Core.Identity;
//using Smartstore.Domain;

//namespace Smartstore.Core.Content.Forums
//{
//    internal class ForumPostMap : IEntityTypeConfiguration<ForumPost>
//    {
//        public void Configure(EntityTypeBuilder<ForumPost> builder)
//        {
//            builder.HasOne(c => c.ForumTopic)
//                .WithMany()
//                .HasForeignKey(c => c.TopicId);

//            builder.HasOne(c => c.Customer)
//                .WithMany()
//                .HasForeignKey(c => c.CustomerId)
//                .OnDelete(DeleteBehavior.SetNull);
//        }
//    }

//    /// <summary>
//    /// Represents a forum post.
//    /// </summary>
//    [Table("Forums_Post")]
//    [Index(nameof(TopicId), Name = "IX_Forums_Post_TopicId")]
//    [Index(nameof(CustomerId), Name = "IX_Forums_Post_CustomerId")]
//    [Index(nameof(CreatedOnUtc), Name = "IX_CreatedOnUtc")]
//    [Index(nameof(Published), Name = "IX_Published")]
//    public partial class ForumPost : BaseEntity, IAuditable
//    {
//        private readonly ILazyLoader _lazyLoader;

//        public ForumPost()
//        {
//        }

//        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
//        private ForumPost(ILazyLoader lazyLoader)
//        {
//            _lazyLoader = lazyLoader;
//        }

//        /// <summary>
//        /// Gets or sets the forum topic identifier.
//        /// </summary>
//        public int TopicId { get; set; }

//        /// <summary>
//        /// Gets or sets the customer identifier.
//        /// </summary>
//        public int CustomerId { get; set; }

//        /// <summary>
//        /// Gets or sets the text.
//        /// </summary>
//        [Required, MaxLength]
//        public string Text { get; set; }

//        /// <summary>
//        /// Gets or sets the IP address.
//        /// </summary>
//        [StringLength(100)]
//        public string IPAddress { get; set; }

//        /// <summary>
//        /// Gets or sets the date and time of instance creation.
//        /// </summary>
//        public DateTime CreatedOnUtc { get; set; }

//        /// <summary>
//        /// Gets or sets the date and time of instance update.
//        /// </summary>
//        public DateTime UpdatedOnUtc { get; set; }

//        /// <summary>
//        /// Gets or sets a value indicating whether the entity is published.
//        /// </summary>
//        public bool Published { get; set; }

//        private ForumTopic _forumTopic;
//        /// <summary>
//        /// Gets the topic.
//        /// </summary>
//        public virtual ForumTopic ForumTopic {
//            get => _lazyLoader?.Load(this, ref _forumTopic) ?? _forumTopic;
//            protected set => _forumTopic = value;
//        }

//        private Customer _customer;
//        /// <summary>
//        /// Gets the customer.
//        /// </summary>
//        public virtual Customer Customer {
//            get => _lazyLoader?.Load(this, ref _customer) ?? _customer;
//            protected set => _customer = value;
//        }

//        private ICollection<ForumPostVote> _forumPostVotes;
//        /// <summary>
//        /// Forum post votes.
//        /// </summary>
//        public virtual ICollection<ForumPostVote> ForumPostVotes
//        {
//            get => _lazyLoader?.Load(this, ref _forumPostVotes) ?? _forumPostVotes;
//            protected set => _forumPostVotes = value;
//        }
//    }
//}

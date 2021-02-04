//using System;
//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.Diagnostics.CodeAnalysis;
//using System.Text.Json.Serialization;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Infrastructure;
//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using Smartstore.Core.Identity;
//using Smartstore.Domain;

//namespace Smartstore.Core.Content.Forums
//{
//    public class ForumTopicMap : IEntityTypeConfiguration<ForumTopic>
//    {
//        public void Configure(EntityTypeBuilder<ForumTopic> builder)
//        {
//            builder.HasOne(c => c.Forum)
//                .WithMany()
//                .HasForeignKey(c => c.ForumId);

//            builder.HasOne(c => c.Customer)
//                .WithMany()
//                .HasForeignKey(c => c.CustomerId)
//                .OnDelete(DeleteBehavior.SetNull);
//        }
//    }

//    /// <summary>
//    /// Represents a forum topic.
//    /// </summary>
//    [Table("Forums_Topic")]
//    [Index(nameof(ForumId), Name = "IX_Forums_Topic_ForumId")]
//    [Index(nameof(Subject), Name = "IX_Subject")]
//    [Index(nameof(NumPosts), Name = "IX_NumPosts")]
//    [Index(nameof(CreatedOnUtc), Name = "IX_CreatedOnUtc")]
//    [Index(nameof(ForumId), nameof(Published), Name = "IX_ForumId_Published")]
//    [Index(nameof(TopicTypeId), nameof(LastPostTime), Name = "IX_TopicTypeId_LastPostTime")]
//    public partial class ForumTopic : BaseEntity, IAuditable
//    {
//        private readonly ILazyLoader _lazyLoader;

//        public ForumTopic()
//        {
//        }

//        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
//        private ForumTopic(ILazyLoader lazyLoader)
//        {
//            _lazyLoader = lazyLoader;
//        }

//        /// <summary>
//        /// Gets or sets the forum identifier.
//        /// </summary>
//        public int ForumId { get; set; }

//        /// <summary>
//        /// Gets or sets the customer identifier.
//        /// </summary>
//        public int CustomerId { get; set; }

//        /// <summary>
//        /// Gets or sets the topic type identifier.
//        /// </summary>
//        public int TopicTypeId { get; set; }

//        /// <summary>
//        /// Gets or sets the subject.
//        /// </summary>
//        [Required, StringLength(450)]
//        public string Subject { get; set; }

//        /// <summary>
//        /// Gets or sets the number of posts.
//        /// </summary>
//        public int NumPosts { get; set; }

//        /// <summary>
//        /// Gets or sets the number of views.
//        /// </summary>
//        public int Views { get; set; }

//        /// <summary>
//        /// Gets or sets the first post identifier, for example of the first search hit.
//        /// This property is not a data member.
//        /// </summary>
//        [NotMapped, JsonIgnore]
//        public int FirstPostId { get; set; }

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

//        /// <summary>
//        /// Gets or sets the forum topic type.
//        /// </summary>
//        [NotMapped, JsonIgnore]
//        public ForumTopicType ForumTopicType
//        {
//            get => (ForumTopicType)TopicTypeId;
//            set => TopicTypeId = (int)value;
//        }

//        private Forum _forum;
//        /// <summary>
//        /// Gets the forum.
//        /// </summary>
//        public virtual Forum Forum {
//            get => _lazyLoader?.Load(this, ref _forum) ?? _forum;
//            set => _forum = value;
//        }

//        private Customer _customer;
//        /// <summary>
//        /// Gets the customer.
//        /// </summary>
//        public virtual Customer Customer {
//            get => _lazyLoader?.Load(this, ref _customer) ?? _customer;
//            set => _customer = value;
//        }

//        /// <summary>
//        /// Gets the number of replies.
//        /// </summary>
//        [NotMapped, JsonIgnore]
//        public int NumReplies
//        {
//            get
//            {
//                if (NumPosts > 0)
//                {
//                    return NumPosts - 1;
//                }

//                return 0;
//            }
//        }
//    }
//}

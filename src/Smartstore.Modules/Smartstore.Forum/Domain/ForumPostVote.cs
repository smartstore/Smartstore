using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Smartstore.Core.Identity;

namespace Smartstore.Forum
{
    internal class ForumPostVoteMap : IEntityTypeConfiguration<ForumPostVote>
    {
        public void Configure(EntityTypeBuilder<ForumPostVote> builder)
        {
            builder.HasOne(c => c.ForumPost)
                .WithMany(c => c.ForumPostVotes)
                .HasForeignKey(c => c.ForumPostId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    /// <summary>
    /// Represents a vote for a forum post.
    /// </summary>
    [Table("ForumPostVote")]  // Enables EF TPT inheritance
    public partial class ForumPostVote : CustomerContent
    {
        public ForumPostVote()
        {
        }

        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private member.", Justification = "Required for EF lazy loading")]
        private ForumPostVote(ILazyLoader lazyLoader)
            : base(lazyLoader)
        {
        }

        /// <summary>
        /// Forum post identifier.
        /// </summary>
        public int ForumPostId { get; set; }

        /// <summary>
        /// A value indicating whether the customer voted for or against a forum post.
        /// </summary>
        public bool Vote { get; set; }

        private ForumPost _forumPost;
        /// <summary>
        /// Forum post entity.
        /// </summary>
        public virtual ForumPost ForumPost
        {
            get => _forumPost ?? LazyLoader.Load(this, ref _forumPost);
            protected set => _forumPost = value;
        }
    }
}

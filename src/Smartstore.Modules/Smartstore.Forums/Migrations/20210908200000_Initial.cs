using System.Data;
using FluentMigrator;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Identity;
using Smartstore.Domain;
using Smartstore.Forums.Domain;

namespace Smartstore.Forums.Migrations
{
    [MigrationVersion("2021-09-08 20:00:00", "Forum: Initial")]
    internal class Initial : Migration
    {
        public override void Up()
        {
            // INFO: without a name an index becomes IX_Forums_Forum_DisplayOrder instead of IX_DisplayOrder.
            const string forumGroup = "Forums_Group";
            const string forum = "Forums_Forum";
            const string forumTopic = "Forums_Topic";
            const string forumPost = "Forums_Post";
            const string forumSubscription = "Forums_Subscription";
            const string forumPostVote = "ForumPostVote";
            const string privateMessage = "Forums_PrivateMessage";
            const string id = nameof(BaseEntity.Id);

            // ForumGroup.
            if (!Schema.Table(forumGroup).Exists())
            {
                Create.Table(forumGroup)
                    .WithIdColumn()
                    .WithColumn(nameof(ForumGroup.Name)).AsString(200).NotNullable()
                    .WithColumn(nameof(ForumGroup.Description)).AsString(int.MaxValue).Nullable()
                    .WithColumn(nameof(ForumGroup.DisplayOrder)).AsInt32().NotNullable()
                        .Indexed("IX_DisplayOrder")
                    .WithColumn(nameof(ForumGroup.CreatedOnUtc)).AsDateTime2().NotNullable()
                    .WithColumn(nameof(ForumGroup.UpdatedOnUtc)).AsDateTime2().NotNullable()
                    .WithColumn(nameof(ForumGroup.LimitedToStores)).AsBoolean().NotNullable()
                        .Indexed("IX_LimitedToStores")
                    .WithColumn(nameof(ForumGroup.SubjectToAcl)).AsBoolean().NotNullable()
                        .Indexed("IX_SubjectToAcl");
            }

            // Forum.
            if (!Schema.Table(forum).Exists())
            {
                Create.Table(forum)
                    .WithIdColumn()
                    .WithColumn(nameof(Domain.Forum.ForumGroupId)).AsInt32().NotNullable()
                        .Indexed().ForeignKey(forumGroup, id).OnDelete(Rule.Cascade)
                    .WithColumn(nameof(Domain.Forum.Name)).AsString(200).NotNullable()
                    .WithColumn(nameof(Domain.Forum.Description)).AsString(int.MaxValue).Nullable()
                    .WithColumn(nameof(Domain.Forum.NumTopics)).AsInt32().NotNullable()
                    .WithColumn(nameof(Domain.Forum.NumPosts)).AsInt32().NotNullable()
                    .WithColumn(nameof(Domain.Forum.LastTopicId)).AsInt32().NotNullable()
                    .WithColumn(nameof(Domain.Forum.LastPostId)).AsInt32().NotNullable()
                    .WithColumn(nameof(Domain.Forum.LastPostCustomerId)).AsInt32().NotNullable()
                    .WithColumn(nameof(Domain.Forum.LastPostTime)).AsDateTime2().Nullable()
                    .WithColumn(nameof(Domain.Forum.DisplayOrder)).AsInt32().NotNullable()
                        .Indexed("IX_Forums_Forum_DisplayOrder")
                    .WithColumn(nameof(Domain.Forum.CreatedOnUtc)).AsDateTime2().NotNullable()
                    .WithColumn(nameof(Domain.Forum.UpdatedOnUtc)).AsDateTime2().NotNullable();

                Create.Index("IX_ForumGroupId_DisplayOrder")
                    .OnTable(forum)
                    .OnColumn(nameof(Domain.Forum.ForumGroupId)).Ascending()
                    .OnColumn(nameof(Domain.Forum.DisplayOrder)).Ascending()
                    .WithOptions()
                    .NonClustered();
            }

            // ForumTopic.
            if (!Schema.Table(forumTopic).Exists())
            {
                Create.Table(forumTopic)
                    .WithIdColumn()
                    .WithColumn(nameof(ForumTopic.ForumId)).AsInt32().NotNullable()
                        .Indexed().ForeignKey(forum, id).OnDelete(Rule.Cascade)
                    .WithColumn(nameof(ForumTopic.CustomerId)).AsInt32().NotNullable()
                        .Indexed().ForeignKey(nameof(Customer), id)
                    .WithColumn(nameof(ForumTopic.TopicTypeId)).AsInt32().NotNullable()
                    .WithColumn(nameof(ForumTopic.Subject)).AsString(450).NotNullable()
                        .Indexed("IX_Subject")
                    .WithColumn(nameof(ForumTopic.NumPosts)).AsInt32().NotNullable()
                        .Indexed("IX_NumPosts")
                    .WithColumn(nameof(ForumTopic.Views)).AsInt32().NotNullable()
                    .WithColumn(nameof(ForumTopic.LastPostId)).AsInt32().NotNullable()
                    .WithColumn(nameof(ForumTopic.LastPostCustomerId)).AsInt32().NotNullable()
                    .WithColumn(nameof(ForumTopic.LastPostTime)).AsDateTime2().Nullable()
                    .WithColumn(nameof(ForumTopic.CreatedOnUtc)).AsDateTime2().NotNullable()
                        .Indexed("IX_CreatedOnUtc")
                    .WithColumn(nameof(ForumTopic.UpdatedOnUtc)).AsDateTime2().NotNullable()
                    .WithColumn(nameof(ForumTopic.Published)).AsBoolean().NotNullable();

                Create.Index("IX_ForumId_Published")
                    .OnTable(forumTopic)
                    .OnColumn(nameof(ForumTopic.ForumId)).Ascending()
                    .OnColumn(nameof(ForumTopic.Published)).Ascending()
                    .WithOptions()
                    .NonClustered();

                Create.Index("IX_TopicTypeId_LastPostTime")
                    .OnTable(forumTopic)
                    .OnColumn(nameof(ForumTopic.TopicTypeId)).Ascending()
                    .OnColumn(nameof(ForumTopic.LastPostTime)).Ascending()
                    .WithOptions()
                    .NonClustered();
            }

            // ForumPost.
            if (!Schema.Table(forumPost).Exists())
            {
                Create.Table(forumPost)
                    .WithIdColumn()
                    .WithColumn(nameof(ForumPost.TopicId)).AsInt32().NotNullable()
                        .Indexed().ForeignKey(forumTopic, id).OnDelete(Rule.Cascade)
                    .WithColumn(nameof(ForumPost.CustomerId)).AsInt32().NotNullable()
                        .Indexed().ForeignKey(nameof(Customer), id)
                    .WithColumn(nameof(ForumPost.Text)).AsString(int.MaxValue).NotNullable()
                    .WithColumn(nameof(ForumPost.IPAddress)).AsString(100).Nullable()
                    .WithColumn(nameof(ForumPost.CreatedOnUtc)).AsDateTime2().NotNullable()
                        .Indexed("IX_CreatedOnUtc")
                    .WithColumn(nameof(ForumPost.UpdatedOnUtc)).AsDateTime2().NotNullable()
                    .WithColumn(nameof(ForumPost.Published)).AsBoolean().NotNullable()
                        .Indexed("IX_Published");
            }

            // ForumSubscription.
            if (!Schema.Table(forumSubscription).Exists())
            {
                Create.Table(forumSubscription)
                    .WithIdColumn()
                    .WithColumn(nameof(ForumSubscription.SubscriptionGuid)).AsGuid().NotNullable()
                    .WithColumn(nameof(ForumSubscription.CustomerId)).AsInt32().NotNullable()
                        .Indexed().ForeignKey(nameof(Customer), id)
                    .WithColumn(nameof(ForumSubscription.ForumId)).AsInt32().NotNullable()
                        .Indexed("IX_Forums_Subscription_ForumId")
                    .WithColumn(nameof(ForumSubscription.TopicId)).AsInt32().NotNullable()
                        .Indexed("IX_Forums_Subscription_TopicId")
                    .WithColumn(nameof(ForumPost.CreatedOnUtc)).AsDateTime2().NotNullable();
            }

            // ForumPostVote.
            if (!Schema.Table(forumPostVote).Exists())
            {
                Create.Table(forumPostVote)
                    .WithIdColumn()
                    .WithColumn(nameof(ForumPostVote.ForumPostId)).AsInt32().NotNullable()
                        .Indexed().ForeignKey(forumPost, id).OnDelete(Rule.Cascade)
                    .WithColumn(nameof(ForumPostVote.Vote)).AsBoolean().NotNullable();

                Create.ForeignKey()
                    .FromTable(forumPostVote).ForeignColumn(id)
                    .ToTable(nameof(CustomerContent)).PrimaryColumn(id);
            }

            // PrivateMessage.
            if (!Schema.Table(privateMessage).Exists())
            {
                Create.Table(privateMessage)
                    .WithIdColumn()
                    .WithColumn(nameof(PrivateMessage.StoreId)).AsInt32().NotNullable()
                    .WithColumn(nameof(PrivateMessage.FromCustomerId)).AsInt32().NotNullable()
                        .Indexed().ForeignKey(nameof(Customer), id)
                    .WithColumn(nameof(PrivateMessage.ToCustomerId)).AsInt32().NotNullable()
                        .Indexed().ForeignKey(nameof(Customer), id)
                    .WithColumn(nameof(PrivateMessage.Subject)).AsString(450).NotNullable()
                    .WithColumn(nameof(PrivateMessage.Text)).AsString(int.MaxValue).NotNullable()
                    .WithColumn(nameof(PrivateMessage.IsRead)).AsBoolean().NotNullable()
                    .WithColumn(nameof(PrivateMessage.IsDeletedByAuthor)).AsBoolean().NotNullable()
                    .WithColumn(nameof(PrivateMessage.IsDeletedByRecipient)).AsBoolean().NotNullable()
                    .WithColumn(nameof(PrivateMessage.CreatedOnUtc)).AsDateTime2().NotNullable();
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave forum schema as it is or ask merchant to delete it.
        }
    }
}

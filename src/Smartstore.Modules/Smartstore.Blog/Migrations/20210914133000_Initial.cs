using System.Data;
using FluentMigrator;
using Smartstore.Blog.Domain;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Domain;

namespace Smartstore.Blog.Migrations
{
    [MigrationVersion("2021-09-14 13:30:00", "Blog: Initial")]
    internal class Initial : Migration
    {
        // TODO: (mh) (core) Check if database was initialized correctly when sample data is finished.
        public override void Up()
        {
            const string blogPost = "BlogPost";
            const string blogComment = "BlogComment";
            
            const string id = nameof(BaseEntity.Id);

            if (!Schema.Table(blogPost).Exists())
            {
                Create.Table(blogPost)
                    .WithColumn(id).AsInt32().PrimaryKey().Identity().NotNullable()
                    .WithColumn(nameof(BlogPost.Title)).AsString(4000).NotNullable()
                    .WithColumn(nameof(BlogPost.Body)).AsString(int.MaxValue).Nullable()
                    .WithColumn(nameof(BlogPost.AllowComments)).AsBoolean().NotNullable()
                    .WithColumn(nameof(BlogPost.ApprovedCommentCount)).AsInt32().NotNullable()
                    .WithColumn(nameof(BlogPost.NotApprovedCommentCount)).AsInt32().NotNullable()
                    .WithColumn(nameof(BlogPost.Tags)).AsString(4000).Nullable()
                    .WithColumn(nameof(BlogPost.StartDateUtc)).AsDateTime2().Nullable()
                    .WithColumn(nameof(BlogPost.EndDateUtc)).AsDateTime2().Nullable()
                    .WithColumn(nameof(BlogPost.MetaKeywords)).AsString(400).Nullable()
                    .WithColumn(nameof(BlogPost.MetaDescription)).AsString(4000).Nullable()
                    .WithColumn(nameof(BlogPost.MetaTitle)).AsString(400).Nullable()
                    .WithColumn(nameof(BlogPost.LimitedToStores)).AsBoolean().NotNullable()
                    .WithColumn(nameof(BlogPost.CreatedOnUtc)).AsDateTime2().Nullable()
                    .WithColumn(nameof(BlogPost.SectionBg)).AsString(100).Nullable()
                    .WithColumn(nameof(BlogPost.Intro)).AsString(int.MaxValue).Nullable()
                    .WithColumn(nameof(BlogPost.DisplayTagsInPreview)).AsBoolean().NotNullable()
                    .WithColumn(nameof(BlogPost.IsPublished)).AsBoolean().NotNullable()
                    .WithColumn(nameof(BlogPost.PreviewDisplayType)).AsInt32().NotNullable()
                    .WithColumn(nameof(BlogPost.MediaFileId)).AsInt32().Nullable()
                        .Indexed("IX_MediaFileId")
                        .ForeignKey(nameof(MediaFile), id).OnDelete(Rule.None)
                    .WithColumn(nameof(BlogPost.PreviewMediaFileId)).AsInt32().Nullable()
                        .Indexed("IX_PreviewMediaFileId")
                        .ForeignKey(nameof(MediaFile), id).OnDelete(Rule.None)
                    .WithColumn(nameof(BlogPost.LanguageId)).AsInt32().NotNullable()
                        .Indexed("IX_LanguageId")
                        .ForeignKey(nameof(Language), id).OnDelete(Rule.None);
            }

            if (!Schema.Table(blogComment).Exists())
            {
                Create.Table(blogComment)
                    .WithColumn(id).AsInt32().PrimaryKey().Identity().NotNullable()
                        .Indexed("IX_Id")
                        .ForeignKey(nameof(CustomerContent), id).OnDelete(Rule.None)
                    .WithColumn(nameof(BlogComment.CommentText)).AsString(int.MaxValue).Nullable()
                    .WithColumn(nameof(BlogComment.BlogPostId)).AsInt32().NotNullable()
                        .Indexed("IX_BlogPostId")
                        .ForeignKey(blogPost, id).OnDelete(Rule.None);
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave blog schema as it is or ask merchant to delete it.
        }
    }
}

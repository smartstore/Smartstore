using System.Data;
using FluentMigrator;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Domain;

namespace Smartstore.News.Migrations
{
    [MigrationVersion("2021-09-20 15:30:00", "News: Initial")]
    internal class Initial : Migration
    {
        public override void Up()
        {
            const string newsItem = "News";
            const string newsComment = "NewsComment";

            const string id = nameof(BaseEntity.Id);

            if (!Schema.Table(newsItem).Exists())
            {
                Create.Table(newsItem)
                    .WithIdColumn()
                    .WithColumn(nameof(NewsItem.Title)).AsString(450).NotNullable()
                        .Indexed("IX_Title")
                    .WithColumn(nameof(NewsItem.Short)).AsString(4000).NotNullable()
                    .WithColumn(nameof(NewsItem.Full)).AsMaxString().NotNullable()
                    .WithColumn(nameof(NewsItem.Published)).AsBoolean().NotNullable()
                    .WithColumn(nameof(NewsItem.StartDateUtc)).AsDateTime2().Nullable()
                    .WithColumn(nameof(NewsItem.EndDateUtc)).AsDateTime2().Nullable()
                    .WithColumn(nameof(NewsItem.AllowComments)).AsBoolean().NotNullable()
                    .WithColumn(nameof(NewsItem.ApprovedCommentCount)).AsInt32().NotNullable()
                    .WithColumn(nameof(NewsItem.NotApprovedCommentCount)).AsInt32().NotNullable()
                    .WithColumn(nameof(NewsItem.LimitedToStores)).AsBoolean().NotNullable()
                    .WithColumn(nameof(NewsItem.CreatedOnUtc)).AsDateTime2().NotNullable()
                    .WithColumn(nameof(NewsItem.MetaKeywords)).AsString(400).Nullable()
                    .WithColumn(nameof(NewsItem.MetaDescription)).AsString(4000).Nullable()
                    .WithColumn(nameof(NewsItem.MetaTitle)).AsString(400).Nullable()
                    .WithColumn(nameof(NewsItem.MediaFileId)).AsInt32().Nullable()
                        .Indexed("IX_MediaFileId")
                        .ForeignKey(nameof(MediaFile), id).OnDelete(Rule.None)
                    .WithColumn(nameof(NewsItem.PreviewMediaFileId)).AsInt32().Nullable()
                        .Indexed("IX_PreviewMediaFileId")
                        .ForeignKey(nameof(MediaFile), id).OnDelete(Rule.None)
                    .WithColumn(nameof(NewsItem.LanguageId)).AsInt32().Nullable()
                        .Indexed("IX_LanguageId")
                        .ForeignKey(nameof(Language), id).OnDelete(Rule.None);
            }

            if (!Schema.Table(newsComment).Exists())
            {
                Create.Table(newsComment)
                    .WithIdColumn()
                    .WithColumn(nameof(NewsComment.CommentTitle)).AsString(450).Nullable()
                    .WithColumn(nameof(NewsComment.CommentText)).AsMaxString().Nullable()
                    .WithColumn(nameof(NewsComment.NewsItemId)).AsInt32().NotNullable()
                        .Indexed().ForeignKey(newsItem, id).OnDelete(Rule.Cascade);

                Create.ForeignKey()
                    .FromTable(newsComment).ForeignColumn(id)
                    .ToTable(nameof(CustomerContent)).PrimaryColumn(id);
            }
        }

        public override void Down()
        {
            // INFO: no down initial migration. Leave news schema as it is or ask merchant to delete it.
        }
    }
}

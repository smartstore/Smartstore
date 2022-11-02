using FluentMigrator;
using Smartstore.Core.Messaging;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2022-08-31 21:15:00", "Core: MailAttachmentContentId")]
    internal class MailAttachmentContentId : Migration
    {
        public override void Up()
        {
            var tableName = nameof(QueuedEmailAttachment);
            var isEmbeddedName = nameof(QueuedEmailAttachment.IsEmbedded);
            var contentIdName = nameof(QueuedEmailAttachment.ContentId);

            if (!Schema.Table(tableName).Column(isEmbeddedName).Exists())
            {
                Create.Column(isEmbeddedName).OnTable(tableName).AsBoolean().NotNullable().WithDefaultValue(false);
            }

            if (!Schema.Table(tableName).Column(contentIdName).Exists())
            {
                Create.Column(contentIdName).OnTable(tableName).AsString(64).Nullable();
            }
        }

        public override void Down()
        {
        }
    }
}

using FluentMigrator;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Messaging;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-10-03 12:00:00", "Core: Queued email attachment pending state")]
internal class QueuedEmailAttachmentIsPending : Migration
{
    const string TableName = nameof(QueuedEmailAttachment);
    const string ColumnName = nameof(QueuedEmailAttachment.IsPending);

    public override void Up()
    {
        if (!Schema.Table(TableName).Column(ColumnName).Exists())
        {
            Create.Column(ColumnName)
                .OnTable(TableName)
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);
        }
    }

    public override void Down()
    {
        if (Schema.Table(TableName).Column(ColumnName).Exists())
        {
            Delete.Column(ColumnName).FromTable(TableName);
        }
    }
}
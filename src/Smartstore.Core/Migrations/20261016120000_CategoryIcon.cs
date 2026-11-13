using FluentMigrator;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-10-16 12:00:00", "Core: Category icon")]
internal class CategoryIcon : Migration
{
    const string TableName = nameof(Category);
    const string ColumnName = nameof(Category.Icon);

    public override void Up()
    {
        if (!Schema.Table(TableName).Column(ColumnName).Exists())
        {
            Create.Column(ColumnName)
                .OnTable(TableName)
                .AsString(512)
                .Nullable();
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

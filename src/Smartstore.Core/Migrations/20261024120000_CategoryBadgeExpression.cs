using FluentMigrator;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Data;
using Smartstore.Data;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-10-24 12:00:00", "Core: Category badge expression")]
internal class CategoryBadgeExpression : Migration
{
    const string TableName = nameof(Category);
    const string ColumnName = nameof(Category.BadgeStyle);

    private readonly SmartDbContext _db;

    public CategoryBadgeExpression(SmartDbContext db)
    {
        _db = db;
    }

    public override void Up()
    {
        // SQLite is used for disposable test databases and is not upgraded here.
        if (_db.DataProvider.ProviderType == DbSystemType.SQLite)
        {
            return;
        }

        if (!Schema.Table(TableName).Column(ColumnName).Exists())
        {
            return;
        }

        // Remove the old numeric default before changing the column type.
        Delete.DefaultConstraint().OnTable(TableName).OnColumn(ColumnName);

        Alter.Column(ColumnName)
            .OnTable(TableName)
            .AsString(100)
            .Nullable();

        var table = _db.DataProvider.EncloseIdentifier(TableName);
        var column = _db.DataProvider.EncloseIdentifier(ColumnName);
        var badgeText = _db.DataProvider.EncloseIdentifier(nameof(Category.BadgeText));

        // Convert all legacy values in one update, independently of the UI enum.
        Execute.Sql($"""
            UPDATE {table}
            SET {column} = CASE
                WHEN {badgeText} IS NULL OR TRIM({badgeText}) = '' THEN NULL
                WHEN {column} = '0' THEN 'secondary'
                WHEN {column} = '1' THEN 'primary'
                WHEN {column} = '2' THEN 'success'
                WHEN {column} = '3' THEN 'info'
                WHEN {column} = '4' THEN 'warning'
                WHEN {column} = '5' THEN 'danger'
                WHEN {column} = '6' THEN 'light'
                WHEN {column} = '7' THEN 'dark'
                ELSE {column}
            END
            WHERE {column} IN ('0', '1', '2', '3', '4', '5', '6', '7')
                OR ({column} IS NOT NULL AND ({badgeText} IS NULL OR TRIM({badgeText}) = ''))
            """);
    }

    public override void Down()
    {
        // Converting back to an integer would discard persisted badge modifiers.
    }
}

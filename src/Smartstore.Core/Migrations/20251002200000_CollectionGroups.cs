using FluentMigrator;
using Smartstore.Core.Common;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2025-10-02 20:00:00", "Core: Collection groups")]
    internal class CollectionGroups : Migration
    {
        public override void Up()
        {
            const string tableName = "CollectionGroup";

            if (!Schema.Table(tableName).Exists())
            {
                Create.Table(tableName)
                    .WithIdColumn()
                    .WithColumn(nameof(CollectionGroup.EntityId)).AsInt32().NotNullable()
                    .WithColumn(nameof(CollectionGroup.EntityName)).AsString(100).NotNullable()
                    .WithColumn(nameof(CollectionGroup.Name)).AsString(400).NotNullable()
                    .WithColumn(nameof(CollectionGroup.DisplayOrder)).AsInt32().NotNullable()
                        .Indexed();

                Create.Index()
                    .OnTable(tableName)
                    .OnColumn(nameof(CollectionGroup.EntityId)).Ascending()
                    .OnColumn(nameof(CollectionGroup.EntityName)).Ascending()
                    .WithOptions()
                    .NonClustered();
            }
        }

        public override void Down()
        {            
        }
    }
}

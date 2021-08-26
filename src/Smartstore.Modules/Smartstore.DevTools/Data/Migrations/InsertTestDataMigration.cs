using System;
using Smartstore.Core.Data.Migrations;

namespace Smartstore.DevTools.Data.Migrations
{
    [MigrationVersion("2021-08-19 14:39:12", "Insert DevTools test data")]
    public class InsertTestDataMigration : DatabaseMigrationBase
    {
        public override void Up()
        {
            Insert.IntoTable("DevToolsTestEntity").Row(new 
            {
                Name = "My first data row.", 
                LimitedToStores = false,
                SubjectToAcl = false,
                Published = true,
                Deleted = false,
                DisplayOrder = 50,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                IsActive = true,
                Notes = "Hello world."
            });

            Insert.IntoTable("DevToolsTestEntity").Row(new
            {
                Name = "Progressively supply pandemic strategic theme areas through competitive metrics. Appropriately simplify standards compliant.",
                Description = "Assertively repurpose timely e-commerce after open-source ideas. Conveniently seize wireless solutions rather than integrated channels. Objectively architect.",
                LimitedToStores = false,
                SubjectToAcl = false,
                Published = true,
                Deleted = false,
                DisplayOrder = 60,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
                IsActive = false
            });
        }

        public override void Down()
        {
            Delete.FromTable("DevToolsTestEntity").Row(new { DisplayOrder = 60 });
            Delete.FromTable("DevToolsTestEntity").Row(new { Name = "My first data row." });
        }
    }
}

using System.Data;
using FluentMigrator;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Content.Media;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2023-04-04 15:00:00", "Core: TreeNode")]
    internal class TreeNodeMigration : Migration, IDataSeeder<SmartDbContext>
    {
        public override void Up()
        {
            FixInvalidParentCategoryIds();

            // Make Category.ParentCategoryId nullable and create FK
            Alter.Column("ParentCategoryId").OnTable(nameof(Category)).AsInt32().Nullable();

            // Update ParentCategoryId 0 --> NULL
            Update.Table(nameof(Category))
                .Set(new { ParentCategoryId = DBNull.Value })
                .Where(new { ParentCategoryId = 0 });

            // Add TreePath to Category and MediaFolder with indexes
            Create.Column("TreePath").OnTable(nameof(Category)).AsString(400).NotNullable().WithDefaultValue("")
                .Indexed("IX_Category_TreePath");
            Create.Column("TreePath").OnTable(nameof(MediaFolder)).AsString(400).NotNullable().WithDefaultValue("")
                .Indexed("IX_TreePath");

            // Create FK for Category.ParentCategoryId
            Create.ForeignKey()
                .FromTable(nameof(Category)).ForeignColumn("ParentCategoryId")
                .ToTable(nameof(Category)).PrimaryColumn("Id")
                .OnDelete(Rule.None);
        }

        public override void Down()
        {
            // Down will possibly fail
        }

        public DataSeederStage Stage => DataSeederStage.Late;
        public bool AbortOnFailure => false;

        public Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            return CategoryService.RebuidTreePathsAsync(context, cancelToken);
        }

        private void FixInvalidParentCategoryIds()
        {
            try
            {
                IfDatabase("sqlserver").Execute.Sql(@"
                    UPDATE [dbo].[Category] SET ParentCategoryId = 0
                    WHERE [Id] In (SELECT [Id] FROM [dbo].[Category] c1 WHERE c1.ParentCategoryId <> 0 AND NOT EXISTS (SELECT 1 FROM [dbo].[Category] c2 WHERE c2.Id = c1.ParentCategoryId))
                ");
            }
            catch
            {
            }
        }
    }
}
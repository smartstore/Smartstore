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

        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            var folders = await context.MediaFolders
                .Include(x => x.Children)
                .Where(x => x.ParentId == null)
                .ToListAsync(cancelToken);

            foreach (var folder in folders)
            {
                BuildTreePath(folder);
            }
            await context.SaveChangesAsync(cancelToken);

            var categories = await context.Categories
                .Include(x => x.Children)
                .Where(x => x.ParentId == null)
                .ToListAsync(cancelToken);

            foreach (var category in categories)
            {
                BuildTreePath(category);
            }
            await context.SaveChangesAsync(cancelToken);

            static void BuildTreePath(ITreeNode node)
            {
                node.TreePath = node.BuildTreePath();
                var childNodes = node.GetChildNodes();
                if (!childNodes.IsNullOrEmpty())
                {
                    foreach (var childNode in childNodes)
                    {
                        BuildTreePath(childNode);
                    }
                }
            }
        }
    }
}
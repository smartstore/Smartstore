// using FluentMigrator;
// using Smartstore.Core.Content.Menus;
// using Smartstore.Core.Data;
// using Smartstore.Core.Data.Migrations;
// using Smartstore.Core.Data.Migrations.Icons;
// using Smartstore.Data.Migrations;
//
// namespace Smartstore.Core.Migrations;
//
// [MigrationVersion("2026-10-17 12:00:00", "Core: Icon migration")]
// internal class IconMigration : Migration, IDataSeeder<SmartDbContext>
// {
//     const string TableName = "MenuItemRecord";
//     const string ColumnName = nameof(MenuItemEntity.Icon);
//
//     private readonly IIconMigrator _iconMigrator;
//
//     public IconMigration(IIconMigrator iconMigrator)
//     {
//         _iconMigrator = iconMigrator;
//     }
//
//     public override void Up()
//     {
//         if (Schema.Table(TableName).Column(ColumnName).Exists())
//         {
//             Alter.Column(ColumnName)
//                 .OnTable(TableName)
//                 .AsString(512)
//                 .Nullable();
//         }
//     }
//
//     public override void Down()
//     {
//         // Keep the expanded column to avoid truncating persisted icon expressions.
//     }
//
//     // Run the potentially large data conversion after schema migration, without the early seeder timeout.
//     public DataSeederStage Stage => DataSeederStage.Late;
//     public bool AbortOnFailure => false;
//
//     public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
//     {
//         // The migration framework owns this DbContext. The same service also supports later manual runs.
//         // Report persistence is not connected yet; conversion warnings remain non-fatal.
//         await _iconMigrator.MigrateAsync(context, cancelToken: cancelToken);
//     }
// }

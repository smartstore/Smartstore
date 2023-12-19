using FluentMigrator;
using Smartstore.Core.Localization;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2023-01-05 18:00:00", "Core: Remove translation dupes")]
    internal class RemoveTranslationDupes : Migration, IDataSeeder<SmartDbContext>
    {
        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public override void Up()
        {
        }

        public override void Down()
        {
        }

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            try
            {
                var dupes = (await context.LocaleStringResources
                    .GroupBy(x => new { x.LanguageId, x.ResourceName })
                    .ToListAsync(cancelToken))
                    .SelectMany(grp => grp.Skip(1))
                    .ToList();

                context.LocaleStringResources.RemoveRange(dupes);
                await context.SaveChangesAsync(cancelToken);
            }
            catch
            {
            }
            finally
            {
                context.DetachEntities<LocaleStringResource>();
            }
        }
    }
}
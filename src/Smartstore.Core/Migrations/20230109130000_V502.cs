using FluentMigrator;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2023-01-09 13:00:00", "V502")]
    internal class V502 : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        public override void Up()
        {
        }

        public override void Down()
        {
        }

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Products.Price.OfferCountdown",
                "Ends in <b class=\"fwm\">{0}</b>",
                "Endet in <b class=\"fwm\">{0}</b>");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.CustomerNameFormat.Hint",
                "Sets the customer's display name to be used for public content such as product reviews, comments, etc..",
                "Legt den Anzeigenamen des Kunden fest, der für öffentliche Inhalte wie Produktbewertungen, Kommentare, etc. verwendet wird.");

            builder.AddOrUpdate("Admin.System.Maintenance.DbBackup.SameServerNote",
                "Backups and restores of databases are only possible if the database server (e.g. MS SQL Server or MySQL) and the physical location of the store installation are on the same server.",
                "Sicherungen und Wiederherstellungen von Datenbanken sind nur möglich, wenn sich der Datenbankserver (z.B. MS SQL Server oder MySQL) und der physikalische Speicherort der Shop-Installation auf einem gemeinsamen Server befinden.");
        }
    }
}

using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await MigrateSettingsAsync(context, cancelToken);
        }

        public Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            return Task.CompletedTask;
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("AriaLabel.MainNavigation", "Main navigation", "Hauptnavigation");
            builder.AddOrUpdate("AriaLabel.SearchBox",
                "Type in a search term and press Enter to search for products.",
                "Geben Sie einen Suchbegriff ein und drücken Sie die Eingabetaste, um nach Produkten zu suchen.");

            builder.AddOrUpdate("Search.SearchBox.Clear", "Clear search term", "Suchbegriff löschen");
            builder.AddOrUpdate("Common.ScrollUp", "Scroll up", "Nach oben scrollen");

            builder.AddOrUpdate("Admin.Configuration.Settings.General.Common.Captcha.Hint",
                "CAPTCHAs are used for security purposes to help distinguish between human and machine users. They are typically used to verify that internet forms are being"
                + " filled out by humans and not robots (bots), which are often misused for this purpose. reCAPTCHA accounts are created at <a"
                + " class=\"alert-link\" href=\"https://cloud.google.com/security/products/recaptcha?hl=en\" target=\"_blank\">Google</a>. Select <b>Task (v2)</b> as the reCAPTCHA type.",
                "CAPTCHAs dienen der Sicherheit, indem sie dabei helfen, zu unterscheiden, ob ein Nutzer ein Mensch oder eine Maschine ist. In der Regel wird diese Funktion genutzt,"
                + " um zu überprüfen, ob Internetformulare von Menschen oder Roboter (Bots) ausgefüllt werden, da Bots hier oft missbräuchlich eingesetzt werden."
                + " reCAPTCHA-Konten werden bei <a class=\"alert-link\" href=\"https://cloud.google.com/security/products/recaptcha?hl=de\" target=\"_blank\">Google</a>"
                + " angelegt. Wählen Sie als reCAPTCHA-Typ <b>Aufgabe (v2)</b> aus.");
        }
    }
}
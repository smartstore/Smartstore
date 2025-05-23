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
            builder.AddOrUpdate("AriaLabel.ShowPreviousProducts",
                "Show previous product group",
                "Vorherige Produktgruppe anzeigen");
            builder.AddOrUpdate("AriaLabel.ShowNextProducts",
                "Show next product group",
                "Nächste Produktgruppe anzeigen");

            builder.AddOrUpdate("AriaDescription.SearchBox",
                "Enter a search term. Press the Enter key to view all the results.",
                "Geben Sie einen Suchbegriff ein. Drücken Sie die Eingabetaste, um alle Ergebnisse aufzurufen.");
            builder.AddOrUpdate("AriaDescription.InstantSearch",
                "Results will appear automatically as you type.",
                "Während Sie tippen, erscheinen automatisch erste Ergebnisse.");
            builder.AddOrUpdate("AriaDescription.AutoSearchBox",
                "Enter a search term. Results will appear automatically as you type.",
                "Geben Sie einen Suchbegriff ein. Die Ergebnisse erscheinen automatisch, während Sie tippen.");

            builder.AddOrUpdate("Search.SearchBox.Clear", "Clear search term", "Suchbegriff löschen");
            builder.AddOrUpdate("Common.ScrollUp", "Scroll up", "Nach oben scrollen");
            builder.AddOrUpdate("Common.SelectAction", "Select action", "Aktion wählen");

            builder.AddOrUpdate("Admin.Configuration.Settings.General.Common.Captcha.Hint",
                "CAPTCHAs are used for security purposes to help distinguish between human and machine users. They are typically used to verify that internet forms are being"
                + " filled out by humans and not robots (bots), which are often misused for this purpose. reCAPTCHA accounts are created at <a"
                + " class=\"alert-link\" href=\"https://cloud.google.com/security/products/recaptcha?hl=en\" target=\"_blank\">Google</a>. Select <b>Task (v2)</b> as the reCAPTCHA type.",
                "CAPTCHAs dienen der Sicherheit, indem sie dabei helfen, zu unterscheiden, ob ein Nutzer ein Mensch oder eine Maschine ist. In der Regel wird diese Funktion genutzt,"
                + " um zu überprüfen, ob Internetformulare von Menschen oder Roboter (Bots) ausgefüllt werden, da Bots hier oft missbräuchlich eingesetzt werden."
                + " reCAPTCHA-Konten werden bei <a class=\"alert-link\" href=\"https://cloud.google.com/security/products/recaptcha?hl=de\" target=\"_blank\">Google</a>"
                + " angelegt. Wählen Sie als reCAPTCHA-Typ <b>Aufgabe (v2)</b> aus.");

            builder.AddOrUpdate("Polls.TotalVotes", "{0} votes cast.", "{0} abgegebene Stimmen.");

            builder.AddOrUpdate("Blog.RSS.Hint",
                "Opens the RSS feed with the latest blog posts. Subscribe with an RSS reader to stay informed.",
                "Öffnet den RSS-Feed mit aktuellen Blogbeiträgen. Mit einem RSS-Reader abonnieren und informiert bleiben.");

            builder.AddOrUpdate("News.RSS.Hint",
                "Opens the RSS feed with the latest news. Subscribe with an RSS reader to stay informed.",
                "Öffnet den RSS-Feed mit aktuellen News. Mit einem RSS-Reader abonnieren und informiert bleiben.");
        }
    }
}
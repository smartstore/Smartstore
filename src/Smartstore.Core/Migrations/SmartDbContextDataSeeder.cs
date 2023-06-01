using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            //await MigrateSettingsAsync(context, cancelToken);
        }

        //public async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        //{
        //    await context.SaveChangesAsync(cancelToken);
        //}

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Account.Fields.Password", "Password", "Passwort");
            builder.AddOrUpdate("Account.Fields.PasswordSecurity", "Password security", "Passwortsicherheit");

            builder.AddOrUpdate("Account.Register.Result.AlreadyRegistered", "You are already registered.", "Sie sind bereits registriert.");
            builder.AddOrUpdate("Admin.Common.Cleanup", "Cleanup", "Aufräumen");

            builder.AddOrUpdate("Admin.System.QueuedEmails.DeleteAll.Confirm",
                "Are you sure you want to delete all sent or undeliverable emails?",
                "Sind Sie sicher, dass alle gesendeten oder unzustellbaren E-Mails gelöscht werden sollen?");

            builder.AddOrUpdate("Admin.ContentManagement.Menus.Item.InvalidTargetLink",
                "Unknown or invalid target \"{0}\" at menu link \"{1}\".",
                "Unbekanntes oder ungültiges Ziel \"{0}\" bei Menü-Link \"{1}\".");

            builder.AddOrUpdate("Account.Fields.StreetAddress2", "Street address 2", "Adresszusatz");
            builder.AddOrUpdate("Address.Fields.Address2", "Address 2", "Adresszusatz");
            builder.AddOrUpdate("Address.Fields.Address2.Required", "Address 2 is required.", "Adresszusatz wird benötigt");
            builder.AddOrUpdate("Admin.Address.Fields.Address2", "Address 2", "Adresszusatz");
            builder.AddOrUpdate("Admin.Address.Fields.Address2.Hint", "Enter address 2", "Adresszusatz bzw. zweite Adresszeile");
            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.StreetAddress2Enabled", 
                "'Street address addition' enabled", 
                "\"Adresszusatz\" aktiv");
            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.StreetAddress2Enabled.Hint", 
                "Defines whether the input of 'street address addition' is enabled.", 
                "Legt fest, ob das Eingabefeld \"Adresszusatz\" aktiviert ist");
            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.StreetAddress2Required", 
                "'Street address addition' required", "\"Adresszusatz\" ist erforderlich");
            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.StreetAddress2Required.Hint", 
                "Defines whether 'street address addition' is required.", 
                "Legt fest, ob die Eingabe von \"Adresszusatz\" erforderlich ist.");
            builder.AddOrUpdate("Admin.Customers.Customers.Fields.StreetAddress2", "Address 2", "Adresszusatz");
            builder.AddOrUpdate("Admin.Customers.Customers.Fields.StreetAddress2.Hint", "The address 2.", "Adresszusatz");
            builder.AddOrUpdate("Admin.Orders.Address.Address2", "Address 2", "Adresszusatz");
            builder.AddOrUpdate("PDFPackagingSlip.Address2", "Address 2: {0}", "Adresszusatz: {0}");

            var generalCommon = "Admin.Configuration.Settings.GeneralCommon";
            var socialSettings = "Admin.Configuration.Settings.GeneralCommon.SocialSettings";

            builder.Delete(
                $"{socialSettings}.FacebookLink.Hint",
                $"{socialSettings}.InstagramLink.Hint",
                $"{socialSettings}.PinterestLink.Hint",
                $"{socialSettings}.TwitterLink.Hint",
                $"{socialSettings}.YoutubeLink.Hint");

            builder.AddOrUpdate($"{socialSettings}.LeaveEmpty", 
                "Leave empty to hide the link.", 
                "Leer lassen, um den Link auszublenden.");

            builder.AddOrUpdate($"{socialSettings}.FlickrLink", "Flickr Link", "Flickr Link");
            builder.AddOrUpdate($"{socialSettings}.LinkedInLink", "LinkedIn Link", "LinkedIn Link");
            builder.AddOrUpdate($"{socialSettings}.XingLink", "Xing Link", "Xing Link");
            builder.AddOrUpdate($"{socialSettings}.TikTokLink", "TikTok Link", "TikTok Link");
            builder.AddOrUpdate($"{socialSettings}.SnapchatLink", "Snapchat Link", "Snapchat Link");
            builder.AddOrUpdate($"{socialSettings}.VimeoLink", "Vimeo Link", "Vimeo Link");

            builder.AddOrUpdate($"{generalCommon}").Value("en", "General settings");
            builder.AddOrUpdate($"{generalCommon}.SecuritySettings").Value("en", "Security");
            builder.AddOrUpdate($"{generalCommon}.LocalizationSettings").Value("en", "Localization");
            builder.AddOrUpdate($"{generalCommon}.PdfSettings").Value("en", "PDF");

            var seoSettings = $"{generalCommon}.SEOSettings";

            builder.AddOrUpdate($"{seoSettings}", "SEO", "SEO");
            builder.AddOrUpdate($"{seoSettings}.Routing", "Internal links", "Interne Links");
            builder.AddOrUpdate($"{generalCommon}.AppendTrailingSlashToUrls",
                "Append trailing slash to links",
                "Links mit Schrägstrich abschließen",
                "Forces all internal links to end with a trailing slash.",
                "Erzwingt, dass alle internen Links mit einem Schrägstrich abschließen.");
            builder.AddOrUpdate($"{generalCommon}.TrailingSlashRule",
                "Trailing slash mismatch rule",
                "Regel für Nichtübereinstimmung",
                "Rule to apply when an incoming URL does not match the 'Append trailing slash to links' setting.",
                "Regel, die angewendet werden soll, wenn eine eingehende URL nicht mit der Option 'Links mit Schrägstrich abschließen' übereinstimmt.");

            builder.AddOrUpdate($"{seoSettings}.RestartInfo",
                "Changing link options will take effect only after restarting the application. Also, the XML sitemap should be regenerated to reflect the changes.",
                "Das Ändern von Link-Optionen wird erst nach einen Neustart der Anwendung wirksam. Außerdem sollte die XML Sitemap neu generiert werden.");

            builder.AddOrUpdate("Enums.TrailingSlashRule.Allow", "Allow", "Erlauben");
            builder.AddOrUpdate("Enums.TrailingSlashRule.Redirect", "Redirect (recommended)", "Weiterleiten (empfohlen)");
            builder.AddOrUpdate("Enums.TrailingSlashRule.RedirectToHome", "Redirect to home", "Zur Startseite weiterleiten");
            builder.AddOrUpdate("Enums.TrailingSlashRule.Disallow", "Disallow (HTTP 404)", "Nicht zulassen (HTTP 404)");
        }
    }
}
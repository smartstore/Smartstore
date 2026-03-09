using Smartstore.Core.Common.Configuration;
using Smartstore.Data.Migrations;
using Smartstore.Utilities;

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
            return context.MigrateSettingsAsync(builder =>
            {
                builder.Add(TypeHelper.NameOf<CommonSettings>(x => x.MinLogLevelToRetain, true), LogLevel.Error);
            });
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Orders.Products.AppliedDiscounts",
                "The following discounts were applied to the products: {0}.",
                "Auf die Produkte wurden die folgenden Rabatte gewährt: {0}.");

            builder.AddOrUpdate("Identity.Error.PasswordRequiresDigit",
                "At least one number (0–9)", 
                "Mindestens eine Ziffer (0–9)");

            builder.AddOrUpdate("Identity.Error.PasswordRequiresLower",
                "At least one lowercase letter (a–z)",
                "Mindestens ein Kleinbuchstabe (a–z)");

            builder.AddOrUpdate("Identity.Error.PasswordRequiresNonAlphanumeric",
                "At least one special character (e.g. !@#$)",
                "Mindestens ein Sonderzeichen (z.B. !@#$)");

            builder.AddOrUpdate("Identity.Error.PasswordRequiresUniqueChars",
                "At least {0} unique characters",
                "Mindestens {0} eindeutige Zeichen");

            builder.AddOrUpdate("Identity.Error.PasswordRequiresUpper",
                "At least one uppercase letter (A–Z)",
                "Mindestens ein Großbuchstabe (A–Z)");

            builder.AddOrUpdate("Identity.Error.PasswordTooShort",
                "At least {0} characters",
                "Mindestens {0} Zeichen");

            builder.AddOrUpdate("Account.Register.Result.MeetPasswordRules",
                "Password must meet these rules: {0}",
                "Passwort muss diese Regeln erfüllen: {0}");

            builder.AddOrUpdate("Admin.OrderNotice.OrderPlacedUcp",
                "The order was placed using UCP (Agentic Commerce) \"{0}\". The payment token {1} was processed without a user interface.",
                "Bestellung ist über UCP (Agentic Commerce) \"{0}\" eingegangen. Das Zahlungstoken {1} wurde ohne Benutzeroberfläche verarbeitet.");

            builder.AddOrUpdate("Order.Product(s).OrderedQuantity", "Ordered", "Bestellt");

            builder.AddOrUpdate("Admin.AI.TextCreation.Organize",
                "Organize",
                "Gliedern");

            builder.AddOrUpdate("Smartstore.AI.Prompts.EnsureLogicalFlow",
                "Add headings (h2-h6) where appropriate, break long paragraphs, use lists for enumerations, ensure logical flow.",
                "Füge passende Überschriften (h2-h6) ein, unterteile lange Absätze, nutze Listen für Aufzählungen und stelle einen logischen Ablauf sicher.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.AssignIdToHeader",
                "Assign each heading a unique, concise id attribute in kebab-case format based on core content keywords. On ID collision: number them (e.g., id=\"benefits-1\", id=\"benefits-2\").",
                "Vergib für jede Überschrift ein eindeutiges, prägnantes id-Attribut im \"kebab-case\"-Format, das auf den Kern-Keywords des Textinhalts basiert. Bei ID-Kollision: nummeriere (z.B. id=\"vorteile-1\", id=\"vorteile-2\").");

            builder.AddOrUpdate("Smartstore.AI.Prompts.CleanupMarkup",
                "Remove inline styles, empty elements, unnecessary span/div/font wrappers, and redundant nesting.",
                "Entferne Inline-Styles, leere Elemente, überflüssige span/div/font-Umhüllungen und unnötige Verschachtelungen.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.PreserveSemantic",
                "Keep all semantic HTML intact (including tables, blockquotes, images, code blocks, etc.). Preserve CSS classes, links, and attributes.",
                "Behalte sämtliche semantischen HTML-Elemente bei (inkl. Tabellen, Blockquotes, Bilder, Code-Blöcke etc.). Erhalte CSS-Klassen, Links und Attribute.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.OnlyImproveStructure",
                "Only improve structure—don't remove content or meaningful markup.",
                "Verbessere ausschließlich die Struktur – keine Inhalte oder bedeutsames Markup entfernen.");

            builder.AddOrUpdate("Admin.AI.TopicGenerate", "Generate {0}", "{0} generieren");
            builder.AddOrUpdate("Admin.AI.TopicOptimize", "Optimize {0}", "{0} optimieren");
            builder.AddOrUpdate("Admin.AI.Topic.Text", "Text", "Text");
            builder.AddOrUpdate("Admin.AI.Topic.Image", "Image", "Bild");
            builder.AddOrUpdate("Admin.AI.Topic.ShortDesc", "Short Description", "Kurzbeschreibung");
            builder.AddOrUpdate("Admin.AI.Topic.MetaTitle", "Title-Tag", "Title-Tag");
            builder.AddOrUpdate("Admin.AI.Topic.MetaDesc", "Meta Description", "Meta Description");
            builder.AddOrUpdate("Admin.AI.Topic.MetaKeywords", "Meta Keywords", "Meta Keywords");
            builder.AddOrUpdate("Admin.AI.Topic.FullDesc", "Full Description", "Langtext");
            builder.Delete(
                "Admin.AI.EditHtml", 
                "Admin.AI.CreateImage", 
                "Admin.AI.CreateText",
                "Admin.AI.CreateShortDesc",
                "Admin.AI.CreateFullDesc",
                "Admin.AI.CreateMetaTitle",
                "Admin.AI.CreateMetaDesc",
                "Admin.AI.CreateMetaKeywords");

            builder.AddOrUpdate("Order.Product(s).OrderedQuantity", "Ordered", "Bestellt");

            builder.AddOrUpdate("ReturnCase.EntireOrder", "Entire order", "Gesamte Bestellung");
            builder.AddOrUpdate("ReturnCase.CertainItems", "Certain items", "Bestimmte Artikel");

            builder.AddOrUpdate("ReturnCase.WithdrawEntireOrder",
                "I would like to withdraw the entire order:",
                "Ich möchte die gesamte Bestellung stornieren:");
            builder.AddOrUpdate("ReturnCase.WithdrawItems",
                "I would like to withdraw the following items:", 
                "Ich möchte folgende Artikel stornieren:");

            builder.AddOrUpdate("Account.CustomerOrders.ReturnItems", "Return items", "Artikel zurücksenden");

            builder.AddOrUpdate("ReturnRequests.NoItemsSubmitted",
                "Please select the number of items you wish to return.",
                "Bitte wählen Sie die Menge der Artikel aus, die Sie zurücksenden möchten.");

            builder.AddOrUpdate("Account.PasswordRecovery.EmailHasBeenSent",
                "If there is an account associated with this email, we have sent a link to reset your password.",
                "Falls ein Konto mit dieser E-Mail-Adresse verknüpft ist, haben wir Ihnen soeben Anweisungen zum Zurücksetzen Ihres Passworts zugeschickt.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestsEnabled")
                .Value("en", "Return requests enabled");

            builder.Delete("Admin.Configuration.Settings.Order.ReturnRequestsEnabled.Hint",
                "Admin.Configuration.Settings.Order.OrderSettings");

            builder.AddOrUpdate("Admin.Configuration.Settings.Order.NumberOfDaysReturnRequestAvailable",
                "Allowed period for return requests (days)",
                "Erlaubter Zeitraum für Retourenanträge (in Tagen)");

            builder.AddOrUpdate("Admin.Configuration.Settings.Order.NumberOfDaysReturnRequestAvailable.Hint",
                "The number of days after order completion during which customers can submit return requests."
                + " This applies to RMA return requests only and not to legal withdrawal. The value 0 means \"unlimited\".",
                "Die Anzahl der Tage nach Abschluss der Bestellung, während der Kunden Retourenanträge einreichen können."
                + " Dies gilt nur für Retourenanträge im Rahmen des RMA-Verfahrens und nicht für den gesetzlichen Widerruf. Der Wert 0 bedeutet \"unbegrenzt\".");

            builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestReasons.Hint")
                .Value("de", "Eine kommaseparierte Liste von Retourengründen, die der Benutzer auswählen kann, wenn er einen Rücksendeantrag übermittelt.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestActions")
                .Value("en", "Requested action for return");
           
            builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestActions.Hint",
                "A comma-separated list of the actions that a customer will be able to select when submitting a return request. This is not used for legal withdrawal.",
                "Eine kommaseparierte Liste von Aktionen, die ein Benutzer auswählen kann, wenn er einen Rücksendeantrag übermittelt. Beispiel: \"Ersatz\", \"Gutschein\" usw."
                + " Dies wird nicht für den gesetzlichen Widerspruch verwendet.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestSettings", "Returns", "Retouren");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnTargets.Option.Withdrawal", "Withdrawal", "Widerruf");
        }
    }
}
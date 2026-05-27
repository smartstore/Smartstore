using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Configuration;
using Smartstore.Data.Migrations;
using Smartstore.Utilities;

namespace Smartstore.Core.Data.Migrations;

public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
{
    public DataSeederStage Stage => DataSeederStage.Early;
    public bool AbortOnFailure => false;

    public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
    {
        await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        await MigrateSettingsAsync(context, cancelToken);
    }

    public async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
    {
        // ContentSlider: Corrected setting name & adaptions for template 6.
        var contentSliderTemplate = new[]
        {
            // Template1
            @"<div class=""container h-100""><div class=""row h-100""><div class=""col-md-6 py-3 text-md-right text-center""><h2 data-aos=""slide-right"" style=""--aos-delay: 600ms"">Slide-Title</h2><p class=""d-none d-md-block"" data-aos=""slide-right"" style=""--aos-delay: 800ms"">Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.</p></div><div class=""col-md-6 picture-container""><img alt="""" src=""https://picsum.photos/600/600"" class=""img-fluid"" /></div></div></div>",
            // Template2
            @"<div class=""container h-100""><div class=""row h-100""><div class=""col-6 col-md-3 picture-container""><img src=""https://picsum.photos/400/600"" class=""img-fluid"" /></div><div class=""col-6 col-md-9 py-3""><h2 data-aos=""slide-left"" style=""--aos-delay: 600ms"">Slide-Title</h2><p data-aos=""slide-left"" style=""--aos-delay: 800ms"">Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum.</p></div></div></div>",
            // Template3
            @"<div class=""container h-100""><div class=""row h-100""><div class=""col-md-12 col-lg-6 picture-container""><img alt="""" src=""https://picsum.photos/600/600"" class=""img-fluid"" /></div><div class=""col-lg-6 d-none d-lg-block""><h2 data-aos=""slide-left"" style=""--aos-delay: 600ms"">Slide-Title</h2><p data-aos=""slide-left"" style=""--aos-delay: 800ms"">Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.</p></div></div></div>",
            // Template4
            @"<div class=""container h-100""><div class=""row h-100""><div class=""col-md-6 d-flex align-items-center justify-content-end""><figure class=""picture-container vertical-align-middle"" data-aos=""zoom-in"" data-aos-easing=""ease-out-cubic""><img src=""https://picsum.photos/300/300"" class=""img-fluid"" /></figure></div><div class=""col-md-6 d-flex align-items-center""><div><h2 data-aos=""slide-left"" style=""--aos-delay: 600ms"">Slide-Title</h2><p data-aos=""slide-left"" style=""--aos-delay: 900ms"">Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est .</p></div></div></div></div>",
            // Template5
            @"<div class=""container h-100""><div class=""row h-100""><div class=""col col-md-3 col-sm-6"" data-aos=""slide-down"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 1000ms""><img src=""https://picsum.photos/300/500/"" class=""img-fluid"" /></div><div class=""col-md-3 col-12 col-sm-6 d-none d-sm-block"" data-aos=""slide-down"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 1500ms""><img src=""https://picsum.photos/300/501/"" class=""img-fluid"" /></div><div class=""col-md-3 d-none d-md-block"" data-aos=""slide-down"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 2000ms""><img src=""https://picsum.photos/300/502/"" class=""img-fluid"" /></div><div class=""col-md-3 d-none d-md-block"" data-aos=""slide-down"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 2500ms""><img src=""https://picsum.photos/300/503/"" class=""img-fluid"" /></div></div></div>",
            // Template6
            @"<div class=""container p-0""><div class=""row h-100 g-0""><div class=""col-md-3 d-none d-md-block"" data-aos=""fade-up"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 1500ms""><img src=""https://picsum.photos/330/501/"" class=""img-fluid"" /></div><div class=""col-md-3 col-12 col-sm-6"" data-aos=""fade-right"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 500ms""><img class=""img-fluid"" src=""https://picsum.photos/330/500/"" /></div><div class=""col-md-3 col-sm-6"" data-aos=""fade-left"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 500ms""><img class=""img-fluid"" src=""https://picsum.photos/330/500/"" /></div><div class=""col-md-3 d-none d-md-block"" data-aos=""fade-up"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 1500ms""><img src=""https://picsum.photos/330/501/"" class=""img-fluid"" /></div></div></div>"
        };

        for (var i = 0; i < 6; i++)
        {
            var templateName = $"ContentSliderSettings.Template{i + 1}";
            var template = await context.Settings
                .Where(x => x.Name == templateName)
                .FirstOrDefaultAsync(cancelToken);

            if (template != null)
            {
                context.Remove(template);
                context.Settings.Add(new Setting
                {
                    Name = templateName,
                    Value = contentSliderTemplate[i],
                    StoreId = 0
                });
            }
        }

        await context.MigrateSettingsAsync(builder =>
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

        builder.Delete("Admin.ReturnRequests.Updated",
            "Admin.ReturnRequests.Deleted");

        builder.AddOrUpdate("ReturnRequests.NoItemsSubmitted",
            "Please select the items you wish to return and specify the quantity.",
            "Wählen Sie bitte die Artikel und die Menge aus, die Sie zurücksenden möchten.");

        builder.AddOrUpdate("ReturnRequests.Submit", "Submit return request", "Retourenantrag absenden");
        builder.AddOrUpdate("ReturnRequests.Submitted", "A return request has been submitted.", "Der Retourenantrag wurde übermittelt.");

        builder.AddOrUpdate("Admin.ReturnRequests.Fields.ID",
            "ID",
            "ID",
            "ID of the withdrawal or return",
            "ID des Widerrufs bzw. der Retoure");

        builder.AddOrUpdate("Admin.Common.SuccessfullySaved",
            "The changes have been saved successfully.",
            "Die Änderungen wurden erfolgreich gespeichert.");
        builder.AddOrUpdate("Admin.Common.SuccessfullyDeleted",
            "The entries were successfully deleted.",
            "Die Einträge wurden erfolgreich gelöscht.");

        builder.AddOrUpdate("Account.PasswordRecovery.EmailHasBeenSent",
            "If there is an account associated with this email, we have sent a link to reset your password.",
            "Falls ein Konto mit dieser E-Mail-Adresse verknüpft ist, haben wir Ihnen soeben Anweisungen zum Zurücksetzen Ihres Passworts zugeschickt.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestsEnabled")
            .Value("en", "Return requests enabled");

        builder.Delete("Admin.Configuration.Settings.Order.ReturnRequestsEnabled.Hint",
            "Admin.Configuration.Settings.Order.OrderSettings",
            "Admin.Configuration.Settings.Order.ReturnRequestsDescription2");

        builder.AddOrUpdate("Admin.Configuration.Settings.Order.NumberOfDaysReturnRequestAvailable",
            "Allowed period for return requests (in days)",
            "Erlaubter Zeitraum für Retourenanträge (in Tagen)");

        builder.AddOrUpdate("Admin.Configuration.Settings.Order.NumberOfDaysReturnRequestAvailable.Hint",
            "The number of days after order completion during which customers can submit return requests."
            + " This applies to RMA return requests only and not to legal withdrawal. The value 0 means \"unlimited\".",
            "Die Anzahl der Tage nach Abschluss der Bestellung, während der Kunden Retourenanträge einreichen können."
            + " Dies gilt nur für Retourenanträge im Rahmen des RMA-Verfahrens und nicht für den gesetzlichen Widerruf. Der Wert 0 bedeutet \"unbegrenzt\".");

        builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestReasons.Hint")
            .Value("de", "Eine kommaseparierte Liste von Retourengründen, die der Benutzer auswählen kann, wenn er einen Retourenantrag übermittelt.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestActions")
            .Value("en", "Available return actions");

        builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestActions.Hint",
            "A comma-separated list of the actions that a customer will be able to select when submitting a return request. This is not used for legal withdrawal.",
            "Eine kommaseparierte Liste von Aktionen, aus denen der Benutzer wählen kann, wenn er einen Retourenantrag übermittelt. Beispiel: \"Ersatz\", \"Gutschein\" usw."
            + " Dies wird nicht für den gesetzlichen Widerruf verwendet.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestSettings", "Returns", "Retouren");

        builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnTargets.Option.Withdrawal", "Withdrawal", "Widerruf");

        var prefix = "Admin.Configuration.Settings.Resiliency";

        builder.AddOrUpdate($"{prefix}.QueuedMailSending", "Mail Sending", "E-Mail-Versand");
        builder.AddOrUpdate($"{prefix}.QueuedMailSendingNotes",
            "Limits how many queued emails can be sent within a specific time window to prevent overload during email bursts.",
            "Begrenzt die Anzahl der E-Mails, die innerhalb eines bestimmten Zeitfensters versendet werden können, um Überlastung bei E-Mail-Spitzen zu verhindern.");

        builder.AddOrUpdate($"{prefix}.MailSendRateWindow",
            "Time window (hh:mm:ss)",
            "Zeitfenster (hh:mm:ss)",
            "The time period for measuring the queued mail send rate (e.g., 1 minute).",
            "Der Zeitraum für die Messung der E-Mail-Versandrate (z.B. 1 Minute).");

        builder.AddOrUpdate($"{prefix}.MailSendRateLimit",
            "Limit",
            "Grenzwert",
            "The maximum number of queued emails that may be sent during the time window. Empty value means there is no limit.",
            "Die maximale Anzahl von E-Mails, die während des Zeitfensters versendet werden dürfen. Ein leerer Wert bedeutet: keine Begrenzung.");

        builder.AddOrUpdate("Admin.Rules.FilterDescriptor.AllProductsWithDeliveryTimeInCart",
            "All products with delivery time in cart",
            "Alle Produkte mit Lieferzeit im Warenkorb");

        builder.AddOrUpdate("Common.Unlimited", "Unlimited", "Unbegrenzt");

        builder.AddOrUpdate("RewardPoints.PointsForPurchasesInfo",
            "For every {0} net order value, {1} points are awarded.",
            "Für einen Auftragswert von je {0} netto werden {1} Punkte gewährt.");

        builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.GoogleRecaptcha.Info",
            "Manage keys and domains in the <a class='fwm' href='https://www.google.com/recaptcha/admin' target='_blank'>reCAPTCHA Admin Console</a>. v3 runs invisibly with a risk score; optional step-up challenges can be enabled if needed.",
            "Keys und Domains verwalten Sie in der <a class='fwm' href='https://www.google.com/recaptcha/admin' target='_blank'>reCAPTCHA Admin Console</a>. Bei v3 erfolgt die Bewertung unsichtbar per Score; Step-Up-Prüfungen sind optional möglich.");

        builder.AddOrUpdate("Enums.VatNumberStatus.ServiceUnavailable",
            "Online checks are currently unavailable",
            "Onlineprüfung derzeit nicht möglich");

        builder.AddOrUpdate("Admin.Customers.Customers.Fields.VatNumberStatus",
            "VAT number status",
            "Status der Steuernummer");

        builder.AddOrUpdate("Admin.Customers.Customers.Fields.VatNumber.MarkAs",
            "Mark as",
            "Markieren als");

        builder.AddOrUpdate("Admin.Customers.CheckVatNumber",
            "Check online",
            "Online prüfen",
            "Checks the VAT number online and updates its status.",
            "Prüft die Steuernummer online und aktualisiert deren Status.");

        builder.AddOrUpdate("Admin.Customers.VatNumberValidationError",
            "The following error occurred while verifying the VAT number online: {0}",
            "Bei der Onlineprüfung der Steuernummer ist folgender Fehler aufgetreten: {0}");

        builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShippingMetadataInProductDetail",
            "Shipping information for search engines",
            "Versandinformationen für Suchmaschinen",
            "This specifies whether structured shipping information is added for products with free shipping for search engines."
            + " Other shipping costs are not included as these are based on information provided by customers and cannot be clearly assigned to a single product.",
            "Legt fest, ob für Produkte mit kostenlosem Versand strukturierte Versandinformationen für Suchmaschinen hinzugefügt werden."
            + " Andere Versandkosten werden nicht berücksichtigt, da sie auf Kundenangaben basieren und sich nicht eindeutig einem einzelnen Produkt zuordnen lassen.");

        builder.AddOrUpdate("RewardPoints.Message.EarnedForNewsletterSubscription",
            "Earned reward points for signing up for the newsletter in the \"{0}\" store.",
            "Erhaltene Bonuspunkte für die Newsletter-Anmeldung im Shop \"{0}\".");

        builder.AddOrUpdate("Admin.Configuration.Settings.RewardPoints.PointsForNewsletterSubscription",
            "Points for subscribing to the newsletter",
            "Punkte für das Abonnieren des Newsletters",
            "Specifies how many reward points registered customers will receive as a one-time bonus for subscribing to the newsletter.",
            "Legt fest, wie viele Bonuspunkte registrierte Kunden einmalig für das Abonnieren des Newsletters erhalten.");

        builder.AddOrUpdate("Admin.Configuration.Settings.RewardPoints.Description",
            "The reward points program allows customers to earn points for certain actions, such as registering or placing orders.",
            "Das Bonuspunkteprogramm ermöglicht es Kunden, für bestimmte Aktionen Punkte zu sammeln, beispielsweise für Registrierungen oder Bestellungen.");

        builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.NewsletterEnabled.Hint",
            "Specifies whether the option to subscribe to the newsletter is displayed.",
            "Legt fest, ob die Option zum Abonnieren des Newsletters angezeigt wird.");

        builder.AddOrUpdate("Common.Print", "Print", "Drucken");

        builder.AddOrUpdate("Admin.Configuration.Settings.Order.ReturnRequestsDescription1",
            "Customers can use the returns system to request the return of items. Unlike withdrawal, returns are only possible for completed orders.",
            "Über das Retourensystem können Kunden die Rücksendung von Artikeln beantragen. Im Gegensatz zum Widerruf ist eine Retoure nur für abgeschlossene Aufträge möglich.");

        builder.AddOrUpdate("Admin.Configuration.Settings.Price.ShowRequiredProductPricesWithMainProduct",
            "Show additional prices with main product",
            "Zusatzpreise beim Hauptprodukt anzeigen",
            "Specifies whether the prices of automatically added required products are displayed below the price of the main product, for example \"+ €3.00 deposit\"."
            + " This makes additional mandatory costs, such as deposits or required accessories, visible before the customer proceeds to the shopping cart.",
            "Legt fest, ob Preise automatisch hinzugefügter erforderlicher Produkte unterhalb des Preises des Hauptprodukts angezeigt werden, beispielsweise \"+ 3,00 € Pfand\"."
            + " Dadurch werden zusätzliche Pflichtkosten wie Pfand oder erforderliches Zubehör bereits vor dem Warenkorb sichtbar.");

        builder.AddOrUpdate("Products.RequiredProductPriceInfo",
            "<span>+ {0} <span title=\"{1}\">{2}</span></span>",
            "<span>+ {0} <span title=\"{1}\">{2}</span></span>");

        builder.AddOrUpdate("Common.Preset", "Preset", "Voreinstellung");

        builder.AddOrUpdate("Admin.Catalog.Products.Fields.GTIN", "GTIN / EAN", "GTIN / EAN");

        builder.AddOrUpdate("ReturnRequests.SelectProduct(s)")
            .Value("de", "Welche Artikel möchten Sie zurücksenden?");

        builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.ExchangeRateDate", "Exchange rate date", "Umrechnungskursdatum");

        builder.AddOrUpdate("Admin.ThemeVar.Boxed")
            .Value("de", "Legt fest, ob sich die Seite über den kompletten verfügbaren Platz streckt.");

        builder.AddOrUpdate("Admin.System.Maintenance.AttributeFileUploadsDeleted",
            "{0} media files, {1} downloads, and {2} tracks of uploaded attribute files have been deleted.",
            "Es wurden {0} Mediendateien, {1} Downloads und {2} Verweise von hochgeladenen Attributdateien gelöscht.");
    }
}
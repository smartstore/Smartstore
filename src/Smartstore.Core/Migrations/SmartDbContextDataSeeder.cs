using Smartstore.Core.Configuration;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateSettingsAsync(builder =>
            {
                builder.Delete("CustomerSettings.AvatarMaximumSizeBytes", "CatalogSettings.FileUploadMaximumSizeBytes");
            });

            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await MigrateSettingsAsync(context, cancelToken);
        }

        public async Task MigrateSettingsAsync(SmartDbContext db, CancellationToken cancelToken = default)
        {
            // ContentSlider: Replace old service in templates.

            var contentSliderTemplate = new[]
            {
                // Template1
                @"\r\n<div class=""container h-100"">\r\n\t<div class=""row h-100"">\r\n\t\t<div class=""col-md-6 py-3 text-md-right text-center"">\r\n\t\t\t<h2 data-aos=""slide-right"" style=""--aos-delay: 600ms"">Slide-Title</h2>\r\n\t\t\t<p class=""d-none d-md-block"" data-aos=""slide-right"" style=""--aos-delay: 800ms"">Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.</p>\r\n\t\t</div>\r\n\t\t<div class=""col-md-6 picture-container"">\r\n\t\t\t<img alt="""" src=""https://picsum.photos/600/600"" class=""img-fluid"" />\r\n\t\t</div>\r\n\t</div>\r\n</div>\r\n",
                // Template2
                @"\r\n<div class=""container h-100"">\r\n\t<div class=""row h-100"">\r\n\t\t<div class=""col-6 col-md-3 picture-container"">\r\n\t\t\t<img src=""https://picsum.photos/400/600"" class=""img-fluid"" />\r\n\t\t</div>\r\n\t\t<div class=""col-6 col-md-9 py-3"">\r\n\t\t\t<h2 data-aos=""slide-left"" style=""--aos-delay: 600ms"">Slide-Title</h2>\r\n\t\t\t<p data-aos=""slide-left"" style=""--aos-delay: 800ms"">Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum.</p>\r\n\t\t</div>\r\n\t</div>\r\n</div>\r\n",
                // Template3
                @"\r\n<div class=""container h-100"">\r\n\t<div class=""row h-100"">\r\n\t\t<div class=""col-md-12 col-lg-6 picture-container"">\r\n\t\t\t<img alt="""" src=""https://picsum.photos/600/600"" class=""img-fluid"" />\r\n\t\t</div>\r\n\t\t<div class=""col-lg-6 d-none d-lg-block"">\r\n\t\t\t<h2 data-aos=""slide-left"" style=""--aos-delay: 600ms"">Slide-Title</h2>\r\n\t\t\t<p data-aos=""slide-left"" style=""--aos-delay: 800ms"">Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.</p>\r\n\t\t</div>\r\n\t</div>\r\n</div>\r\n",
                // Template4
                @"\r\n<div class=""container h-100"">\r\n\t<div class=""row h-100"">\r\n\t\t<div class=""col-md-6 d-flex align-items-center justify-content-end"">\r\n\t\t\t<figure class=""picture-container vertical-align-middle"" data-aos=""zoom-in"" data-aos-easing=""ease-out-cubic"">\r\n\t\t\t\t<img src=""https://picsum.photos/300/300"" class=""img-fluid"" />\r\n\t\t\t</figure>\r\n\t\t</div>\r\n\t\t<div class=""col-md-6 d-flex align-items-center"">\r\n\t\t\t<div>\r\n\t\t\t\t<h2 data-aos=""slide-left"" style=""--aos-delay: 600ms"">Slide-Title</h2>\r\n\t\t\t\t<p data-aos=""slide-left"" style=""--aos-delay: 900ms"">Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est .</p>\r\n\t\t\t</div>\r\n\t\t</div>\r\n\t</div>\r\n</div>\r\n",
                // Template5
                @"\r\n<div class=""container h-100"">\r\n\t<div class=""row h-100"">\r\n\t\t<div class=""col col-md-3 col-sm-6"" data-aos=""slide-down"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 1000ms"">\r\n\t\t\t<img src=""https://picsum.photos/300/500/"" class=""img-fluid"" />\r\n\t\t</div>\r\n\t\t<div class=""col-md-3 col-12 col-sm-6 d-none d-sm-block"" data-aos=""slide-down"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 1500ms"">\r\n\t\t\t<img src=""https://picsum.photos/300/501/"" class=""img-fluid"" />\r\n\t\t</div>\r\n\t\t<div class=""col-md-3 d-none d-md-block"" data-aos=""slide-down"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 2000ms"">\r\n\t\t\t<img src=""https://picsum.photos/300/502/"" class=""img-fluid"" />\r\n\t\t</div>\r\n\t\t<div class=""col-md-3 d-none d-md-block"" data-aos=""slide-down"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 2500ms"">\r\n\t\t\t<img src=""https://picsum.photos/300/503/"" class=""img-fluid"" />\r\n\t\t</div>\r\n\t</div>\r\n</div>\r\n",
                // Template6
                @"\r\n<div class=""container"">\r\n\t<div class=""row h-100"">\r\n\t\t<div class=""col-md-3 hidden-sm-down"" data-aos=""fade-up"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 500ms"">\r\n\t\t\t<img src=""https://picsum.photos/300/501/"" class=""img-fluid"" />\r\n\t\t</div>\r\n\t\t<div class=""col-md-3 col-12 col-sm-6""  data-aos=""fade-right"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 1500ms"">\r\n\t\t\t<img class=""img-fluid"" src=""https://picsum.photos/300/500/"" />\r\n\t\t</div>\r\n\t\t<div class=""col-md-3 col-sm-6""  data-aos=""fade-left"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 1500ms"">\r\n\t\t\t<img class=""img-fluid"" src=""https://picsum.photos/300/500/"" />\r\n\t\t</div>\r\n\t\t<div class=""col-md-3 d-none d-md-block"" data-aos=""fade-up"" data-aos-easing=""ease-out-cubic"" style=""--aos-delay: 500ms"">\r\n\t\t\t<img src=""https://picsum.photos/300/501/"" class=""img-fluid"" />\r\n\t\t</div>\r\n\t</div>\r\n</div>\r\n"
            };

            for (var i = 0; i < 6; i++)
            {
                var templateName = $"ContentSlider.Template{i + 1}";
                var template = await db.Settings
                    .Where(x => x.Name == templateName)
                    .FirstOrDefaultAsync(cancelToken);

                if (template != null)
                {
                    db.Remove(template);
                    db.Settings.Add(new Setting
                    {
                        Name = templateName,
                        Value = contentSliderTemplate[i],
                        StoreId = 0
                    });
                }
            }

            await db.SaveChangesAsync(cancelToken);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.Delete(
                "Account.ChangePassword.Errors.PasswordIsNotProvided",
                "Common.Wait...",
                "Topic.Button",
                "Admin.Configuration.Settings.RewardPoints.Earning.Hint1",
                "Admin.Configuration.Settings.RewardPoints.Earning.Hint2",
                "Admin.Configuration.Settings.RewardPoints.Earning.Hint3",
                "ShoppingCart.MaximumUploadedFileSize");

            builder.AddOrUpdate("Admin.Report.MediaFilesSize", "Media size", "Mediengröße");
            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.Affiliate", "Affiliate", "Partner");
            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.Authentication", "Authentication", "Authentifizierung");

            builder.AddOrUpdate("Admin.Customers.RemoveAffiliateAssignment",
                "Remove assignment to affiliate",
                "Zuordnung zum Partner entfernen");

            builder.AddOrUpdate("Admin.Configuration.Settings.Order.MaxMessageOrderAgeInDays",
                "Maximum order age for sending messages",
                "Maximale Auftragsalter für den Nachrichtenversand",
                "Specifies the maximum order age in days up to which to create and send messages. Set to 0 to always send messages.",
                "Legt das maximale Auftragsalter in Tagen fest, bis zu dem Nachrichten erstellt und gesendet werden sollen. Setzen Sie diesen Wert auf 0, um Nachrichten immer zu versenden.");

            builder.AddOrUpdate("Admin.MessageTemplate.OrderTooOldForMessageInfo",
                "The message \"{0}\" was not sent. The order {1} is too old ({2}).",
                "Die Nachricht \"{0}\" wurde nicht gesendet. Der Auftrag {1} ist zu alt ({2}).");

            // Typo.
            builder.AddOrUpdate("Admin.Configuration.Settings.ShoppingCart.ShowConfirmOrderLegalHint.Hint")
                .Value("de", "Legt fest, ob rechtliche Hinweise in der Warenkorbübersicht auf der Bestellabschlussseite angezeigt werden. Dieser Text kann in den Sprachresourcen geändert werden.");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.UseNativeNameInLanguageSelector",
                "Display native language name in language selector",
                "In der Sprachauswahl den Sprachnamen in der Landesprache anzeigen",
                "Specifies whether the native language name should be displayed in the language selector. Otherwise, the name maintained in the backend is used.",
                "Legt fest, ob in der Sprachauswahl die Sprachnamen in der nativen Landesprache angezeigt werden soll. Ansonsten wird der im Backend hinterlegte Name verwendet.");

            builder.AddOrUpdate("Common.PageNotFound", "The page does not exist.", "Die Seite existiert nicht.");

            builder.AddOrUpdate("Admin.GiftCards.Fields.Language",
                "Language",
                "Sprache",
                "Specifies the language of the message content.",
                "Legt die Sprache des Nachrichteninhalts fest.");

            builder.AddOrUpdate("RewardPoints.OrderAmount", "Order amount", "Bestellwert");
            builder.AddOrUpdate("RewardPoints.PointsForPurchasesInfo",
                "For every {0} order amount {1} points are awarded.",
                "Für je {0} Auftragswert werden {1} Punkte gewährt.");

            builder.AddOrUpdate("Common.Error.BotsNotPermitted",
                "This process is not permitted for search engine queries (bots).",
                "Dieser Vorgang ist für Suchmaschinenanfragen (Bots) nicht zulässig.");

            // ----- Conditional attributes review (begin)
            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.EditAttributeDetails",
                "Edit attribute. Product: {0}",
                "Attribut bearbeiten. Produkt: {0}");

            builder.AddOrUpdate("Admin.Catalog.Attributes.ProductAttributes.OptionsSetsInfo",
                "<strong>{0}</strong> option sets and <strong>{1}</strong> options",
                "<strong>{0}</strong> Options-Sets und <strong>{1}</strong> Optionen");

            builder.AddOrUpdate("Admin.Rules.ProductAttribute.OneCondition",
                "<span>Only show the attribute if at least</span> {0} <span>of the following rules are true.</span>",
                "<span>Das Attribut nur anzeigen, wenn mindestens</span> {0} <span>der folgenden Regeln zutrifft.</span>");

            builder.AddOrUpdate("Admin.Rules.ProductAttribute.AllConditions",
                "<span>Only show the attribute if</span> {0} <span>of the following rules are true.</span>",
                "<span>Das Attribut nur anzeigen, wenn</span> {0} <span>der folgenden Regeln erfüllt sind.</span>");


            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.EditOptions",
                "Edit <strong>{0}</strong> options",
                "<strong>{0}</strong> Optionen bearbeiten");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.EditRules",
                "Edit <strong>{0}</strong> rules",
                "<strong>{0}</strong> Regeln bearbeiten");

            builder.AddOrUpdate("Admin.Catalog.Products.ProductVariantAttributes.EditOptionsAndRules",
                "Edit <strong>{0}</strong> options and <strong>{1}</strong> rules",
                "<strong>{0}</strong> Optionen und <strong>{1}</strong> Regeln bearbeiten");


            builder.AddOrUpdate("Admin.Rules.AddRuleWarning", "Please add a rule first.", "Bitte zuerst eine Regel hinzufügen.");

            builder.AddOrUpdate("Admin.Rules.AddCondition", "Add rule", "Regel hinzufügen");
            builder.AddOrUpdate("Admin.Rules.AllConditions", 
                "<span>If</span> {0} <span>of the following rules are true.</span>", 
                "<span>Wenn</span> {0} <span>der folgenden Regeln erfüllt sind.</span>");

            builder.AddOrUpdate("Admin.Rules.OneCondition",
                "<span>If at least</span> {0} <span>of the following rules are true.</span>",
                "<span>Wenn mindestens</span> {0} <span>der folgenden Regeln zutrifft.</span>");

            builder.AddOrUpdate("Admin.Rules.SaveConditions", "Save all rules", "Alle Regeln speichern");
            builder.AddOrUpdate("Admin.Rules.SaveToCreateConditions",
                "Rules can only be created after saving.",
                "Regeln können erst nach einem Speichern festgelegt werden.");

            builder.AddOrUpdate("Admin.Rules.TestConditions").Value("de", "Regeln {0} Testen {1}");
            
            builder.AddOrUpdate("Admin.Rules.EditRuleSet", "Edit rule set", "Regelsatz bearbeiten");
            builder.AddOrUpdate("Admin.Rules.OpenRuleSet", "Open rule set", "Regelsatz öffnen");
            builder.Delete(
                "Admin.Rules.EditRule",
                "Admin.Rules.OpenRule",
                "Admin.Catalog.Products.ProductVariantAttributes.Attributes.Values.ViewLink");
            // ----- Conditional attributes review (end)

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.MaxAvatarFileSize",
                "Maximum avatar size",
                "Maximale Avatar-Größe",
                "Specifies the maximum file size of an avatar (in KB). The default is 10,240 (10 MB).",
                "Legt die maximale Dateigröße eines Avatar in KB fest. Der Standardwert ist 10.240 (10 MB).");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.ShowOnPasswordRecoveryPage",
                "Show on password recovery page",
                "Auf der Seite zur Passwort-Wiederherstellung anzeigen");

            builder.AddOrUpdate("Checkout.SpecifyDifferingShippingAddress",
                "I would like to specify a different delivery address after defining my billing address.",
                "Ich möchte nach der Festlegung meiner Rechnungsadresse eine abweichende Lieferanschrift festlegen.");
        }
    }
}
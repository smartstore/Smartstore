using Microsoft.EntityFrameworkCore;
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

            await db.Settings
                .Where(x => x.Name == "PaymentSettings.BypassPaymentMethodSelectionIfOnlyOne")
                .ExecuteUpdateAsync(x => x.SetProperty(s => s.Name, s => "PaymentSettings.SkipPaymentSelectionIfSingleOption"), cancelToken);
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

            // TODO: (mh) Move all AI relevant resources to a dedicated method. Too many res.

            // TODO: (mh) Translation grammar. In other languages the token will be placed at the start. TBD with MC.
            builder.AddOrUpdate("Admin.AI.CreateImageWith", "Create image with ", "Bild erzeugen mit ");
            builder.AddOrUpdate("Admin.AI.CreateTextWith", "Create text with ", "Text erzeugen mit ");
            builder.AddOrUpdate("Admin.AI.TranslateTextWith", "Translate with ", "Übersetzen mit ");
            builder.AddOrUpdate("Admin.AI.MakeSuggestionWith", "Make suggestions with ", "Mach mir Vorschläge mit ");

            // TODO: (mh) Translation grammar. In other languages the token will be placed at the start. TBD with MC.
            builder.AddOrUpdate("Admin.AI.CreateShortDescWith", "Create short description with ", "Kurzbeschreibung erzeugen mit ");
            builder.AddOrUpdate("Admin.AI.CreateMetaTitleWith", "Create title tag with ", "Title-Tag erzeugen mit ");
            builder.AddOrUpdate("Admin.AI.CreateMetaDescWith", "Create meta description with ", "Meta Description erzeugen mit ");
            builder.AddOrUpdate("Admin.AI.CreateMetaKeywordsWith", "Create meta keywords with ", "Meta Keywords erzeugen mit ");
            builder.AddOrUpdate("Admin.AI.CreateFullDescWith", "Create full description with ", "Langtext erzeugen mit ");

            builder.AddOrUpdate("Admin.AI.TextCreation.CreateNew", "Create new", "Neu erstellen");
            builder.AddOrUpdate("Admin.AI.TextCreation.Summarize", "Summarize", "Zusammenfassen");
            builder.AddOrUpdate("Admin.AI.TextCreation.Improve", "Improve", "Schreibstil verbessern");
            builder.AddOrUpdate("Admin.AI.TextCreation.Simplify", "Simplify", "Vereinfachen");
            builder.AddOrUpdate("Admin.AI.TextCreation.Extend", "Extend", "Ausführlicher schreiben");

            builder.AddOrUpdate("Admin.AI.TextCreation.DefaultPrompt", "Create text on the topic: '{0}'", "Erzeuge Text zum Thema: '{0}'.");
            builder.AddOrUpdate("Admin.AI.ImageCreation.DefaultPrompt", "Create a picture on the topic: '{0}'", "Erzeuge ein Bild zum Thema: '{0}'.");
            builder.AddOrUpdate("Admin.AI.Suggestions.DefaultPrompt", "Make suggestions on the topic: '{0}'", "Mache Vorschläge zum Thema '{0}'.");

            builder.AddOrUpdate("Admin.AI.MenuItemTitle.ChangeStyle", "Change style", "Sprachstil ändern");
            builder.AddOrUpdate("Admin.AI.MenuItemTitle.ChangeTone", "Change tone", "Ton ändern");

            builder.AddOrUpdate("Admin.AI.SpecifyTopic", "Please enter a topic", "Bitte geben Sie ein Thema an");

            // TODO: (mh) Why leading spaces? Better: Add all parts/sentences to a list and join by space to generate string result.
            // TODO: (mh) Make class PromptResources. Make virtual method per Res (optionally with params).
            builder.AddOrUpdate("Smartstore.AI.Prompts.DontUseQuotes", 
                " Do not enclose the text in quotation marks.", 
                " Schließe den Text nicht in Anführungszeichen ein.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.DontNumberSuggestions", 
                " Do not number the suggestions.", 
                " Nummeriere die Vorschläge nicht.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.SeparateWithNumberSign", 
                " Separate each suggestion with the # sign.", 
                " Trenne jeden Vorschlag mit dem #-Zeichen.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.CharLimit", 
                " Limit your answer to {0} characters!", 
                " Begrenze deine Antwort auf {0} Zeichen!");
            builder.AddOrUpdate("Smartstore.AI.Prompts.WordLimit",
                " The text may contain a maximum of {0} words.",
                " Der Text darf maximal {0} Wörter enthalten.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.SeparateListWithComma", 
                " The list should be comma-separated so that it can be inserted directly as a meta tag.", 
                " Die Liste soll kommagetrennt sein, so dass sie direkt als META-tag eingefügt werden kann.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.ReserveSpaceForShopName",
                " Do not use the name of the website as this will be added later. Reserve 5 words for this.", 
                " Verwende dabei nicht den Namen der Webseite da dieser später hinzugefügt wird. Reserviere dafür 5 Worte.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.CreatePicture", 
                " Create an image for the topic: '{0}'.", 
                " Erstelle ein Bild zum Thema: '{0}'.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.AddCallToAction",
                " Finally, insert a link with the text '{0}' that refers to '{1}'. The link is given the CSS classes 'btn btn-primary'",
                " Füge abschließend einen Link mit dem Text '{0}' ein, der auf '{1}' verweist. Der Link erhält die CSS-Klassen 'btn btn-primary'");
            builder.AddOrUpdate("Smartstore.AI.Prompts.AddLink",
                " Insert a link that refers to '{0}'.",
                " Füge einen Link ein, der auf '{0}' verweist.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.AddNamedLink",
                " Insert a link with the text '{0}' that refers to '{1}'.",
                " Füge einen Link mit dem Text '{0}' ein, der auf '{1}' verweist.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.AddToc",
                " Insert a table of contents with the title '{0}'." +
                " The title receives a {1} tag." +
                " Link the individual points of the table of contents to the respective headings of the paragraphs.",
                " Füge ein Inhaltsverzeichnis mit dem Titel '{0}' ein." +
                " Der Titel erhält ein {1}-Tag." +
                " Verlinke die einzelnen Punkte des Inhaltsverzeichnisses mit den jeweiligen Überschriften der Absätze.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.IncludeImages",
                " After each paragraph, add another p-tag with the style specification 'width:450px', which contains an i-tag with the classes 'far fa-xl fa-file-image ai-preview-file'." +
                " The title attribute of the i-tag should be the heading of the respective paragraph.",
                " Füge nach jedem Absatz ein weiteres p-Tag zu mit der style-Angabe 'width:450px', das ein i-Tag mit den Klassen 'far fa-xl fa-file-image ai-preview-file' enthält." +
                " Das title-Attribut des i-Tags soll die Überschrift des jeweiligen Absatzes sein.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.NoIntroImage",
                " The intro does not receive a picture.",
                " Das Intro erhält kein Bild.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.NoConclusionImage",
                " The conclusion does not receive a picture.",
                " Das Fazit erhält kein Bild.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.UseKeywords",
                " Use the following keywords: '{0}'.",
                " Verwende folgende Keywords: '{0}'.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.MakeKeywordsBold",
                " Include the keywords in b-tags.",
                " Schließe die Keywords in b-Tags ein.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.KeywordsToAvoid",
                " Do not use the following keywords under any circumstances: '{0}'.",
                " Verwende unter keinen Umständen folgende Keywords: '{0}'.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.IncludeConclusion",
                " End the text with a conclusion.",
                " Schließe den Text mit einem Fazit ab.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.ParagraphHeadingTag",
                " The headings of the individual sections are given {0} tags.",
                " Die Überschriften der einzelnen Abschnitte erhalten {0}-Tags.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.WriteCompleteParagraphs",
                " Write complete texts for each section.",
                " Schreibe vollständige Texte für jeden Abschnitt.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.ParagraphWordCount",
                " Each section should contain a maximum of {0} words.",
                " Jeder Abschnitt soll maximal {0} Wörter enthalten.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.ParagraphCount",
                " The text should be divided into {0} paragraphs, which are enclosed with p tags.",
                " Der Text soll in {0} Abschnitte eingeteilt werden, die mit p-Tags umschlossen werden.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.MainHeadingTag",
                " The main heading is given a {0} tag.",
                " Die Hauptüberschrift erhält ein {0}-Tag.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.IncludeIntro",
                " Start with an introduction.",
                " Beginne mit einer Einleitung.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.LanguageStyle",
                " The language style should be {0}.",
                " Der Sprachstil soll {0} sein.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.LanguageTone",
                " The tone should be {0}.",
                " Der Ton soll {0} sein.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.Language",
                " Write in {0}.",
                " Schreibe in {0}.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.DontCreateTitle",
                " Do not create the title: '{0}'.",
                " Erstelle nicht den Titel: '{0}'.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.StartWithDivTag",
                " Start with a div tag.",
                " Starte mit einem div-Tag.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.JustHtml",
                " Just return the HTML you have created so that it can be integrated directly into a website. " +
                "Don't give explanations about what you have created or introductions like: 'Gladly, here is your HTML'. " +
                "Do not include the generated HTML in any delimiters like: '```html'.",
                " Gib nur das erstellte HTML-zurück, so dass es direkt in einer Webseite eingebunden werden kann. " +
                "Mache keine Erklärungen dazu was du erstellt hast oder Einleitungen wie: 'Gerne, hier ist dein HTML'. " +
                "Schließe das erzeugte HTML auch nicht in irgendwelche Begrenzer ein wie: '```html'.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.CreateHtml",
                " Create HTML text.",
                " Erstelle HTML-Text.");
            // TODO: (mh) dito
            builder.AddOrUpdate("Smartstore.AI.Prompts.RolePrefix",
                " Be a ",
                " Sei ein ");

            // TODO: (mh) dito
            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.Translator",
                "professional translator.",
                "professioneller Übersetzer.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.Copywriter",
                "professional copywriter.",
                "professioneller Texter.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.Marketer",
                "marketing expert.",
                "Marketing-Experte.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.SEOExpert",
                "SEO expert.",
                "SEO-Experte.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.Blogger",
                "professional blogger.",
                "professioneller Blogger.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.Journalist",
                "professional journalist.",
                "professioneller Journalist.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.SalesPerson",
                "assistant that creates product descriptions that convince a potential customer to make a purchase.",
                "Assistent, der Produktbeschreibungen erstellt, die einen potentiellen Kunden von einem Kauf überzeugen.");
            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.ProductExpert",
                "expert for the product: '{0}'.",
                "Experte für das Produkt: '{0}'.");

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

            // ----- Quick checkout (begin)
            builder.AddOrUpdate("Checkout.SpecifyDifferingShippingAddress",
                "I would like to specify a different delivery address after defining my billing address.",
                "Ich möchte nach der Festlegung meiner Rechnungsadresse eine abweichende Lieferanschrift festlegen.");

            builder.AddOrUpdate("Address.Fields.IsDefaultBillingAddress",
                "Set as default billing address",
                "Als Standard-Rechnungsanschrift festlegen");

            builder.AddOrUpdate("Address.Fields.IsDefaultShippingAddress",
                "Set as default shipping address",
                "Als Standard-Lieferanschrift festlegen");

            builder.AddOrUpdate("Address.IsDefaultAddress", "Is default address", "Ist Standardadresse");
            builder.AddOrUpdate("Address.IsDefaultBillingAddress", "Is default billing address", "Ist Standard-Rechnungsanschrift");
            builder.AddOrUpdate("Address.IsDefaultShippingAddress", "Is default shipping address", "Ist Standard-Lieferanschrift");

            builder.AddOrUpdate("Address.SetDefaultAddress",
                "Sets the address as the default billing and shipping address.",
                "Legt die Adresse als Standard-Rechnungs- und Lieferanschrift fest.");

            builder.AddOrUpdate("Account.Fields.PreferredShippingMethod", "Preferred shipping method", "Bevorzugte Versandart");
            builder.AddOrUpdate("Account.Fields.PreferredPaymentMethod", "Preferred payment method", "Bevorzugte Zahlungsart");

            builder.AddOrUpdate("Admin.Configuration.Settings.ShoppingCart.QuickCheckoutEnabled",
                "Quick Checkout",
                "Quick-Checkout",
                "With Quick Checkout, settings from the customer's last order or default purchase settings (e.g. for the billing and shipping address) are applied"
                + " and the associated checkout steps are skipped. This allows the customer to go directly from the shopping cart to the order confirmation page.",
                "Beim Quick-Checkout werden Einstellungen der letzten Bestellung oder Kaufvoreinstellungen des Kunden angewendet (z.B. für die Rechnungs- und Lieferanschrift)"
                + " und die zugehörigen Checkout-Schritte übersprungen. Der Kunde hat so die Möglichkeit direkt vom Warenkorb zur Bestellbestätigungsseite zu gelangen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.ShoppingCart.CustomersCanChangePreferredShipping",
                "Customers can change their preferred shipping method",
                "Kunden können Ihre bevorzugte Versandart ändern");

            builder.AddOrUpdate("Admin.Configuration.Settings.ShoppingCart.CustomersCanChangePreferredPayment",
                "Customers can change their preferred payment method",
                "Kunden können Ihre bevorzugte Zahlungsart ändern");

            builder.AddOrUpdate("Admin.Configuration.Settings.Payment.SkipPaymentSelectionIfSingleOption",
                "Only display payment method selection if more than one payment method is available",
                "Zahlartauswahl nur anzeigen, wenn mehr als eine Zahlart zur Verfügung steht",
                "Specifies whether the payment method selection in checkout is only displayed if more than one payment method is available.",
                "Legt fest, ob die Zahlartauswahl im Checkout nur angezeigt wird, wenn mehr als eine Zahlart zur Verfügung steht.");

            builder.AddOrUpdate("Checkout.Process.Standard",
                "Standard",
                "Standard",
                "The customer goes through all the necessary checkout steps.",
                "Der Kunde durchläuft alle erforderlichen Checkout-Schritte.");

            builder.AddOrUpdate("Checkout.Process.Terminal",
                "Terminal",
                "Terminal",
                "The customer is directly redirected to the confirmation page. Addresses, shipping and payment methods are skipped.",
                "Der Kunde gelangt direkt zur Bestätigungsseite. Anschriften, Versand- und Zahlart werden übersprungen.");

            builder.AddOrUpdate("Checkout.Process.Terminal.PaymentMethod",
                "Terminal with payment",
                "Terminal mit Zahlung",
                "The customer is redirected to the confirmation page via the payment method selection. Addresses and shipping methods are skipped.",
                "Der Kunde gelangt über die Zahlartauswahl zur Bestätigungsseite. Anschriften und Versandart werden übersprungen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.ShoppingCart.CheckoutProcess",
                "Checkout process",
                "Checkout-Prozess",
                "Specifies the type of checkout with the steps to be processed.",
                "Legt die Art des Checkout mit den zu durchlaufenden Schritten fest.");
            // ----- Quick checkout (end)

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.MaxAvatarFileSize",
                "Maximum avatar size",
                "Maximale Avatar-Größe",
                "Specifies the maximum file size of an avatar (in KB). The default is 10,240 (10 MB).",
                "Legt die maximale Dateigröße eines Avatar in KB fest. Der Standardwert ist 10.240 (10 MB).");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.ShowOnPasswordRecoveryPage",
                "Show on password recovery page",
                "Auf der Seite zur Passwort-Wiederherstellung anzeigen");

            builder.Delete("Admin.ContentManagement.Topics.Validation.NoWhiteSpace");
            
            builder.AddOrUpdate("Admin.Common.HtmlId.NoWhiteSpace",
                "Spaces are invalid for the HTML attribute 'id'.",
                "Leerzeichen sind für das HTML-Attribut 'id' ungültig.");

            builder.AddOrUpdate("CookieManager.Dialog.AdUserDataConsent.Heading",
                "Marketing",
                "Marketing");

            builder.AddOrUpdate("CookieManager.Dialog.AdUserDataConsent.Intro",
                "With your consent, our advertising partners can set cookies to create an interest profile for you so that we can offer you targeted advertising. For this purpose, we pass on an identifier unique to your customer account to these services.",
                "Unsere Werbepartner können mit Ihrer Einwilligung Cookies setzen, um ein Interessenprofil für Sie zu erstellen, damit wir Ihnen gezielt Werbung anbieten können. Dazu geben wir eine für Ihr Kundenkonto eindeutige Kennung an diese Dienste weiter. ");

            builder.AddOrUpdate("CookieManager.Dialog.AdPersonalizationConsent.Heading",
                "Personalization",
                "Personalisierung");

            builder.AddOrUpdate("CookieManager.Dialog.AdPersonalizationConsent.Intro",
                "Consent to personalisation enables us to offer enhanced functionality and personalisation. They can be set by us or by third-party providers whose services we use on our pages.",
                "Die Zustimmung zur Personalisierung ermöglicht es uns, erweiterte Funktionalität und Personalisierung anzubieten. Sie können von uns oder von Drittanbietern gesetzt werden, deren Dienste wir auf unseren Seiten nutzen.");

            builder.AddOrUpdate("Admin.Configuration.Settings.ShoppingCart.ShowEssentialAttributesInMiniShoppingCart",
                "Show essential features in mini-shopping cart",
                "Wesentliche Merkmale im Mini-Warenkorb anzeigen");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.LinkManufacturerLogoInLists",
                "Link brand logo",
                "Marken-Logo verlinken");

            builder.AddOrUpdate("Common.OptimizeTableInfo",
                "The table '{0}' is already in an optimal state.",
                "Die Tabelle '{0}' ist bereits in einem optimalen Zustand.");

            builder.AddOrUpdate("Common.OptimizeTableSuccess",
                "The table '{0}' has been successfully optimized: {1} &rarr; {2}. Difference: {3} ({4}).",
                "Die Tabelle '{0}' wurde erfolgreich optimiert: {1} &rarr; {2}. Unterschied: {3} ({4}).");

            builder.AddOrUpdate("Admin.Configuration.Settings.ShoppingCart.AllowActivatableCartItems",
                "Products in shopping cart can be deactivated",
                "Produkte im Warenkorb können deaktiviert werden",
                "Specifies whether products in the shopping cart can be deactivated. Deactivated products will not be ordered and will remain in the shopping cart after the order has been placed.",
                "Legt fest, ob Produkte im Warenkorb deaktiviert werden können. Deaktivierte Produkte werden nicht mitbestellt und verbleiben nach Auftragseingang im Warenkorb.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.ShowProductTags", "Show tags", "Tags anzeigen");

            builder.AddOrUpdate("Common.SearchProducts", "Search products", "Produkte durchsuchen");
            builder.AddOrUpdate("Common.NoProductsFound", "No products were found.", "Es wurden keine Produkte gefunden.");

            builder.AddOrUpdate("Admin.Catalog.Products.GroupedProductConfiguration.SearchMinAssociatedCount",
                "Minimum product count for search",
                "Minimale Produktanzahl für Suche",
                "Specifies the minimum number of associated products from which the search field is displayed.",
                "Legt die Mindestanzahl verknüpfter Produkte fest, ab denen das Suchfeld angezeigt wird.");
            
            builder.AddOrUpdate("Admin.Catalog.Products.GroupedProductConfiguration.Collapsible",
                 "Collapsible associated products",
                 "Aufklappbare verknüpfte Produkte",
                 "Specifies whether details of the associated product are expanded/collapsed by clicking on a header (Accordion).",
                 "Legt fest, ob Details zum verknüpften Produkt durch Klick auf eine Titelzeile auf- oder zugeklappt werden (Akkordeon).");
        }
    }
}
using FluentMigrator;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Configuration;
using Smartstore.Core.Security;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2025-12-09 12:00:00", "V630")]
    internal class V630 : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
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
            builder.AddOrUpdate("Aria.Label.AlphabeticallySortedLinks", "Alphabetically sorted links", "Alphabetisch sortierte Links");
            builder.AddOrUpdate("Aria.Label.BundleContains", "The product set contains {0}", "Das Produktset enthält {0}");
            builder.AddOrUpdate("Aria.Label.ActiveFilters", "Active filters", "Aktive Filter");
            builder.AddOrUpdate("Aria.Label.ProductVariants", "Product variants", "Produktvarianten");
            // INFO: We use punctuation (commas and periods), so that SR pauses for a moment when reading aloud. Hyphens and colons are not reliable.
            builder.AddOrUpdate("Aria.Label.Choice", "{0}, {1}", "{0}, {1}");

            builder.AddOrUpdate("Aria.Label.CartItemSummary",
                "{0} at {1}.",
                "{0} zu {1}.");
            builder.AddOrUpdate("Aria.Label.CartItemSummaryWithTotal",
                "{0} at {1} each, quantity {2}, total {3}.",
                "{0} zu je {1}, Menge {2}, Gesamt {3}.");
            builder.AddOrUpdate("Aria.Label.CartItemSummaryWithAttributes",
                "{0} at {1}, quantity {2}. {3}",
                "{0} zu {1}, Menge {2}. {3}");

            builder.AddOrUpdate("Aria.Label.CartTotalSummary",
                "Your order: {0} {1}, {2} products.",
                "Ihre Bestellung: {0} {1}, {2} Artikel.");
            builder.AddOrUpdate("Aria.Label.BuyHint",
                "By clicking on \"Buy,\" I accept the terms and conditions.",
                "Mit Klick auf \"Kaufen\" akzeptiere ich die Bedingungen.");

            builder.AddOrUpdate("Reviews.Overview.Review",
                "Rating: {0} out of 5 stars. {1} review.",
                "Bewertung: {0} von 5 Sternen. {1} Bewertung.");
            builder.AddOrUpdate("Reviews.Overview.Reviews",
                "Rating: {0} out of 5 stars. {1} reviews.",
                "Bewertung: {0} von 5 Sternen. {1} Bewertungen.");

            builder.AddOrUpdate("Aria.Label.PaginatorItemsPerPage", "Results per page", "Ergebnisse pro Seite");

            builder.AddOrUpdate("Homepage.TopCategories", "Top categories", "Top-Warengruppen");
            builder.AddOrUpdate("Common.SkipList", "Skip list", "Liste überspringen");
            builder.AddOrUpdate("Common.PleaseWait", "Please wait…", "Bitte warten…");
            builder.AddOrUpdate("Common.Helpful", "Helpful", "Hilfreich");
            builder.AddOrUpdate("Common.NotHelpful", "Not helpful", "Nicht hilfreich");

            builder.AddOrUpdate("Products.ProductsHaveBeenAddedToTheCart",
                "{0} of {1} products have been added to the shopping cart.",
                "{0} von {1} Produkten wurden in den Warenkorb gelegt.");

            builder.Delete(
                "Media.Category.ImageAlternateTextFormat",
                "Media.Manufacturer.ImageAlternateTextFormat",
                "Media.Product.ImageAlternateTextFormat",
                "Common.DecreaseValue",
                "Common.IncreaseValue",
                "Admin.Orders.Address.EditAddress");

            builder.AddOrUpdate("ShoppingCart.DiscountCouponCode.Removed", "The discount code has been removed", "Der Rabattcode wurde entfernt");
            builder.AddOrUpdate("ShoppingCart.GiftCardCouponCode.Removed", "The gift card code has been removed", "Der Geschenkgutschein wurde entfernt");
            builder.AddOrUpdate("ShoppingCart.RewardPoints.Applied", "The reward points were applied.", "Die Bonuspunkte wurden angewendet.");
            builder.AddOrUpdate("ShoppingCart.RewardPoints.Removed", "The reward points have been removed.", "Die Bonuspunkte wurden entfernt.");

            // Resource value was a bit off.
            builder.AddOrUpdate("ShoppingCart.DiscountCouponCode.Tooltip", "Your discount code", "Ihr Rabattcode");

            // Replace problematic "&amp;gt;".
            builder.AddOrUpdate("Search.Facet.RemoveFilter", "Remove filter: {0} \"{1}\"", "Filter aufheben: {0} \"{1}\"");

            builder.AddOrUpdate("Products.SavingBadgeLabel", "&minus; {0} %", "&minus; {0} %");

            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.CartWeightRule", "Weight of all products in cart", "Gewicht aller Produkte im Warenkorb");

            builder.AddOrUpdate("Admin.Configuration.Settings.Price.ApplyDiscountsOfLinkedProducts",
                "Apply discounts of linked products",
                "Rabatte von verknüpften Produkten anwenden",
                "Specifies whether discounts (e.g. tier prices) of linked products are taken into account when calculating attribute price surcharges.",
                "Legt fest, ob bei der Berechnung von Attributpreisaufschlägen die Rabatte (z.B. Staffelpreise) von verknüpften Produkten berücksichtigt werden.");

            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.AllProductsFromCategoryInCart",
                "All products from category in cart",
                "Alle Produkte aus Kategorie im Warenkorb");
            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.AllProductsFromManufacturerInCart",
                "All products from manufacturer in cart",
                "Alle Produkte von Hersteller im Warenkorb");
            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.ProductInCategoryTreeCartRule",
                "Product from category or subcategories in cart",
                "Produkt aus Kategorie oder Unterkategorien im Warenkorb");
            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.SubscribedToNewsletter",
                "Subscribed to newsletter",
                "Newsletter abonniert");

            builder.AddOrUpdate("LinkBuilder.LinkTarget",
                "Define the target attribute for the link.",
                "Definieren Sie das Attribut target für den Link.");

            builder.AddOrUpdate("PrivateMessages.Inbox", "Private messages", "Private Nachrichten");

            builder.AddOrUpdate("Admin.Catalog.Attributes.SpecificationAttributes.EditAttributeDetails",
                "Edit specification attribute",
                "Spezifikationsattribut bearbeiten");

            builder.AddOrUpdate("Smartstore.AI.Prompts.CreateImagesOnlyOnExplicitRequest",
                "Only add placeholders for images if this is explicitly requested.",
                "Füge nur dann Platzhalter für Bilder hinzu, wenn dies ausdrücklich gewünscht wird.");

            builder.AddOrUpdate("Admin.Permissions.AllPermissionsGranted", "All {0} permissions granted.", "Alle {0} Rechte gewährt.");
            builder.AddOrUpdate("Admin.Permissions.NumPermissionsGranted", "{0} of {1} permissions granted.", "{0} von {1} Rechten gewährt.");
            builder.AddOrUpdate("Admin.Permissions.NoPermissionGranted", "No permissions granted.", "Keine Rechte gewährt.");

            builder.AddOrUpdate("Admin.System.Warnings.EuVatWebService.Unstable",
                "Due to the server's IPv6 configuration, the EU web service for validating VAT numbers may not work properly.",
                "Aufgrund der IPv6-Konfiguration des Servers funktioniert der Web-Service der EU zur Überprüfung von Steuernummern möglicherweise nicht korrekt.");

            builder.AddOrUpdate("Admin.System.SystemInfo.IPAddress", "IP address", "IP-Adresse");
            builder.AddOrUpdate("Admin.System.SystemInfo.IPAddress.Hint", "The IP address of the machine.", "Die IP-Adresse der Maschine.");

            // This a workaround/fallback for missing string resources in plugins with duplicate permission names:
            builder.AddOrUpdate("Plugins.Permissions.DisplayName.Display", "Display", "Anzeigen");

            // Return requests
            builder.AddOrUpdate("ReturnRequests.NoItemsSubmitted",
                "Your return request has not been submitted. Please select the required quantity to return.",
                "Ihr Rücksendewunsch wurde nicht übermittelt. Bitte wählen Sie die erforderliche Rücksendemenge aus.");

            builder.AddOrUpdate("ReturnRequests.ReturnsNotPossible",
                "Returns are not possible for this order.",
                "Für diesen Auftrag ist eine Rücksendung von Artikeln nicht möglich.");

            builder.AddOrUpdate("ReturnRequests.Products.Quantity")
                .Value("en", "Quantity to return");

            builder.AddOrUpdate("ReturnRequests.WhyReturning")
                .Value("de", "Warum möchten Sie diese Artikel zurücksenden?");
            builder.AddOrUpdate("ReturnRequests.SelectProduct(s)")
                .Value("de", "Welche Produkte möchten Sie zurücksenden?");

            builder.AddOrUpdate("ReturnRequests.Title",
                "Returnable items from order no. {0}",
                "Retournierbare Artikel aus Auftrag Nr. {0}");

            builder.AddOrUpdate("ReturnRequests.Products.RequestAlreadyExists",
                "There are already return requests for this item.",
                "Zu diesem Artikel gibt es bereits Rücksendewünsche.");

            builder.AddOrUpdate("Common.EnlargeView", "Enlarge view", "Ansicht vergrößern");

            builder.AddOrUpdate("Admin.Catalog.Categories.Products.AddNew", "Assign products", "Produkte zuordnen");
            builder.AddOrUpdate("Admin.Catalog.Categories.ProductsHaveBeenAssignedToCategory",
                "{0} of {1} products have been assigned to the category.",
                "{0} von {1} Produkten wurden der Warengruppe zugeordnet.");

            builder.AddOrUpdate("Enums.PostIntroVisibility.Hidden", "Don't show", "Nicht anzeigen");
            builder.AddOrUpdate("Enums.PostIntroVisibility.TwoLines", "Two lines maximum", "Maximal zweizeilig");
            builder.AddOrUpdate("Enums.PostIntroVisibility.ThreeLines", "Three lines maximum", "Maximal dreizeilig");
            builder.AddOrUpdate("Enums.PostIntroVisibility.FullText", "Show all", "Komplett anzeigen");

            builder.AddOrUpdate("Enums.PostListColumns.Two", "Two columns", "2 Spalten");
            builder.AddOrUpdate("Enums.PostListColumns.Three", "Three columns", "3 Spalten");

            builder.Delete(
                "Admin.ContentManagement.Blog.BlogPosts.Fields",
                "Admin.Configuration.Settings.Blog.PostsPageSize",
                "Admin.Configuration.Settings.Blog.PostsPageSize.Hint",
                "Admin.ContentManagement.Blog.Heading.Display",
                "Admin.ContentManagement.Blog.Heading.Publish",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.Picture",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.Picture.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.PreviewDisplayType",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.PreviewDisplayType.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.PreviewPicture",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.PreviewPicture.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.SectionBg",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.SectionBg.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.DisplayTagsInPreview",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.DisplayTagsInPreview.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.Title",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.Title.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.Intro",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.Intro.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.Body",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.Body.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.Tags",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.Tags.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.AllowComments",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.AllowComments.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.StartDate",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.StartDate.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.EndDate",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.EndDate.Hint",
                "Admin.ContentManagement.Blog.BlogPosts.Fields.Comments");

            builder.AddOrUpdate("Admin.Configuration.Settings.PostsPageSize", "Posts per page", "Beiträge pro Seite");

            // Collection Groups
            builder.AddOrUpdate("Permissions.DisplayName.CollectionGroup", "Display groups", "Anzeigegruppen");
            builder.AddOrUpdate("Admin.Configuration.CollectionGroups", "Display groups", "Anzeigegruppen");
            builder.AddOrUpdate("Admin.Configuration.CollectionGroups.Add", "Add display group", "Anzeigegruppe hinzufügen");

            builder.AddOrUpdate("Admin.Configuration.CollectionGroups.Info",
                "Display groups can be used to organize lists, such as lists of specification attributes, and present them more clearly.",
                "Mit Hilfe von Anzeigegruppen können Listen, wie etwa eine Liste von Spezifikationsattributen, gruppiert und somit übersichtlicher dargestellt werden.");

            builder.AddOrUpdate("Admin.Catalog.Attributes.SpecificationAttributes.Fields.CollectionGroup",
                "Display Group",
                "Anzeigegruppe",
                "Specifies the name of a display group (optional). This causes the attributes to be displayed in groups in the frontend.",
                "Legt den Namen einer Anzeigegruppe fest (optional). Dadurch werden die Attribute im Frontend gruppiert angezeigt.");

            builder.AddOrUpdate("Admin.Configuration.CollectionGroup.Name",
                "Name",
                "Name",
                "Specifies the name of the display group.",
                "Legt den Namen der Anzeigegruppe fest.");

            builder.AddOrUpdate("Admin.Configuration.CollectionGroup.EntityName",
                "Object",
                "Objekt",
                "The name of the object assigned to the display group.",
                "Der Name des Objekts, das der Anzeigegruppe zugeordnet ist.");

            builder.AddOrUpdate("Admin.Configuration.CollectionGroup.NumberOfAssignments",
                "Assignments",
                "Zuordnungen",
                "The number of objects assigned to the display group.",
                "Die Anzahl der Objekte, die der Anzeigegruppe zugeordnet sind.");

            builder.AddOrUpdate("Common.DontShowDialogAgain", "Do not show this dialog again", "Diesen Dialog nicht mehr anzeigen");

            builder.AddOrUpdate("Admin.Common.Configured", "Configured", "Konfiguriert");
            builder.AddOrUpdate("Admin.Common.NotConfigured", "Not configured", "Nicht konfiguriert");

            builder.AddOrUpdate("Admin.Promotions.Campaigns.Warning",
                "Save the campaign and use the preview button to test it before sending it to many customers."
                + " You can set additional settings, such as the email account to be used, in the"
                + " <a href=\"{0}\" class=\"alert-link\">System.Campaign</a>",
                "Speichern Sie die Kampagne und benutzen Sie den Vorschau-Button, um sie zu testen, bevor Sie sie an viele Kunden versenden."
                + " Weitere Einstellungen, wie beispielsweise das bei der Kampagne zu verwendende E-Mail-Konto, können bei der Nachrichtenvorlage"
                + " <a href=\"{0}\" class=\"alert-link\">System.Campaign</a> vorgenommen werden.");

            builder.AddOrUpdate("Admin.Promotions.Discounts.Fields.CouponCode")
                .Value("de", "Rabattcode");
            builder.AddOrUpdate("Admin.Promotions.Discounts.Fields.RequiresCouponCode")
                .Value("de", "Rabattcode erforderlich");
            builder.AddOrUpdate("Admin.Promotions.Discounts.Fields.RequiresCouponCode.Hint")
                .Value("de", "Legt fest, dass der Rabatt erst nach Eingabe des Rabattcodes auf der Warenkorbseite angewendet wird.");
            builder.Delete("Admin.Promotions.Discounts.Fields.CouponCode.Hint");

            builder.AddOrUpdate("Products.ProductCodeNotFound",
                "The product with the code <b>{0}</b> could not be found.",
                "Es wurde kein Produkt mit dem Produktcode <b>{0}</b> gefunden.");


            // MOSAIC
            // M (Motiv) =>  we already got these resources.
            // O (Optics)
            builder.AddOrUpdate("Admin.AI.ImageCreation.Optic",
                "The visual style is {0} with a {1} color palette.",
                "Die Optik ist {0} und die Farbgestaltung ist {1}.");
            builder.AddOrUpdate("Admin.AI.ImageCreation.Optic.Fallback",
                "The visual style is focused on {0} and uses a color palette described as {1}.",
                "Die Optik ist auf {0} fokussiert und verwendet eine Farbgestaltung, die als {1} beschrieben wird.");

            // Optics: Medium only
            builder.AddOrUpdate("Admin.AI.ImageCreation.Optic.MediumOnly",
                "The visual style is {0}.",
                "Die Optik ist {0}.");
            builder.AddOrUpdate("Admin.AI.ImageCreation.Optic.MediumOnly.Fallback",
                "The visual style is focused on {0}.",
                "Die Optik ist auf {0} fokussiert.");

            // Optics: Color only
            builder.AddOrUpdate("Admin.AI.ImageCreation.Optic.ColorOnly",
                "The visual style uses a {0} color palette.",
                "Die Optik verwendet eine Farbgestaltung, die {0} ist.");
            builder.AddOrUpdate("Admin.AI.ImageCreation.Optic.ColorOnly.Fallback",
                "The visual style uses a color palette focused on {0}.",
                "Die Optik verwendet eine Farbgestaltung mit Fokus auf {0}.");

            // S (Scene)
            builder.AddOrUpdate("Admin.AI.ImageCreation.Scene",
                "The scene takes place {0}.",
                "Die Szene spielt {0}.");
            builder.AddOrUpdate("Admin.AI.ImageCreation.Scene.Fallback",
                "The scene takes place in an environment with a focus on {0}.",
                "Die Szene spielt in einer Umgebung mit Fokus auf {0}.");

            // A (Atmosphere)
            builder.AddOrUpdate("Admin.AI.ImageCreation.Atmosphere",
                "The atmosphere feels {1} and is shaped by {0}.",
                "Die Atmosphäre wirkt {1} und wird durch {0} geprägt.");
            builder.AddOrUpdate("Admin.AI.ImageCreation.Atmosphere.Fallback",
                "The atmosphere is intended to feel {1} and is shaped by lighting that is {0}.",
                "Die Atmosphäre soll {1} wirken und wird durch eine Beleuchtung geprägt, die {0} ist.");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Atmosphere.LightingOnly",
                "The atmosphere is shaped by {0}.",
                "Die Atmosphäre wird durch {0} geprägt.");
            builder.AddOrUpdate("Admin.AI.ImageCreation.Atmosphere.LightingOnly.Fallback",
                "The atmosphere is shaped by lighting that is {0}.",
                "Die Atmosphäre wird durch eine Beleuchtung geprägt, die {0} ist.");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Atmosphere.MoodOnly",
                "The atmosphere feels {0}.",
                "Die Atmosphäre wirkt {0}.");
            builder.AddOrUpdate("Admin.AI.ImageCreation.Atmosphere.MoodOnly.Fallback",
                "The atmosphere is intended to feel {0}.",
                "Die Atmosphäre soll {0} wirken.");

            // I (Inszenierung)
            builder.AddOrUpdate("Admin.AI.ImageCreation.Staging",
                "The staging follows {0}.",
                "Die Inszenierung folgt {0}.");
            builder.AddOrUpdate("Admin.AI.ImageCreation.Staging.Fallback",
                "The staging is arranged with a focus on {0}.",
                "Die Inszenierung ist mit Fokus auf {0} angelegt.");

            // K (Kontext) is handled by PromptGenerators because they know their context.

            // O => Medium params
            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Photo", "photo", "Foto");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Photo", "that of a photograph", "die einer Fotografie");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Painting", "painting", "Gemälde");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Painting", "that of a painting", "die eines Gemäldes");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Illustration", "illustration", "Illustration");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Illustration", "that of an illustration", "die einer Illustration");

            // O => Colors params
            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Colorful", "colorful", "bunt");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Colorful", "colorful", "bunt");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Monochromatic", "monochromatic", "einfarbig");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Monochromatic", "monochromatic", "einfarbig");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Muted", "muted", "gedämpft");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Muted", "muted", "gedämpft");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Bright", "bright", "hell");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Bright", "bright", "hell");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Vibrant", "vibrant", "lebhaft");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Vibrant", "vibrant", "lebhaft");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Pastel", "pastel", "pastellfarben");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Pastel", "pastel", "pastellfarben");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.BlackAndWhite", "black and white", "schwarz-weiß");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.BlackAndWhite", "black and white", "schwarz-weiß");


            // S => Environment params
            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Outdoors", "outdoors", "draußen");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Outdoors", "outdoors in an open environment", "draußen im Freien");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Inside", "indoors", "drinnen");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Inside", "indoors in an interior space", "in einem Innenraum");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Shop", "shop", "Geschäft");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Shop", "inside a shop interior", "in einem Geschäft / Laden");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Kitchen", "kitchen", "Küche");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Kitchen", "in a kitchen interior", "in einer Küche");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.City", "city", "Stadt");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.City", "in an urban city environment", "in einer städtischen Umgebung");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Beach", "beach", "Strand");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Beach", "at a beach", "an einem Strand");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Forest", "forest", "Wald");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Forest", "in a forest", "in einem Wald");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.LivingRoom", "living room", "Wohnzimmer");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.LivingRoom", "in a living room interior", "in einem Wohnzimmer");

            // A => Lighting params
            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Ambient", "ambient", "ambient");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Ambient", "soft ambient lighting", "weiches Umgebungslicht");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Overcast", "overcast", "bewölkt");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Overcast", "overcast daylight", "bewölktes Tageslicht");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Neon", "neon", "neon");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Neon", "neon lighting", "Neonlicht");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Studio", "studio lights", "Studiobeleuchtung");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Studio", "professional studio lighting", "professionelle Studiobeleuchtung");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Soft", "soft", "weich");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Soft", "soft, diffused light", "weiches, diffuses Licht");

            // A => Mood params
            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Cosy", "cosy", "gemütlich");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Cosy", "cosy and inviting", "gemütlich und einladend");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Futuristic", "futuristic", "futuristisch");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Futuristic", "futuristic and high-tech", "futuristisch und technologisch");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Hectic", "hectic", "hektisch");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Hectic", "busy and hectic", "hektisch und geschäftig");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Nostalgic", "nostalgic", "nostalgisch");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Nostalgic", "nostalgic and sentimental", "nostalgisch und sentimental");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Mysterious", "mysterious", "geheimnisvoll");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Mysterious", "mysterious and slightly enigmatic", "geheimnisvoll und rätselhaft");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Relaxing", "relaxing", "entspannend");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Relaxing", "calm and relaxing", "ruhig und entspannend");

            // I => Staging params
            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Asymmetrical", "asymmetrical", "asymmetrisch");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Asymmetrical", "an asymmetrical composition", "einer asymmetrischen Komposition");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.RuleOfThirds", "Rule of thirds", "Drittel-Regel");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.RuleOfThirds", "the rule of thirds", "der Drittel-Regel");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.GoldenRatio", "Golden ratio", "Goldener Schnitt");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.GoldenRatio", "the golden ratio", "dem Goldenen Schnitt");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Closeup", "Closeup", "Nahaufnahme");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Closeup", "a close-up composition", "einer Nahaufnahme-Komposition");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Portrait", "Portrait", "Porträt");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Portrait", "a classic portrait composition", "einer klassischen Porträtkomposition");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Headshot", "Headshot", "Kopf-und-Schultern-Porträt");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Headshot", "a headshot composition", "einer Kopf-und-Schultern-Porträtkomposition");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.Symmetrical", "symmetrical", "symmetrisch");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.Symmetrical", "a symmetrical composition", "einer symmetrischen Komposition");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Param.BirdsEye", "Birds-eye view", "Vogelperspektive");
            builder.AddOrUpdate("Admin.AI.ImageCreation.PromptFragment.BirdsEye", "a bird's-eye view composition", "einer Komposition aus der Vogelperspektive");

            builder.AddOrUpdate("Admin.AI.ImageCreation.Describe", "Add picture description", "Bildbeschreibung hinzufügen");
        }
    }
}

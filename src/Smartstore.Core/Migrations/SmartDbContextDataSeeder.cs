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
                "Admin.Configuration.Settings.Blog.PostsPageSize",
                "Admin.Configuration.Settings.Blog.PostsPageSize.Hint");

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

            builder.AddOrUpdate("Admin.AI.EditImage", "Edit image", "Bild bearbeiten");
        }
    }
}
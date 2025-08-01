using FluentMigrator;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Configuration;
using Smartstore.Core.Security;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2025-07-04 12:00:00", "V620")]
    internal class V620 : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
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
            builder.AddOrUpdate("Aria.Label.MainNavigation",
                "Main navigation",
                "Hauptnavigation",
                "ناوبری اصلی");
            builder.AddOrUpdate("Aria.Label.PageNavigation",
                "Page navigation",
                "Seitennavigation",
                "ناوبری صفحه");
            builder.AddOrUpdate("Aria.Label.OffCanvasMenuTab",
                "Shop sections",
                "Shopbereiche",
                "بخش‌های فروشگاه");
            builder.AddOrUpdate("Aria.Label.ShowPreviousProducts",
                "Show previous product group",
                "Vorherige Produktgruppe anzeigen",
                "نمایش گروه محصولات قبلی");
            builder.AddOrUpdate("Aria.Label.ShowNextProducts",
                "Show next product group",
                "Nächste Produktgruppe anzeigen",
                "نمایش گروه محصولات بعدی");
            builder.AddOrUpdate("Aria.Label.CommentForm",
                "Comment form",
                "Kommentarformular",
                "فرم نظرات");
            builder.AddOrUpdate("Aria.Label.Breadcrumb",
                "Breadcrumb",
                "Breadcrumb-Navigation",
                "ناوبری خرده‌نان");
            builder.AddOrUpdate("Aria.Label.MediaGallery",
                "Media gallery",
                "Mediengalerie",
                "گالری رسانه");
            builder.AddOrUpdate("Aria.Label.SearchFilters",
                "Search filters",
                "Suchfilter",
                "فیلترهای جستجو");
            builder.AddOrUpdate("Aria.Label.SelectDeselectEntries",
                "Select or deselect all entries in the list",
                "Alle Einträge der Liste aus- oder abwählen",
                "انتخاب یا لغو انتخاب همه موارد در لیست");
            builder.AddOrUpdate("Aria.Label.SelectDeselectEntry",
                "Select or deselect entry",
                "Eintrag aus- oder abwählen",
                "انتخاب یا لغو انتخاب مورد");
            builder.AddOrUpdate("Aria.Description.SearchBox",
                "Enter a search term. Press the Enter key to view all the results.",
                "Geben Sie einen Suchbegriff ein. Drücken Sie die Eingabetaste, um alle Ergebnisse aufzurufen.",
                "یک عبارت جستجو وارد کنید. کلید Enter را فشار دهید تا همه نتایج نمایش داده شوند.");
            builder.AddOrUpdate("Aria.Description.InstantSearch",
                "Enter a search term. Results will appear automatically as you type. Press the Enter key to view all the results.",
                "Geben Sie einen Suchbegriff ein. Während Sie tippen, erscheinen automatisch erste Ergebnisse. Drücken Sie die Eingabetaste, um alle Ergebnisse aufzurufen.",
                "یک عبارت جستجو وارد کنید. نتایج به‌صورت خودکار هنگام تایپ نمایش داده می‌شوند. کلید Enter را فشار دهید تا همه نتایج نمایش داده شوند.");
            builder.AddOrUpdate("Aria.Description.AutoSearchBox",
                "Enter a search term. Results will appear automatically as you type.",
                "Geben Sie einen Suchbegriff ein. Die Ergebnisse erscheinen automatisch, während Sie tippen.",
                "یک عبارت جستجو وارد کنید. نتایج به‌صورت خودکار هنگام تایپ نمایش داده می‌شوند.");
            builder.AddOrUpdate("Aria.Label.CurrencySelector",
                "Current currency {0} - Change currency",
                "Aktuelle Währung {0} – Währung wechseln",
                "ارز کنونی {0} - تغییر ارز");
            builder.AddOrUpdate("Aria.Label.LanguageSelector",
                "Current language {0} - Change language",
                "Aktuelle Sprache {0} – Sprache wechseln",
                "زبان کنونی {0} - تغییر زبان");
            builder.AddOrUpdate("Aria.Label.SocialMediaLinks",
                "Our social media channels",
                "Unsere Social-Media-Kanäle",
                "کانال‌های رسانه‌های اجتماعی ما");
            builder.AddOrUpdate("Aria.Label.Rating",
                "Rating: {0} out of 5 stars. {1}",
                "Bewertung: {0} von 5 Sternen. {1}",
                "امتیاز: {0} از 5 ستاره. {1}");
            builder.AddOrUpdate("Aria.Label.ExpandItem",
                "Press ENTER for more options to {0}",
                "Drücken Sie ENTER für mehr Optionen zu {0}",
                "کلید ENTER را برای گزینه‌های بیشتر برای {0} فشار دهید");
            builder.AddOrUpdate("Aria.Label.ProductOfOrderPlacedOn",
                "Order {0} from {1}, {2}",
                "Auftrag {0} vom {1}, {2}",
                "سفارش {0} از {1}، {2}");
            builder.AddOrUpdate("Aria.Label.PaginatorItemsPerPage",
                "Results per page:",
                "Ergebnisse pro Seite:",
                "نتایج در هر صفحه:");
            builder.AddOrUpdate("Aria.Label.ApplyPriceRange",
                "Apply price range",
                "Preisbereich anwenden",
                "اعمال محدوده قیمت");
            builder.AddOrUpdate("Aria.Label.PriceRange",
                "Price range",
                "Preisspanne",
                "محدوده قیمت");
            builder.AddOrUpdate("Aria.Label.UploaderProgressBar",
                "{0} fileupload",
                "{0} Dateiupload",
                "آپلود فایل {0}");
            builder.AddOrUpdate("Aria.Label.ShowPassword",
                "Show password",
                "Passwort anzeigen",
                "نمایش رمز عبور");
            builder.AddOrUpdate("Aria.Label.HidePassword",
                "Hide password",
                "Passwort verbergen",
                "مخفی کردن رمز عبور");
            builder.AddOrUpdate("Aria.Label.CheckoutProcess",
                "Checkout process",
                "Bestellprozess",
                "فرآیند تسویه‌حساب");
            builder.AddOrUpdate("Aria.Label.CheckoutStep.Visited",
                "Completed",
                "Abgeschlossen",
                "تکمیل‌شده");
            builder.AddOrUpdate("Aria.Label.CheckoutStep.Current",
                "Current step",
                "Aktueller Schritt",
                "گام کنونی");
            builder.AddOrUpdate("Aria.Label.CheckoutStep.Pending",
                "Not visited",
                "Noch nicht besucht",
                "بازدیدنشده");
            builder.AddOrUpdate("Aria.Label.SearchHitCount",
                "Search results",
                "Suchergebnisse",
                "نتایج جستجو");

            // INFO: Must be generic for Cart, Compare & Wishlist
            builder.AddOrUpdate("Aria.Label.OffCanvasCartTab",
                "My articles",
                "Meine Artikel",
                "محصولات من");

            builder.AddOrUpdate("Search.SearchBox.Clear",
                "Clear search term",
                "Suchbegriff löschen",
                "پاک کردن عبارت جستجو");

            builder.AddOrUpdate("Common.ScrollUp",
                "Scroll up",
                "Nach oben scrollen",
                "حرکت به بالا");

            builder.AddOrUpdate("Common.SelectAction",
                "Select action",
                "Aktion wählen",
                "انتخاب اقدام");

            builder.AddOrUpdate("Common.ExpandCollapse",
                "Expand/collapse",
                "Auf-/zuklappen",
                "باز/بستن");

            builder.AddOrUpdate("Common.DeleteSelected",
                "Delete selected",
                "Ausgewählte löschen",
                "حذف موارد انتخاب‌شده");

            builder.AddOrUpdate("Common.Consent",
                "Consent",
                "Zustimmung",
                "موافقت");

            builder.AddOrUpdate("Common.SelectView",
                "Select view",
                "Ansicht wählen",
                "انتخاب نمایش");

            builder.AddOrUpdate("Common.SecurityPrompt",
                "Security prompt",
                "Sicherheitsabfrage",
                "سؤال امنیتی");

            builder.AddOrUpdate("Common.SkipToMainContent",
                "Skip to main content",
                "Zum Hauptinhalt springen",
                "پرش به محتوای اصلی");

            builder.Delete(
                "Account.BackInStockSubscriptions.DeleteSelected",
                "PrivateMessages.Inbox.DeleteSelected");

            builder.AddOrUpdate("Admin.Configuration.Settings.General.Common.Captcha.Hint",
      "CAPTCHAs are used for security purposes to help distinguish between human and machine users. They are typically used to verify that internet forms are being"
      + " filled out by humans and not robots (bots), which are often misused for this purpose. reCAPTCHA accounts are created at <a"
      + " class=\"alert-link\" href=\"https://cloud.google.com/security/products/recaptcha?hl=en\" target=\"_blank\">Google</a>. Select <b>Task (v2)</b> as the reCAPTCHA type.",
      "CAPTCHAs dienen der Sicherheit, indem sie dabei helfen, zu unterscheiden, ob ein Nutzer ein Mensch oder eine Maschine ist. In der Regel wird diese Funktion genutzt,"
      + " um zu überprüfen, ob Internetformulare von Menschen oder Robotern (Bots) ausgefüllt werden, da Bots hier oft missbräuchlich eingesetzt werden."
      + " reCAPTCHA-Konten werden bei <a class=\"alert-link\" href=\"https://cloud.google.com/security/products/recaptcha?hl=de\" target=\"_blank\">Google</a>"
      + " angelegt. Wählen Sie als reCAPTCHA-Typ <b>Aufgabe (v2)</b> aus.",
      "کپچاها برای اهداف امنیتی استفاده می‌شوند تا بین کاربران انسانی و ماشینی تمایز قائل شوند. معمولاً برای تأیید اینکه فرم‌های اینترنتی توسط انسان‌ها و نه ربات‌ها (که اغلب برای سوءاستفاده استفاده می‌شوند) پر می‌شوند، به کار می‌روند."
      + " حساب‌های reCAPTCHA در <a class=\"alert-link\" href=\"https://cloud.google.com/security/products/recaptcha?hl=fa\" target=\"_blank\">گوگل</a> ایجاد می‌شوند. نوع reCAPTCHA را <b>وظیفه (v2)</b> انتخاب کنید.");

            builder.AddOrUpdate("Polls.TotalVotes",
     "{0} votes cast.",
     "{0} abgegebene Stimmen.",
     "{0} رأی ثبت‌شده.");

            builder.AddOrUpdate("Blog.RSS.Hint",
                "Opens the RSS feed with the latest blog posts. Subscribe with an RSS reader to stay informed.",
                "Öffnet den RSS-Feed mit aktuellen Blogbeiträgen. Mit einem RSS-Reader abonnieren und informiert bleiben.",
                "خوراک RSS با آخرین پست‌های وبلاگ را باز می‌کند. با یک خواننده RSS اشتراک کنید تا مطلع بمانید.");

            builder.AddOrUpdate("News.RSS.Hint",
                "Opens the RSS feed with the latest news. Subscribe with an RSS reader to stay informed.",
                "Öffnet den RSS-Feed mit aktuellen News. Mit einem RSS-Reader abonnieren und informiert bleiben.",
                "خوراک RSS با آخرین اخبار را باز می‌کند. با یک خواننده RSS اشتراک کنید تا مطلع بمانید.");

            builder.AddOrUpdate("Order.CannotCompleteUnpaidOrder",
                "An order with a payment status of \"{0}\" cannot be completed.",
                "Ein Auftrag mit dem Zahlungsstatus \"{0}\" kann nicht abgeschlossen werden.",
                "سفارشی با وضعیت پرداخت «{0}» نمی‌تواند تکمیل شود.");


            builder.AddOrUpdate("Account.CustomerOrders.RecurringOrders.Cancel",
    "Cancel repeat delivery for order {0}",
    "Regelmäßige Lieferung für Auftrag {0} abbrechen",
    "لغو تحویل دوره‌ای برای سفارش {0}");

            builder.AddOrUpdate("Account.Avatar.AvatarChanged",
                "The avatar has been changed.",
                "Der Avatar wurde geändert.",
                "آواتار تغییر کرد.");

            builder.AddOrUpdate("Account.Avatar.AvatarRemoved",
                "The avatar has been removed.",
                "Der Avatar wurde entfernt.",
                "آواتار حذف شد.");

            builder.AddOrUpdate("RewardPoints.History",
                "History of your reward points",
                "Ihr Bonuspunkteverlauf",
                "تاریخچه امتیازات پاداش شما");

            builder.AddOrUpdate("Reviews.Overview.NoReviews",
    "There are no reviews for this product yet.",
    "Zu diesem Produkt liegen noch keine Bewertungen vor.",
    "هنوز هیچ نظری برای این محصول ثبت نشده است.");

            builder.AddOrUpdate("DownloadableProducts.IAgree",
                "I have read and agree to the user agreement.",
                "Ich habe die Nutzungsvereinbarung gelesen und bin einverstanden.",
                "توافق‌نامه کاربر را خوانده‌ام و موافقم.");

            builder.AddOrUpdate("Common.FormFields.Required.Hint",
     "* Input fields with an asterisk are mandatory and must be filled in.",
     "* Eingabefelder mit Sternchen sind Pflichtfelder und müssen ausgefüllt werden.",
     "* فیلدهای ورودی با علامت ستاره اجباری هستند و باید پر شوند.");


            builder.Delete("Categories.Breadcrumb.Top");

            builder.AddOrUpdate("Order.ShipmentStatusEvents",
     "Status of your shipment",
     "Status Ihrer Sendung",
     "وضعیت ارسال شما");

            builder.AddOrUpdate("BackInStockSubscriptions.PopupTitle",
                "Email when available",
                "E-Mail bei Verfügbarkeit",
                "ایمیل هنگام موجود شدن");
        }
    }
}

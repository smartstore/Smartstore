using FluentMigrator;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2022-08-15 13:00:00", "V5Final")]
    internal class V5Final : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
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
            builder.AddOrUpdate("Admin.Configuration.Settings.Catalog.UseSmallProductBoxOnHomePage",
      "Use small product box on homepage",
      "Kleine Produktbox auf der Homepage verwenden",
      "استفاده از جعبه محصول کوچک در صفحه اصلی",
      "Defines the size of the product boxes on the homepage of your store.",
      "Bestimmt die Größe der Produktboxen auf der Startseite Ihres Shops.",
      "اندازه جعبه‌های محصول در صفحه اصلی فروشگاه شما را مشخص می‌کند.");

            builder.AddOrUpdate("Admin.Configuration.Themes.Option.AssetCachingEnabled.Hint",
                "Determines whether compiled asset files should be cached in file system in order to speed up application restarts. Select 'Auto' if caching should depend on the current environment (Debug = disabled, Production = enabled).",
                "Legt fest, ob kompilierte JS- und CSS-Dateien wie bspw. 'Sass' im Dateisystem zwischengespeichert werden sollen, um den Programmstart zu beschleunigen. Wählen Sie 'Automatisch', wenn das Caching von der aktuellen Umgebung abhängig sein soll (Debug = deaktiviert, Produktiv = aktiviert).",
                "مشخص می‌کند که آیا فایل‌های دارایی کامپایل‌شده باید در سیستم فایل ذخیره شوند تا راه‌اندازی برنامه سریع‌تر شود. اگر ذخیره‌سازی باید به محیط فعلی وابسته باشد (دیباگ = غیرفعال، تولید = فعال)، 'خودکار' را انتخاب کنید.");

            builder.AddOrUpdate("Admin.Configuration.Themes.Option.BundleOptimizationEnabled.Hint",
                "Determines whether asset files (JS and CSS) should be grouped together in order to speed up page rendering. Select 'Auto' if bundling should depend on the current environment (Debug = disabled, Production = enabled).",
                "Legt fest, ob JS- und CSS-Dateien in Gruppen zusammengefasst werden sollen, um den Seitenaufbau zu beschleunigen. Wählen Sie 'Automatisch', wenn das Bundling von der aktuellen Umgebung abhängig sein soll (Debug = deaktiviert, Produktiv = aktiviert).",
                "مشخص می‌کند که آیا فایل‌های دارایی (JS و CSS) باید گروه‌بندی شوند تا رندر صفحه سریع‌تر شود. اگر گروه‌بندی باید به محیط فعلی وابسته باشد (دیباگ = غیرفعال، تولید = فعال)، 'خودکار' را انتخاب کنید.");

            builder.AddOrUpdate("Admin.System.Warnings.TaskScheduler.Fail",
                "The task scheduler cannot poll and execute tasks. Base URL: {0}, Status: {1}. Please specify a working base url in appsettings.json, setting 'Smartstore.TaskSchedulerBaseUrl'.",
                "Der Task-Scheduler kann keine Hintergrund-Aufgaben planen und ausführen. Basis-URL: {0}, Status: {1}. Bitte legen Sie eine vom Webserver erreichbare Basis-URL in der Datei appsettings.json Datei fest, Einstellung: 'Smartstore.TaskSchedulerBaseUrl'.",
                "زمان‌بندی وظایف نمی‌تواند وظایف را بررسی و اجرا کند. آدرس پایه: {0}، وضعیت: {1}. لطفاً یک آدرس پایه کاری در فایل appsettings.json، تنظیم 'Smartstore.TaskSchedulerBaseUrl' مشخص کنید.");

            builder.AddOrUpdate("Admin.Common.DataSuccessfullySaved",
                "The data was saved successfully",
                "Die Daten wurden erfolgreich gespeichert",
                "داده‌ها با موفقیت ذخیره شدند");

            builder.AddOrUpdate("Enums.IndexAnalyzerType.Standard",
                "Standard (standard splitting and filtering)",
                "Standard (Standardtrennung und -Filterung)",
                "استاندارد (جداسازی و فیلتر استاندارد)");

            builder.AddOrUpdate("Enums.IndexAnalyzerType.Whitespace",
                "Whitespace (split only for blanks, no filtering)",
                "Whitespace (nur bei Leerzeichen trennen, keine Filterung)",
                "فضای خالی (فقط با فاصله جدا شود، بدون فیلتر)");

            builder.AddOrUpdate("Enums.IndexAnalyzerType.Keyword",
                "Keyword (no splitting, no filtering)",
                "Keyword (keine Trennung, keine Filterung)",
                "کلمه کلیدی (بدون جداسازی، بدون فیلتر)");

            builder.AddOrUpdate("Enums.IndexAnalyzerType.Classic",
                "Classic (classic splitting and filtering)",
                "Classic (klassische Trennung und Filterung)",
                "کلاسیک (جداسازی و فیلتر کلاسیک)");

            builder.AddOrUpdate("Admin.Plugins.KnownGroup.Law",
                "Law",
                "Gesetz",
                "قانون");

            builder.AddOrUpdate("Admin.Orders.List.PaymentId",
                "Payment ID",
                "Zahlungs-ID",
                "شناسه پرداخت",
                "Search by the payment transaction ID (authorization or capturing)",
                "Suche über die Zahlungstransaktions-ID (Autorisierung oder Buchung)",
                "جستجو بر اساس شناسه تراکنش پرداخت (مجوز یا ثبت)");

            builder.AddOrUpdate("Identity.AuthenticationCredentials",
                "Authentication credentials",
                "Zugangsdaten",
                "اطلاعات احراز هویت");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.DisplayRegionInLanguageSelector",
                "Display region in language selector",
                "Region in der Sprachauswahl anzeigen",
                "نمایش منطقه در انتخاب‌گر زبان",
                "Whether to display region/country name in language selector (e.g. 'Deutsch (Deutschland)' instead of 'Deutsch')",
                "Zeigt den Namen der Region/des Landes in der Sprachauswahl an (z. B. 'Deutsch (Deutschland)' statt 'Deutsch')",
                "آیا نام منطقه/کشور در انتخاب‌گر زبان نمایش داده شود یا خیر (مثلاً 'فارسی (ایران)' به جای 'فارسی')");

            builder.AddOrUpdate("Payment.PaymentFailure",
                "A problem has occurred with this payment method. Please try again or select another payment method.",
                "Mit dieser Zahlungsart ist ein Problem aufgetreten. Bitte versuchen Sie es erneut oder wählen Sie eine andere Zahlungsart aus.",
                "مشکلی در این روش پرداخت رخ داده است. لطفاً دوباره تلاش کنید یا روش پرداخت دیگری انتخاب کنید.");

            builder.AddOrUpdate("Payment.MissingCheckoutState",
                "Missing checkout session state ({0}). Your payment cannot be processed. Please go to your shopping cart and checkout again.",
                "Fehlender Checkout-Sitzungsstatus ({0}). Ihre Zahlung kann leider nicht bearbeitet werden. Bitte gehen Sie zurück zum Warenkorb und Durchlaufen Sie den Checkout erneut.",
                "وضعیت جلسه پرداخت ({0}) وجود ندارد. پرداخت شما قابل پردازش نیست. لطفاً به سبد خرید خود برگردید و دوباره پرداخت را انجام دهید.");

            builder.AddOrUpdate("Payment.InvalidCredentials",
                "The credentials for the payment provider are incomplete. Please enter the required credentials in the configuration area of the payment method.",
                "Die Zugangsdaten zum Zahlungsanbieter sind unvollständig. Bitte geben Sie die erforderlichen Zugangsdaten im Konfigurationsbereich der Zahlungsart ein.",
                "اطلاعات احراز هویت ارائه‌دهنده پرداخت ناقص است. لطفاً اطلاعات مورد نیاز را در بخش تنظیمات روش پرداخت وارد کنید.");

            builder.AddOrUpdate("Admin.Configuration.Payment.Methods.AddOrderNotes",
                "Create order notes",
                "Auftragsnotizen anlegen",
                "ایجاد یادداشت‌های سفارش",
                "Specifies whether to create order notes when exchanging data with the payment provider.",
                "Legt fest, ob beim Datenaustausch mit dem Zahlungsanbieter Auftragsnotizen angelegt werden sollen.",
                "مشخص می‌کند که آیا هنگام تبادل داده با ارائه‌دهنده پرداخت، یادداشت‌های سفارش ایجاد شوند یا خیر.");

            builder.AddOrUpdate("Admin.Address.Fields.Country.MustBePublished",
                "Invalid country",
                "Ungültiges Land",
                "کشور نامعتبر");

            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.LastDeviceFamily",
                "Last device family",
                "Zuletzt genutzte Endgerätefamilie",
                "آخرین خانواده دستگاه");

            builder.AddOrUpdate("Admin.Rules.RuleOperator.LikeOperator",
                "Like",
                "Like",
                "شبیه");

            builder.AddOrUpdate("Admin.Rules.RuleOperator.NotLikeOperator",
                "Not like",
                "Not like",
                "غیرشبیه");

            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.CartItemFromCategoryQuantity",
                "Product quantity from category is in range",
                "Produktmenge aus Warengruppe liegt in folgendem Bereich",
                "تعداد محصول از دسته‌بندی در محدوده است");

            builder.AddOrUpdate("PDFInvoice.TaxNumber").Value("en", "Tax Number:");
            builder.AddOrUpdate("PDFInvoice.VatId").Value("en", "Vat-ID:");

            builder.AddOrUpdate("Admin.Catalog.Products.List.SearchProductType",
                "Product type",
                "Produkttyp",
                "نوع محصول");

            builder.AddOrUpdate("Admin.Catalog.Products.List.SearchCategory",
                "Category",
                "Warengruppe",
                "دسته‌بندی");

            builder.AddOrUpdate("Admin.Catalog.Products.List.SearchManufacturer",
                "Manufacturer",
                "Hersteller",
                "تولیدکننده");

            builder.AddOrUpdate("Admin.Catalog.Products.List.SearchProductName",
                "Product name",
                "Produktname",
                "نام محصول");

            builder.AddOrUpdate("Admin.AccessDenied.DetailedDescription",
                "You do not have authorization to perform this operation. Permission: {0}, Systemname: {1}.",
                "Sie haben keine Berechtigung, diesen Vorgang durchzuführen. Zugriffsrecht: {0}, Systemname: {1}.",
                "شما مجوز انجام این عملیات را ندارید. دسترسی: {0}، نام سیستم: {1}.");
        }
    }
}

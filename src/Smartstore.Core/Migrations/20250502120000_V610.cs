using FluentMigrator;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Configuration;
using Smartstore.Core.Security;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    [MigrationVersion("2025-05-02 12:00:00", "V610")]
    internal class V610 : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
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
            await MigrateSettingsAsync(context, cancelToken);
        }

        public async Task MigrateSettingsAsync(SmartDbContext db, CancellationToken cancelToken = default)
        {
            await SettingFactory.SaveSettingsAsync(db, new PerformanceSettings(), false);
            await SettingFactory.SaveSettingsAsync(db, new ResiliencySettings(), false);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Configuration.Settings.Search.CommonFacet.Sorting",
     "Sorting",
     "Sortierung",
     "مرتب‌سازی",
     "Specifies the sorting of the search filters.",
     "Legt die Sortierung der Suchfilter fest.",
     "مرتب‌سازی فیلترهای جستجو را مشخص می‌کند.");

            builder.AddOrUpdate("Enums.FacetSorting.ValueAsc",
                "Value/ID: lowest first",
                "Wert/ID: Niedrigste zuerst",
                "مقدار/شناسه: کمترین اول");

            builder.AddOrUpdate("Admin.Common.ExportToPdf.TooManyItems",
                "Too many objects! A maximum of {0} objects can be converted. Please reduce the number of selected data records ({1}) or increase the limit in the PDF settings.",
                "Zu viele Objekte! Es können maximal {0} Objekte konvertiert werden. Bitte reduzieren Sie die Anzahl der ausgewählten Datensätze ({1}) oder erhöhen Sie das Limit in den PDF-Einstellungen.",
                "تعداد اشیاء بیش از حد است! حداکثر {0} شیء می‌تواند تبدیل شود. لطفاً تعداد رکوردهای داده انتخاب‌شده ({1}) را کاهش دهید یا محدودیت را در تنظیمات PDF افزایش دهید.");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.MaxItemsToPrint",
                "Maximum number of objects to print",
                "Maximale Anzahl zu druckender Objekte",
                "حداکثر تعداد اشیاء برای چاپ",
                "Specifies the maximum number of objects to be printed, above which an error message is issued. The default value is 500 and should not be set too high so that the process does not take too long.",
                "Legt die maximale Anzahl der zu druckenden Objekte fest, bei deren Überschreitung eine Fehlermeldung ausgegeben wird. Der Standardwert ist 500 und sollte nicht zu hoch gewählt werden, damit der Vorgang nicht zu lange dauert.",
                "حداکثر تعداد اشیاء برای چاپ را مشخص می‌کند، که در صورت تجاوز از آن، پیام خطا صادر می‌شود. مقدار پیش‌فرض 500 است و نباید خیلی زیاد تنظیم شود تا فرآیند خیلی طولانی نشود.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Shipping.CalculateShippingAtCheckout",
                "Calculate shipping costs during checkout",
                "Versandkosten während des Checkouts berechnen",
                "محاسبه هزینه‌های حمل و نقل در هنگام پرداخت",
                "Specifies whether shipping costs are displayed on the shopping cart page as long as the customer has not yet entered a shipping address." +
                " If activated, a note appears instead that the calculation will only take place at checkout.",
                "Legt fest, ob Versandkosten auf der Warenkorbseite angezeigt werden, solange der Kunde noch keine Lieferanschrift eingegeben hat." +
                " Wenn aktiviert, erscheint stattdessen ein Hinweis, dass die Berechnung erst beim Checkout erfolgt.",
                "مشخص می‌کند که آیا هزینه‌های حمل و نقل در صفحه سبد خرید نمایش داده می‌شود، تا زمانی که مشتری هنوز آدرس حمل و نقل را وارد نکرده است." +
                " اگر فعال باشد، در عوض یک یادداشت ظاهر می‌شود که محاسبه فقط در هنگام پرداخت انجام می‌شود.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Shipping.UseShippingOriginIfShippingAddressMissing",
                "Shipping origin determines shipping costs if shipping address is missing",
                "Artikelstandort bestimmt Versandkosten bei fehlender Versandanschrift",
                "مبدا حمل و نقل هزینه‌های حمل و نقل را تعیین می‌کند اگر آدرس حمل و نقل موجود نباشد",
                "Specifies that if the customer has never checked out and the shipping address is unknown, the shipping cost from the location where the order was shipped" +
                " (according to \"Shipping Origin\") will be used.",
                "Legt fest, dass die Versandkosten des Ortes verwendet werden, von dem aus der Versand erfolgt (gemäß \"Versand erfolgt ab\")," +
                " sofern der Kunde den Checkout noch nie durchlaufen hat und seine Lieferanschrift unbekannt ist.",
                "مشخص می‌کند که اگر مشتری هرگز پرداخت را انجام نداده باشد و آدرس حمل و نقل ناشناخته باشد، هزینه حمل و نقل از مکانی که سفارش از آنجا ارسال شده است" +
                " (بر اساس \"مبدا حمل و نقل\") استفاده می‌شود.");

            builder.AddOrUpdate("Common.CartRules",
                "Cart rules",
                "Warenkorbregeln",
                "قوانین سبد خرید");

            builder.AddOrUpdate("Common.CustomerRules",
                "Customer rules",
                "Kundenregeln",
                "قوانین مشتری");

            builder.AddOrUpdate("Common.ProductRules",
                "Product rules",
                "Produktregeln",
                "قوانین محصول");

            builder.AddOrUpdate("Admin.Common.RestartHint",
                "Changes to the following settings only take effect after the application has been restarted.",
                "Änderungen an den folgenden Einstellungen werden erst nach einem Neustart der Anwendung wirksam.",
                "تغییرات در تنظیمات زیر تنها پس از راه‌اندازی مجدد برنامه اعمال می‌شود.");

            builder.AddOrUpdate("Admin.Configuration.Settings.RewardPoints.RoundDownPointsForPurchasedAmount",
                "Round down the amount of points for a purchase",
                "Betrag bei Punkten für einen Einkauf abrunden",
                "گرد کردن به پایین مقدار امتیازات برای یک خرید",
                "Specifies whether to round down the amount when calculating the reward points awarded for a product purchase.",
                "Legt fest, ob der Betrag bei der Berechnung der Bonuspunkte, die für den Kauf eines Produkts gewährt werden, abgerundet werden soll.",
                "مشخص می‌کند که آیا هنگام محاسبه امتیازات پاداش اعطا شده برای خرید یک محصول، مقدار را به پایین گرد کنیم.");

            builder.AddOrUpdate("Admin.Configuration.Settings.CustomerUser.HideMyAccountOrders",
                "Hide orders in the \"My account\" area",
                "Bestellungen im Bereich \"Mein Konto\" ausblenden",
                "پنهان کردن سفارشات در بخش \"حساب من\"");

            builder.AddOrUpdate("Admin.Rules.FilterDescriptor.VariantInCart",
                "Product with SKU in cart",
                "Produkt mit SKU im Warenkorb",
                "محصول با SKU در سبد خرید");

            builder.AddOrUpdate("Admin.RecurringPayments.History")
     .Value("de", "Historie")
     .Value("fa", "تاریخچه");
            builder.AddOrUpdate("Admin.RecurringPayments.Fields.CyclesRemaining")
     .Value("de", "Verbleibende Zahlungen")
     .Value("fa", "پرداخت‌های باقی‌مانده");
            builder.AddOrUpdate("Admin.RecurringPayments.Fields.CyclesRemaining.Hint")
        .Value("de", "Die Anzahl der verbleibenden Zahlungen")
        .Value("fa", "تعداد پرداخت‌های باقی‌مانده");

            builder.AddOrUpdate("Admin.RecurringPayments.List.RemainingCycles",
      "Remaining payments",
      "Verbleibende Zahlungen",
      "پرداخت‌های باقی‌مانده",
      "Filter list by remaining payments.",
      "Liste nach verbleibenden Zahlungen filtern.",
      "لیست را بر اساس پرداخت‌های باقی‌مانده فیلتر کنید.");

            // Frontend renaming: "Wiederkehrende Zahlung" -> "Regelmäßige Lieferung".
            builder.AddOrUpdate("Account.CustomerOrders.RecurringOrders.Cancel",
       "Cancel repeat delivery",
       "Regelmäßige Lieferung abbrechen",
       "لغو سفارش دوره‌ای");

            builder.AddOrUpdate("Account.CustomerOrders.RecurringOrders",
                "Repeat deliveries",
                "Regelmäßige Lieferungen",
                "سفارش‌های دوره‌ای");

            builder.AddOrUpdate("Account.CustomerOrders.RecurringOrders.TotalCycles",
                "Total deliveries",
                "Lieferungen insgesamt",
                "تعداد کل تحویل‌ها");

            builder.AddOrUpdate("ShoppingCart.RecurringPeriod",
                "[Repeat deliveries every {0} {1}]",
                "[Regelmäßige Lieferung alle {0} {1}]",
                "[تحویل مکرر هر {0} {1}]");

            builder.AddOrUpdate("Account.CustomerOrders.RecurringOrders.CancelDelivery",
     "Would you like to cancel the repeat delivery?",
     "Möchten Sie die regelmäßige Lieferung abbrechen?",
     "آیا مایل به لغو تحویل دوره‌ای هستید؟");

            builder.AddOrUpdate("Account.CustomerOrders.RecurringOrders.SuccessfullyCanceled",
                "The repeat delivery was successfully canceled.",
                "Die regelmäßige Lieferung wurde erfolgreich abgebrochen.",
                "تحویل دوره‌ای با موفقیت لغو شد.");

            builder.Delete(
                "Admin.RecurringPayments.History.OrderStatus",
                "Admin.RecurringPayments.History.PaymentStatus",
                "Admin.RecurringPayments.History.ShippingStatus",
                "Admin.Orders.Products.RecurringPeriod",
                "Account.CustomerOrders.RecurringOrders.ViewInitialOrder");

            builder.AddOrUpdate("Admin.Catalog.Products.RecycleBin.DeleteProductsResult",
     "{0} of {1} products have been permanently deleted.",
     "Es wurden {0} von {1} Produkten endgültig gelöscht.",
     "{0} از {1} محصول به طور دائم حذف شده‌اند.");

            builder.AddOrUpdate("Admin.Catalog.Products.RecycleBin.DeletedAndSkippedProductsResult",
                "{0} of {1} products have been permanently deleted. {2} Products were skipped as they are assigned to orders and cannot be permanently deleted.",
                "{0} von {1} Produkten wurden endgültig gelöscht. {2} Produkte wurden ausgelassen, da sie Aufträgen zugeordnet sind und nicht permanent gelöscht werden können.",
                "{0} از {1} محصول به طور دائم حذف شده‌اند. {2} محصول به دلیل اختصاص به سفارش‌ها حذف نشدند، زیرا نمی‌توان آن‌ها را به طور دائم حذف کرد.");

            builder.AddOrUpdate("Order.CannotCompleteUnpaidOrder",
                "An unpaid order cannot be completed.",
                "Ein unbezahlter Auftrag kann nicht abgeschlossen werden.",
                "یک سفارش پرداخت نشده نمی‌تواند تکمیل شود.");

            builder.Delete(
                "Admin.Orders.List.StartDate",
                "Admin.Orders.List.StartDate.Hint",
                "Admin.Orders.List.EndDate",
                "Admin.Orders.List.EndDate.Hint",
                "Admin.Customers.Reports.BestBy.StartDate",
                "Admin.Customers.Reports.BestBy.StartDate.Hint",
                "Admin.Customers.Reports.BestBy.EndDate",
                "Admin.Customers.Reports.BestBy.EndDate.Hint",
                "Admin.SalesReport.Bestsellers.StartDate",
                "Admin.SalesReport.Bestsellers.StartDate.Hint",
                "Admin.SalesReport.Bestsellers.EndDate",
                "Admin.SalesReport.Bestsellers.EndDate.Hint",
                "Admin.SalesReport.NeverSold.StartDate",
                "Admin.SalesReport.NeverSold.StartDate.Hint",
                "Admin.SalesReport.NeverSold.EndDate",
                "Admin.SalesReport.NeverSold.EndDate.Hint",
                "Admin.Orders.Shipments.List.StartDate",
                "Admin.Orders.Shipments.List.StartDate.Hint",
                "Admin.Orders.Shipments.List.EndDate",
                "Admin.Orders.Shipments.List.EndDate.Hint",
                "Admin.Common.Search.StartDate",
                "Admin.Common.Search.StartDate.Hint",
                "Admin.Common.Search.EndDate",
                "Admin.Common.Search.EndDate.Hint",
                "Admin.System.QueuedEmails.List.StartDate",
                "Admin.System.QueuedEmails.List.StartDate.Hint",
                "Admin.System.QueuedEmails.List.EndDate",
                "Admin.System.QueuedEmails.List.EndDate.Hint");

            builder.AddOrUpdate("Admin.Media.Editing.Align", "Align", "Ausrichten", "تراز کردن");

            builder.AddOrUpdate("Admin.Media.Editing.AlignTop", "Top", "Oben", "بالا");
            builder.AddOrUpdate("Admin.Media.Editing.AlignMiddle", "Center", "Mitte", "مرکز");
            builder.AddOrUpdate("Admin.Media.Editing.AlignBottom", "Bottom", "Unten", "پایین");


            #region Performance settings

            var prefix = "Admin.Configuration.Settings.Performance";

            builder.AddOrUpdate($"{prefix}", "Performance", "Leistung", "عملکرد");
            builder.AddOrUpdate($"{prefix}.Resiliency", "Overload protection", "Überlastungsschutz", "محافظت در برابر بار اضافی");
            builder.AddOrUpdate($"{prefix}.Cache", "Cache", "Cache", "کش");

            builder.AddOrUpdate($"{prefix}.Hint",
                "For technically experienced users only. Pay attention to the CPU and memory usage when changing these settings.",
                "Nur für technisch erfahrene Benutzer. Achten Sie auf die CPU- und Speicherauslastung, wenn Sie diese Einstellungen ändern.",
                "تنها برای کاربران با تجربه فنی. هنگام تغییر این تنظیمات به مصرف CPU و حافظه توجه کنید.");

            builder.AddOrUpdate($"{prefix}.CacheSegmentSize",
                "Cache segment size",
                "Cache Segment Größe",
                "اندازه بخش کش",
                "The number of entries in a single cache segment when greedy loading is disabled. The larger the catalog, the smaller this value should be. We recommend segment size of 500 for catalogs with less than 100.000 items.",
                "Die Anzahl der Einträge in einem einzelnen Cache-Segment, wenn Greedy Loading deaktiviert ist. Je größer der Katalog ist, desto kleiner sollte dieser Wert sein. Wir empfehlen eine Segmentgröße von 500 für Kataloge mit weniger als 100.000 Einträgen.",
                "تعداد ورودی‌ها در یک بخش کش زمانی که بارگذاری حریصانه غیرفعال است. هرچه کاتالوگ بزرگ‌تر باشد، این مقدار باید کوچک‌تر باشد. ما اندازه بخش 500 را برای کاتالوگ‌های با کمتر از 100,000 آیتم توصیه می‌کنیم.");

            builder.AddOrUpdate($"{prefix}.AlwaysPrefetchTranslations",
                "Always prefetch translations",
                "Übersetzungen immer vorladen (Prefetch)",
                "همیشه ترجمه‌ها را پیش‌بارگذاری کنید",
                "By default, only Instant Search prefetches translations. All other product listings work against the segmented cache. For very large multilingual catalogs (> 500,000), enabling this can improve query performance and reduce resource usage.",
                "Standardmäßig werden nur bei der Sofortsuche Übersetzungen vorgeladen. Alle anderen Produktauflistungen arbeiten mit dem segmentierten Cache. Bei sehr großen mehrsprachigen Katalogen (> 500.000) kann die Aktivierung dieser Option die Abfrageleistung verbessern und die Ressourcennutzung verringern.",
                "به طور پیش‌فرض، فقط جستجوی فوری ترجمه‌ها را پیش‌بارگذاری می‌کند. تمام لیست‌های محصولات دیگر با کش بخش‌بندی‌شده کار می‌کنند. برای کاتالوگ‌های چندزبانه بسیار بزرگ (> 500,000)، فعال کردن این گزینه می‌تواند عملکرد پرس‌وجو را بهبود بخشد و مصرف منابع را کاهش دهد.");

            builder.AddOrUpdate($"{prefix}.AlwaysPrefetchUrlSlugs",
                "Always prefetch URL slugs",
                "URL Slugs immer vorladen (Prefetch)",
                "همیشه اسلاگ‌های URL را پیش‌بارگذاری کنید",
                "By default, only Instant Search prefetches URL slugs. All other product listings work against the segmented cache. For very large multilingual catalogs (> 500,000), enabling this can improve query performance and reduce resource usage.",
                "Standardmäßig werden nur bei der Sofortsuche URL slugs vorgeladen. Alle anderen Produktauflistungen arbeiten mit dem segmentierten Cache. Bei sehr großen mehrsprachigen Katalogen (> 500.000) kann die Aktivierung dieser Option die Abfrageleistung verbessern und die Ressourcennutzung verringern.",
                "به طور پیش‌فرض، فقط جستجوی فوری اسلاگ‌های URL را پیش‌بارگذاری می‌کند. تمام لیست‌های محصولات دیگر با کش بخش‌بندی‌شده کار می‌کنند. برای کاتالوگ‌های چندزبانه بسیار بزرگ (> 500,000)، فعال کردن این گزینه می‌تواند عملکرد پرس‌وجو را بهبود بخشد و مصرف منابع را کاهش دهد.");

            builder.AddOrUpdate($"{prefix}.MaxUnavailableAttributeCombinations",
                "Max. unavailable attribute combinations",
                "Max. nicht verfügbare Attributkombinationen",
                "حداکثر ترکیبات ویژگی‌های ناموجود",
                "Maximum number of attribute combinations that will be loaded and parsed to make them unavailable for selection on the product detail page.",
                "Maximale Anzahl von Attributkombinationen, die geladen und analysiert werden, um nicht verfügbare Kombinationen zu ermitteln.",
                "حداکثر تعداد ترکیبات ویژگی‌هایی که بارگذاری و تجزیه می‌شوند تا برای انتخاب در صفحه جزئیات محصول ناموجود شوند.");

            builder.AddOrUpdate($"{prefix}.MediaDupeDetectorMaxCacheSize",
                "Media Duplicate Detector max. cache size",
                "Max. Cache-Größe für Medien-Duplikat-Detektor",
                "حداکثر اندازه کش برای تشخیص‌دهنده فایل‌های تکراری رسانه",
                "Maximum number of MediaFile entities to cache for duplicate file detection. If a media folder contains more files, no caching is done for scalability reasons and the MediaFile entities are loaded directly from the database.",
                "Maximale Anzahl der MediaFile-Entitäten, die für die Duplikat-Erkennung zwischengespeichert werden. Enthält ein Medienordner mehr Dateien, erfolgt aus Gründen der Skalierbarkeit keine Zwischenspeicherung und die MediaFile-Entitäten werden direkt aus der Datenbank geladen.",
                "حداکثر تعداد موجودیت‌های MediaFile که برای تشخیص فایل‌های تکراری در کش ذخیره می‌شوند. اگر یک پوشه رسانه‌ای فایل‌های بیشتری داشته باشد، به دلایل مقیاس‌پذیری، کشی انجام نمی‌شود و موجودیت‌های MediaFile مستقیماً از پایگاه داده بارگذاری می‌شوند.");

            prefix = "Admin.Configuration.Settings.Resiliency";

            builder.AddOrUpdate($"{prefix}.Description",
                @"Overload protection is used to keep server resources under control, prevent latencies from getting out of hand and keep the system performant and available 
in the event of increased traffic (e.g. due to unidentifiable ""Bad Bots"", peaks, sales events, sudden high visitor numbers).
Limits only apply to guest accounts and bots, not to registered users.",
                @"Überlastungsschutz dient dazu, die Server-Ressourcen unter Kontrolle zu halten, Latenzen nicht ausufern zu lassen und im Falle von erhöhtem Ansturm 
(z.B. durch nicht identifizierbare ""Bad-Bots"", Peaks, Sales Events, plötzlich hohe Nutzerzahlen) das System performant und verfügbar zu halten.
Limits gelten nur für Gastkonten und Bots, nicht für registrierte User.",
                @"محافظت در برابر بار اضافی برای کنترل منابع سرور، جلوگیری از افزایش بی‌رویه تأخیرها و حفظ عملکرد و دسترسی سیستم در صورت افزایش ترافیک (مثلاً به دلیل ""بات‌های بد"" ناشناس، اوج‌ها، رویدادهای فروش، تعداد بالای ناگهانی بازدیدکنندگان) استفاده می‌شود.
محدودیت‌ها فقط برای حساب‌های مهمان و بات‌ها اعمال می‌شود، نه برای کاربران ثبت‌نام‌شده.");

            builder.AddOrUpdate($"{prefix}.LongTraffic", "Traffic limit", "Besucherlimit", "محدودیت ترافیک");
            builder.AddOrUpdate($"{prefix}.LongTrafficNotes",
                "Configuration of the long traffic window. Use these settings to monitor traffic restrictions over a longer period of time, such as a minute or longer.",
                "Konfiguration des langen Zeitfensters. Verwenden Sie diese Einstellungen, um Limits über einen längeren Zeitraum zu überwachen, z.B. eine Minute oder länger.",
                "پیکربندی پنجره ترافیک طولانی. از این تنظیمات برای نظارت بر محدودیت‌های ترافیک در یک دوره زمانی طولانی‌تر، مانند یک دقیقه یا بیشتر، استفاده کنید.");

            builder.AddOrUpdate($"{prefix}.PeakTraffic", "Peak", "Lastspitzen", "اوج");
            builder.AddOrUpdate($"{prefix}.PeakTrafficNotes",
                "The peak traffic window defines the shorter period used for detecting sudden traffic spikes. These settings are useful for reacting to bursts of traffic that occur in a matter of seconds.",
                "Der kürzere Zeitraum, der für die Erkennung plötzlicher Lastspitzen verwendet wird. Diese Einstellungen sind nützlich, um auf Lastspitzen zu reagieren, die innerhalb weniger Sekunden auftreten.",
                "پنجره ترافیک اوج، دوره کوتاه‌تری را برای تشخیص افزایش ناگهانی ترافیک تعریف می‌کند. این تنظیمات برای واکنش به انفجارهای ترافیکی که در عرض چند ثانیه رخ می‌دهند، مفید هستند.");

            builder.AddOrUpdate($"{prefix}.TrafficTimeWindow",
                "Time window (hh:mm:ss)",
                "Zeitfenster (hh:mm:ss)",
                "پنجره زمانی (hh:mm:ss)",
                "The duration of the traffic window, which defines the period of time during which sustained traffic is measured.",
                "Die Dauer des Zeitfensters, das den Zeitraum definiert, in dem der anhaltende Traffic gemessen wird.",
                "مدت زمان پنجره ترافیک، که دوره زمانی را که در آن ترافیک پایدار اندازه‌گیری می‌شود، تعریف می‌کند.");

            builder.AddOrUpdate($"{prefix}.TrafficLimitGuest",
                "Guest limit",
                "Gäste-Grenzwert",
                "محدودیت مهمان",
                "The maximum number of requests allowed from guest users within the duration of the defined time window. Empty value means there is no limit applied for guest users.",
                "Die maximale Anzahl von Gastbenutzern innerhalb des festgelegten Zeitfensters. Ein leerer Wert bedeutet: keine Begrenzung.",
                "حداکثر تعداد درخواست‌های مجاز از کاربران مهمان در مدت زمان پنجره زمانی تعریف‌شده. مقدار خالی به معنای عدم اعمال محدودیت برای کاربران مهمان است.");

            builder.AddOrUpdate($"{prefix}.TrafficLimitBot",
                "Bot limit",
                "Bot-Grenzwert",
                "محدودیت بات",
                "The maximum number of requests allowed from bots within the duration of the defined time window. Empty value means there is no limit applied for bots.",
                "Die maximale Anzahl von Bots innerhalb des festgelegten Zeitfensters. Ein leerer Wert bedeutet: keine Begrenzung.",
                "حداکثر تعداد درخواست‌های مجاز از بات‌ها در مدت زمان پنجره زمانی تعریف‌شده. مقدار خالی به معنای عدم اعمال محدودیت برای بات‌ها است.");

            builder.AddOrUpdate($"{prefix}.TrafficLimitGlobal",
                "Global limit",
                "Globaler Grenzwert",
                "محدودیت جهانی",
                @"The global traffic limit for both guests and bots together. This limit applies to the combined traffic from guests and bots. 
It ensures that the overall system load remains within acceptable thresholds, regardless of the distribution of requests among specific user types.  
Unlike guest or bot limiters, this global limit acts as a safeguard for the entire system. If the cumulative requests from both types exceed this limit 
within the observation window, additional requests may be denied, even if type-specific limits have not been reached. An empty value means that there is no global limit.",
                @"Das globale Limit für Gäste und Bots zusammen. Dieses Limit gilt für den kombinierten Traffic von Gästen und Bots. 
Es stellt sicher, dass die Gesamtsystemlast innerhalb akzeptabler Schwellenwerte bleibt, unabhängig von der Verteilung der Anfragen auf bestimmte Benutzertypen. 
Im Gegensatz zu Gast- oder Bot-Limitern dient dieses globale Limit als Schutz für das gesamte System. 
Wenn die kumulierten Anfragen beider Typen dieses Limit innerhalb des Beobachtungsfensters überschreiten, werden weitere Anfragen abgelehnt, 
auch wenn die typspezifischen Limits nicht erreicht wurden. Ein leerer Wert bedeutet: keine Begrenzung.",
                @"محدودیت ترافیک جهانی برای مهمان‌ها و بات‌ها با هم. این محدودیت برای ترافیک ترکیبی از مهمان‌ها و بات‌ها اعمال می‌شود. 
اطمینان می‌دهد که بار کلی سیستم در آستانه‌های قابل قبول باقی بماند، صرف نظر از توزیع درخواست‌ها در میان انواع کاربران خاص.  
برخلاف محدودیت‌های مهمان یا بات، این محدودیت جهانی به عنوان محافظی برای کل سیستم عمل می‌کند. اگر درخواست‌های تجمعی از هر دو نوع از این محدودیت در پنجره مشاهده فراتر رود، 
درخواست‌های اضافی ممکن است رد شوند، حتی اگر محدودیت‌های خاص نوع نرسیده باشند. مقدار خالی به معنای عدم وجود محدودیت جهانی است.");

            builder.AddOrUpdate($"{prefix}.EnableOverloadProtection",
                "Enable overload protection",
                "Überlastungsschutz aktivieren",
                "فعال کردن محافظت در برابر بار اضافی",
                "When enabled, the system applies the defined traffic limits and overload protection policies. If disabled, no traffic limits are enforced.",
                "Wendet die festgelegten Richtlinien an. Wenn diese Option deaktiviert ist, werden keine Begrenzungen erzwungen.",
                "هنگامی که فعال است، سیستم محدودیت‌های ترافیک تعریف‌شده و سیاست‌های محافظت در برابر بار اضافی را اعمال می‌کند. اگر غیرفعال باشد، هیچ محدودیت ترافیکی اعمال نمی‌شود.");

            builder.AddOrUpdate($"{prefix}.ForbidNewGuestsIfSubRequest",
                "If sub request, forbid \"new\" guests",
                "Wenn Sub-Request, \"neue\" Gäste blockieren",
                "اگر درخواست فرعی باشد، مهمان‌های \"جدید\" را ممنوع کنید",
                @"Forbids ""new"" guest users if the request is a sub/secondary request, e.g., an AJAX request, POST, script, media file, etc. This setting can be used to restrict 
the creation of new guest sessions on successive (secondary) resource requests. A ""bad bot"" that does not accept cookies is difficult to identify 
as a bot and may create a new guest session with each (sub)-request, especially if it varies its client IP address and user agent string with each request. 
If enabled, new guests will be blocked under these circumstances.",
                @"Blockiert ""neue"" Gastbenutzer, wenn es sich bei der Anforderung um eine untergeordnete/sekundäre Anforderung handelt, z. B. AJAX, POST, Skript, Mediendatei usw. 
Diese Einstellung kann verwendet werden, um die Erstellung neuer Gastsitzungen bei aufeinander folgenden (sekundären) Ressourcenanfragen einzuschränken. 
Ein ""Bad Bot"", der keine Cookies akzeptiert, ist schwer als Bot zu erkennen und kann bei jeder (Unter-)Anfrage eine neue Gastsitzung erzeugen, 
insbesondere wenn er seine Client-IP-Adresse und den User-Agent-String bei jeder Anfrage ändert. 
Wenn diese Option aktiviert ist, werden neue Gäste unter diesen Umständen blockiert.",
                @"اگر درخواست یک درخواست فرعی/ثانویه باشد، مانند درخواست AJAX، POST، اسکریپت، فایل رسانه‌ای و غیره، کاربران مهمان ""جدید"" را ممنوع می‌کند. این تنظیم می‌تواند برای محدود کردن 
ایجاد جلسات مهمان جدید در درخواست‌های منابع متوالی (ثانویه) استفاده شود. یک ""بات بد"" که کوکی‌ها را نمی‌پذیرد، به سختی به عنوان بات شناسایی می‌شود 
و ممکن است با هر (زیر)-درخواست یک جلسه مهمان جدید ایجاد کند، به ویژه اگر آدرس IP مشتری و رشته عامل کاربر را با هر درخواست تغییر دهد. 
اگر فعال باشد، مهمان‌های جدید در این شرایط مسدود می‌شوند.");

            #endregion

            builder.AddOrUpdate("Tax.LegalInfoShort3",
      "Prices {0}, {1}",
      "Preise {0}, {1}",
      "قیمت‌ها {0}، {1}");

            builder.AddOrUpdate("Smartstore.AI.Prompts.PleaseContinue",
                "Continue exactly at the marked point without repeating the previous text.",
                "Fahre genau an der markierten Stelle fort, ohne den bisherigen Text zu wiederholen.",
                "دقیقاً از نقطه علامت‌گذاری‌شده ادامه دهید بدون تکرار متن قبلی.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ContinueHere",
                "[Continue here]",
                "[Fortsetzung hier]",
                "[اینجا ادامه دهید]");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Suggestions.Separation",
                "Separate the suggestions with the ¶ character (paragraph mark).",
                "Trenne die Vorschläge durch das ¶ Zeichen (Absatzmarke).",
                "پیشنهادها را با کاراکتر ¶ (علامت پاراگراف) جدا کنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Suggestions.NoNumbering",
                "Do not use numbering.",
                "Verwende keine Nummerierungen.",
                "از شماره‌گذاری استفاده نکنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Suggestions.NoRepitions",
                "Each proposal must be unique - repetitions are not permitted.",
                "Jeder Vorschlag muss einzigartig sein – Wiederholungen sind nicht erlaubt.",
                "هر پیشنهاد باید منحصر به فرد باشد - تکرار مجاز نیست.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Suggestions.CharLimit",
                "Each suggestion should have a maximum of {0} characters.",
                "Jeder Vorschlag soll maximal {0} Zeichen haben.",
                "هر پیشنهاد باید حداکثر {0} کاراکتر داشته باشد.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.CharLimit",
                "The text can contain a maximum of {0} characters.",
                "Der Text darf maximal {0} Zeichen enthalten.",
                "متن می‌تواند حداکثر {0} کاراکتر داشته باشد.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.CharWordLimit",
                "The text may contain no more than {0} characters and no more than {1} words.",
                "Der Text darf nicht mehr als {0} Zeichen und nicht mehr als {1} Wörter enthalten.",
                "متن نباید بیش از {0} کاراکتر و بیش از {1} کلمه داشته باشد.");

            builder.AddOrUpdate("Admin.AI.Suggestions.DefaultPrompt",
                "Make {0} suggestions about the topic '{1}'.",
                "Mache {0} Vorschläge zum Thema '{1}'.",
                "در مورد موضوع '{1}' {0} پیشنهاد ارائه دهید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.DontUseQuotes",
                "Do not enclose the text in quotation marks or other characters.",
                "Schließe den Text nicht in Anführungszeichen oder andere Zeichen ein.",
                "متن را در گیومه یا کاراکترهای دیگر قرار ندهید.");

            builder.AddOrUpdate("Admin.Orders.CompleteUnpaidOrder",
                "The order has a payment status of <strong>{0}</strong>. Do you still want to set it to complete?",
                "Der Auftrag hat den Zahlungsstatus <strong>{0}</strong>. Möchten Sie ihn trotzdem auf komplett setzen?",
                "سفارش دارای وضعیت پرداخت <strong>{0}</strong> است. آیا همچنان می‌خواهید آن را به عنوان کامل تنظیم کنید؟");

            builder.AddOrUpdate("Products.Sorting.Featured")
                .Value("en", "Recommendation")
                .Value("de", "Empfehlung")
                .Value("fa", "توصیه");

            builder.AddOrUpdate("Admin.Configuration.Settings.Search.UseFeaturedSorting",
                "Sort by recommendation",
                "Nach Empfehlung sortieren",
                "مرتب‌سازی بر اساس توصیه",
                "Specifies whether sorting by recommendations is offered instead of sorting by best results. If activated, the products are sorted in the order specified for them.",
                "Legt fest, ob die Sortierung nach Empfehlungen anstelle der Sortierung nach besten Ergebnissen angeboten wird. Wenn aktiviert, werden die Produkte in der für sie angegebenen Reihenfolge sortiert.",
                "مشخص می‌کند که آیا مرتب‌سازی بر اساس توصیه‌ها به جای مرتب‌سازی بر اساس بهترین نتایج ارائه می‌شود. در صورت فعال شدن، محصولات به ترتیبی که برای آن‌ها مشخص شده مرتب می‌شوند.");

            builder.AddOrUpdate("Admin.Catalog.Products.Fields.DisplayOrder",
                "Display order",
                "Reihenfolge",
                "ترتیب نمایش",
                "Specifies the order in which associated products of a grouped product are displayed. In addition, this setting determines the order of hits in the search, if sort by recommendation is enabled in the search settings.",
                "Legt die Reihenfolge fest, in der verknüpfte Produkte eines Gruppenproduktes angezeigt werden. Zusätzlich legt diese Einstellung die Reihenfolge der Treffer bei der Suche fest, sofern in den Sucheinstellungen die Sortierung nach Empfehlung aktiviert ist.",
                "ترتیبی را که محصولات مرتبط یک محصول گروهی نمایش داده می‌شوند، مشخص می‌کند. علاوه بر این، این تنظیم ترتیب نتایج را در جستجو تعیین می‌کند، اگر مرتب‌سازی بر اساس توصیه در تنظیمات جستجو فعال شده باشد.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Shipping.FreeShippingCountryIds",
                "Countries with free shipping",
                "Länder mit kostenlosem Versand",
                "کشورهایی با ارسال رایگان",
                "Specifies the shipping countries for which free shipping is enabled. Free shipping is enabled for all countries if none are specified here (default).",
                "Legt die Lieferländer fest, für die der kostenlose Versand aktiviert ist. Wird hier kein Land angegeben, ist der kostenlose Versand für alle Länder aktiv (Standardeinstellung).",
                "کشورهای ارسالی که ارسال رایگان برای آن‌ها فعال است را مشخص می‌کند. اگر هیچ کشوری در اینجا مشخص نشود، ارسال رایگان برای همه کشورها فعال است (پیش‌فرض).");

            builder.AddOrUpdate("Admin.Plugins.KnownGroup.AI",
                "AI",
                "KI",
                "هوش مصنوعی");

            builder.AddOrUpdate("Admin.AI.TextCreation.Continue",
                "Continue",
                "Fortsetzen",
                "ادامه دهید");

            builder.AddOrUpdate("Smartstore.AI.Prompts.AppendToLastSpan",
                "Be sure to append the new mark-up to the last span tag.",
                "Füge das neue Mark-Up unbedingt an das letzte span-Tag an.",
                "مطمئن شوید که نشانه‌گذاری جدید را به آخرین تگ span اضافه می‌کنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.PreserveOriginalText",
                "Return the complete text in your answer.",
                "Gib in deiner Antwort den vollständigen Text zurück.",
                "متن کامل را در پاسخ خود بازگردانید.");

            builder.AddOrUpdate("Admin.AI.EditHtml",
                "Edit HTML text",
                "HTML-Text bearbeiten",
                "ویرایش متن HTML");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.Rules",
                "You must strictly follow these rules:",
                "Diese Regeln sind zwingend einzuhalten:",
                "شما باید این قوانین را به شدت رعایت کنید:");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.HtmlEditor",
       "You are an intelligent AI editor for web content. You combine the skills of a professional copywriter and technical HTML editor. Your output must ALWAYS be valid HTML!",
       "Du bist ein intelligenter KI-Editor für Webinhalte. Du kombinierst die Fähigkeiten eines professionellen Texters und technischen HTML-Editors. Deine Ausgabe ist IMMER valides HTML!",
       "شما یک ویرایشگر هوشمند هوش مصنوعی برای محتوای وب هستید. شما مهارت‌های یک نویسنده حرفه‌ای و ویرایشگر فنی HTML را ترکیب می‌کنید. خروجی شما باید همیشه HTML معتبر باشد!");

            builder.AddOrUpdate("Smartstore.AI.Prompts.CreateHtml",
                "Return only pure HTML code",
                "Gib ausschließlich reinen HTML-Code zurück",
                "فقط کد HTML خالص را بازگردانید");

            builder.AddOrUpdate("Smartstore.AI.Prompts.DontUseMarkdown",
                "Do not use Markdown formatting, no backticks (```) and no indented code sections.",
                "Verwende keine Markdown-Formatierung, keine Backticks (```) und keine eingerückten Codeabschnitte.",
                "از فرمت Markdown استفاده نکنید، بدون استفاده از بک‌تیک (```) و بخش‌های کد تورفتگی‌شده.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.CaretPos",
                "The placeholder [CARETPOS] marks the position where your new text should appear.",
                "Der Platzhalter [CARETPOS] markiert die Stelle, an der dein neuer Text erscheinen soll.",
                "جایگاه‌نما [CARETPOS] مکانی را مشخص می‌کند که متن جدید شما باید در آن ظاهر شود.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ReturnNewTextOnly",
                "Return ONLY the newly created text - no original parts.",
                "Gib AUSSCHLIESSLICH den neu erstellten Text zurück - keine Originalbestandteile.",
                "فقط متن تازه ایجادشده را بازگردانید - بدون بخش‌های اصلی.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.WrapNewContentWithHighlightTag",
                "Any text that you generate or add must be enclosed in a real HTML <mark> tag. Example: <mark>additional sentence</mark> or <li><mark>additional list item</mark></li>. " +
                "The word 'mark' must never appear as visible text content.",
                "Umschließe jeden neu generierten Text mit einem echten <mark>-Tag. Beispiel: <mark>Zusätzlicher Text</mark> oder <li><mark>Zusätzlicher Listeneintrag</mark></li>. " +
                "Das Wort 'mark' darf niemals als sichtbarer Textinhalt erscheinen.",
                "هر متنی که تولید یا اضافه می‌کنید باید در یک تگ HTML واقعی <mark> قرار گیرد. مثال: <mark>جمله اضافی</mark> یا <li><mark>مورد لیست اضافی</mark></li>. " +
                "کلمه 'mark' هرگز نباید به‌عنوان محتوای متنی قابل‌مشاهده ظاهر شود.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.MissingCaretPosHandling",
                "If the placeholder [CARETPOS] is not included in the HTML, insert the new text at the end of the document.",
                "Wenn der Platzhalter [CARETPOS] im HTML nicht enthalten ist, füge den neuen Text am Ende des Dokuments ein.",
                "اگر جایگاه‌نما [CARETPOS] در HTML وجود نداشته باشد، متن جدید را در انتهای سند درج کنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ValidHtmlOutput",
                "The generated output must be completely valid HTML that fits seamlessly into the existing HTML content.",
                "Die erzeugte Ausgabe muss vollständig valides HTML sein, das sich nahtlos in den bestehenden HTML-Inhalt einfügt.",
                "خروجی تولیدشده باید کاملاً HTML معتبر باشد که به‌صورت یکپارچه در محتوای HTML موجود جای گیرد.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ContinueAtPlaceholder",
                "Determine the next higher block-level element that contains the placeholder [CARETPOS] (e.g. <p> or <div>). " +
                "Consider this element as a valid context area for your text addition. " +
                "If the user instruction requires it, the addition can also be made outside the caret position within this element.",
                "Ermittle das nächsthöhere Block-Level-Element, das den Platzhalter [CARETPOS] enthält (z.B. <p> oder <div>). " +
                "Betrachte dieses Element als gültigen Kontextbereich für deine Textergänzung. " +
                "Wenn die Benutzeranweisung es verlangt, kann die Ergänzung auch außerhalb der CaretPos innerhalb dieses Elements erfolgen.",
                "عنصر سطح بلوک بالاتر بعدی که شامل جایگاه‌نما [CARETPOS] است را تعیین کنید (مثلاً <p> یا <div>). " +
                "این عنصر را به‌عنوان منطقه زمینه معتبر برای افزودن متن خود در نظر بگیرید. " +
                "اگر دستورالعمل کاربر ایجاب کند، افزودن می‌تواند خارج از موقعیت جایگاه‌نما در این عنصر نیز انجام شود.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.RemoveCaretPosPlaceholder",
                "Remove the placeholder [CARETPOS] completely. It must NEVER be included in the response - neither visibly nor as a control character.",
                "Entferne den Platzhalter [CARETPOS] vollständig. Er darf in der Antwort NIEMALS enthalten sein – weder sichtbar noch als Steuerzeichen.",
                "جایگاه‌نما [CARETPOS] را کاملاً حذف کنید. این جایگاه‌نما هرگز نباید در پاسخ گنجانده شود - نه به‌صورت قابل‌مشاهده و نه به‌عنوان کاراکتر کنترلی.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ReturnCompleteParentTag",
                "ALWAYS return the complete enclosing block-level parent element in which the new text was inserted or changed.",
                "Gib IMMER das vollständige umschließende Block-Level-Elternelement zurück, in dem der neue Text eingefügt oder verändert wurde.",
                "همیشه عنصر والد سطح بلوک کامل را که متن جدید در آن درج یا تغییر کرده است، بازگردانید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ReturnCompleteTable",
                "ALWAYS return the complete enclosing <table> tag in which the new text was inserted or changed.",
                "Gib IMMER das vollständige table-Tag zurück, in dem der neue Text eingefügt oder verändert wurde.",
                "همیشه تگ کامل <table> را که متن جدید در آن درج یا تغییر کرده است، بازگردانید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ReturnCompleteList",
                "ALWAYS return the complete tag of the list (<ul>, <ol> or <menu>) in which the new text was inserted or changed.",
                "Gib IMMER das vollständige Tag der Liste (<ul>, <ol> oder <menu>) zurück, in dem der neue Text eingefügt oder verändert wurde.",
                "همیشه تگ کامل لیست (<ul>، <ol> یا <menu>) را که متن جدید در آن درج یا تغییر کرده است، بازگردانید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ReturnCompleteDefinitionList",
                "ALWAYS return the complete enclosing <dl> tag in which the new text was inserted or changed.",
                "Gib IMMER das vollständige dl-Tag zurück, in dem der neue Text eingefügt oder verändert wurde.",
                "همیشه تگ کامل <dl> را که متن جدید در آن درج یا تغییر کرده است، بازگردانید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ReturnInstructionReinforcer",
                "ONLY return this one element - no other content before or after it.",
                "Gib NUR dieses eine Element zurück – keine anderen Inhalte davor oder danach.",
                "فقط این یک عنصر را بازگردانید - بدون محتوای دیگر قبل یا بعد از آن.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.PreservePreviousHighlightTags",
                "Any text deviation from the transmitted original text must be enclosed with the mark tag. " +
                "When you create a new answer, take into account the text you added previously. " +
                "Enclose ANY text you have added within this chat history with the mark tag.",
                "Jegliche Text-Abweichung vom übermittelten Originaltext muss mit dem mark-Tag umschloßen werden. " +
                "Wenn du eine neue Antwort erstellst, berücksichtige den Text, den du zuvor hinzugefügt hast. " +
                "Umschließe JEDEN Text, der von dir innerhalb dieses Chat-Verlaufes hinzugefügt wurde, mit dem mark-Tag.",
                "هر انحراف متنی از متن اصلی ارسالی باید با تگ mark محصور شود. " +
                "هنگام ایجاد پاسخ جدید، متنی را که قبلاً اضافه کرده‌اید در نظر بگیرید. " +
                "هر متنی که در این تاریخچه چت اضافه کرده‌اید را با تگ mark محصور کنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ContinueTable",
                "If the user requests a table extension, use [CARETPOS] exclusively to localize the table. " +
                "Expand the table logically without continuing directly at the caret position - unless the user explicitly requests that the current cell be edited.",
                "Wenn der User eine Tabellenerweiterung verlangt, verwende [CARETPOS] ausschließlich zur Lokalisierung der Tabelle. " +
                "Ergänze die Tabelle logisch, ohne direkt an der Caret-Position weiterzuschreiben – es sei denn, der User fordert ausdrücklich eine Bearbeitung der aktuellen Zelle.",
                "اگر کاربر درخواست گسترش جدول کند، از [CARETPOS] فقط برای مکان‌یابی جدول استفاده کنید. " +
                "جدول را به‌صورت منطقی گسترش دهید بدون اینکه مستقیماً در موقعیت جایگاه‌نما ادامه دهید - مگر اینکه کاربر صراحتاً درخواست ویرایش سلول فعلی را داشته باشد.");

            builder.AddOrUpdate("Admin.Catalog.Products.List.SearchWithOrders",
                "With order assignments",
                "Mit Auftragszuordnungen",
                "با تخصیص‌های سفارش",
                "Filters for products with/without order assignments.",
                "Filtert nach Produkten mit/ohne Auftragszuordnungen.",
                "فیلتر برای محصولات با/بدون تخصیص‌های سفارش.");

            builder.AddOrUpdate("Admin.Customers.CookieConsent",
                "Cookie consent",
                "Cookie-Zustimmung",
                "رضایت کوکی");

            builder.AddOrUpdate("Admin.Customers.CookieConsent.ConsentOn",
                "Cookie consent on",
                "Cookie-Zustimmung am",
                "رضایت کوکی در",
                "The date of the customer's consent to the use of cookies.",
                "Das Datum, an dem der Kunde der Verwendung von Cookies zugestimmt hat.",
                "تاریخ رضایت مشتری برای استفاده از کوکی‌ها.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.NoFriendlyIntroductions",
                "Don't start your answer with meta-comments or introductions like: 'Gladly, here's your HTML'.",
                "Erstelle keine Meta-Kommentare oder Einleitungen wie: 'Gerne, hier ist dein HTML.'",
                "پاسخ خود را با نظرات متا یا مقدمه‌هایی مانند: 'با خوشحالی، این HTML شماست' شروع نکنید.");
            // Changed for more precision.
            builder.AddOrUpdate("Smartstore.AI.Prompts.StartWithDivTag",
      "Do not create a complete HTML document - the output must start with a <div> tag.",
      "Erstelle kein vollständiges HTML-Dokument - die Ausgabe muss mit einem <div>-Tag beginnen.",
      "یک سند HTML کامل ایجاد نکنید - خروجی باید با یک تگ <div> شروع شود.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.DontCreateProductTitle",
                "Do not create a heading that contains the product name.",
                "Erstelle keine Überschrift, die den Produktnamen enthält.",
                "عنوان شامل نام محصول ایجاد نکنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.DontCreateTitle",
                "Do not create the title: '{0}'.",
                "Erstelle nicht den Titel: '{0}'.",
                "عنوان: '{0}' را ایجاد نکنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.WriteCompleteParagraphs",
                "Create a complete and coherent text for each section.",
                "Erstelle für jeden Abschnitt einen inhaltlich vollständigen und zusammenhängenden Text.",
                "برای هر بخش یک متن کامل و منسجم ایجاد کنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.UseImagePlaceholders",
                "If an image is to be inserted, use a <div class=\"mb-3\"> with an <i> tag containing the classes 'far fa-xl fa-file-image ai-preview-file' as a placeholder. " +
                "The title attribute must correspond to the associated section heading.",
                "Wenn ein Bild einzufügen ist, verwende als Platzhalter ein <div class=\"mb-3\"> mit einem <i>-Tag, " +
                "das die Klassen 'far fa-xl fa-file-image ai-preview-file' enthält. Das title-Attribut muss der zugehörigen Abschnittsüberschrift entsprechen.",
                "اگر قرار است تصویری درج شود، از یک <div class=\"mb-3\"> با یک تگ <i> که شامل کلاس‌های 'far fa-xl fa-file-image ai-preview-file' است به‌عنوان جایگاه‌نما استفاده کنید. " +
                "ویژگی title باید با عنوان بخش مرتبط مطابقت داشته باشد.");
            // INFO: Minor change from "Be a ..." to "You are a ...". Seams irrelevant for a human reader, but it's important for the role understanding of the AI.
            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.Translator",
         "Be a professional translator.",
         "Du bist ein professioneller Übersetzer.",
         "مترجم حرفه‌ای باشید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.Copywriter",
                "Be a professional copywriter.",
                "Du bist ein professioneller Texter.",
                "نویسنده حرفه‌ای باشید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.Marketer",
                "Be a marketing expert.",
                "Du bist ein Marketing-Experte.",
                "کارشناس بازاریابی باشید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.SEOExpert",
                "Be a SEO expert.",
                "Du bist ein SEO-Experte.",
                "کارشناس سئو باشید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.Blogger",
                "Be a professional blogger.",
                "Du bist ein professioneller Blogger.",
                "بلاگر حرفه‌ای باشید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.Journalist",
                "Be a professional journalist.",
                "Du bist ein professioneller Journalist.",
                "روزنامه‌نگار حرفه‌ای باشید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.SalesPerson",
                "Be an assistant who creates product descriptions that convince a potential customer to buy.",
                "Du bist ein Assistent bei der Erstellung von Produktbeschreibungen, die einen potentiellen Kunden zum Kauf überzeugen.",
                "دستیاری باشید که توضیحات محصولی ایجاد می‌کند که مشتری بالقوه را به خرید متقاعد کند.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.ProductExpert",
                "Be an expert for the product: '{0}'.",
                "Du bist ein Experte für das Produkt: '{0}'.",
                "کارشناس محصول '{0}' باشید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Role.ImageAnalyzer",
                "Be an image analyzer assistant.",
                "Du bist ein Assistent für Bildanalyse.",
                "دستیار تحلیلگر تصویر باشید.");
            // We change this resource to be a pure user message her. The main instruction of this order was shifted to the role description.
            // INFO: Not only is this the preferred method for engineering prompts, but it also reduces the custom prompt message to a minimum.
            builder.AddOrUpdate("Smartstore.AI.Prompts.IncludeImages",
     "Insert an image after each paragraph.",
     "Füge nach jedem Absatz ein Bild ein.",
     "بعد از هر پاراگراف یک تصویر درج کنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Language",
                "Write in {0}.",
                "Schreibe auf {0}.",
                "به زبان {0} بنویسید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.MainHeadingTag",
                "Use a {0} tag for the main heading.",
                "Nutze für die Hauptüberschrift ein {0}-Tag.",
                "برای عنوان اصلی از تگ {0} استفاده کنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ParagraphHeadingTag",
                "Use {0} tags for the paragraph headings.",
                "Nutze für die Überschriften der Abschnitte {0}-Tags.",
                "برای عناوین پاراگراف‌ها از تگ‌های {0} استفاده کنید.");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.XmlSitemapIncludesAlternateLinks",
                "Add alternate links for localized pages",
                "Alternate Links für lokalisierte Seiten hinzufügen",
                "لینک‌های جایگزین برای صفحات محلی‌سازی‌شده اضافه کنید",
                "Specifies whether to add alternate links (xhtml:link) for localized page versions to the XML Sitemap.",
                "Legt fest, ob Alternate Links (xhtml:link) für lokalisierte Seitenversionen in der XML-Sitemap hinzugefügt werden sollen.",
                "مشخص می‌کند که آیا لینک‌های جایگزین (xhtml:link) برای نسخه‌های محلی‌سازی‌شده صفحات به نقشه سایت XML اضافه شوند.");

            builder.AddOrUpdate("Admin.Configuration.Settings.GeneralCommon.AddAlternateHtmlLinks",
                "Add alternate links for localized pages",
                "Alternate Links für lokalisierte Seiten hinzufügen",
                "لینک‌های جایگزین برای صفحات محلی‌سازی‌شده اضافه کنید",
                "Specifies whether to add alternate links (link rel='alternate') for localized page versions in the HTML header.",
                "Legt fest, ob Alternate Links (link rel='alternate') für lokalisierte Seitenversionen in den HTML-Header eingefügt werden sollen.",
                "مشخص می‌کند که آیا لینک‌های جایگزین (link rel='alternate') برای نسخه‌های محلی‌سازی‌شده صفحات در هدر HTML درج شوند.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ImageAnalyzer.ObjectDefinition",
                "Return exactly one single JSON object with these keys and meanings:",
                "Gib genau ein einzelnes JSON-Objekt mit diesen Keys und Bedeutungen zurück:",
                "دقیقاً یک شیء JSON واحد با این کلیدها و معانی بازگردانید:");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ImageAnalyzer.ObjectDefinition.Title",
                "'title': short, precise description of the image content for the HTML title attribute (max. 60 characters)",
                "'title': kurze, präzise Beschreibung des Bildinhalts für das HTML-title-Attribut (max. 60 characters)",
                "'title': توضیح کوتاه و دقیق محتوای تصویر برای ویژگی title در HTML (حداکثر 60 کاراکتر)");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ImageAnalyzer.ObjectDefinition.Alt",
                "'alt': clearly legible description of the image content for the HTML alt attribute",
                "'alt': klar lesbare Beschreibung des Bildinhalts für das HTML-alt-Attribut",
                "'alt': توضیح واضح و خوانا از محتوای تصویر برای ویژگی alt در HTML");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ImageAnalyzer.ObjectDefinition.Tags",
                "'tags': exactly 5 thematically matching terms, as a comma-separated list",
                "'tags': exakt 5 thematisch passende Begriffe, als kommagetrennte Liste",
                "'tags': دقیقاً 5 اصطلاح مرتبط با موضوع، به‌صورت لیست جدا شده با کاما");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ImageAnalyzer.NoContent",
                "Set the value 'no-content' in every field for which no meaningful content can be determined.",
                "Setze den Wert 'no-content' in jedem Feld, für das kein sinnvoller Inhalt ermittelt werden kann.",
                "مقدار 'no-content' را برای هر فیلدی که محتوای معناداری نمی‌توان تعیین کرد، تنظیم کنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.CreateJson",
                "Only return a single JSON object - without formatting, meta comments or additional text.",
                "Gib ausschließlich ein einziges JSON-Objekt zurück – ohne Formatierungen, Meta-Kommentare oder zusätzlichen Text.",
                "فقط یک شیء JSON واحد بازگردانید - بدون قالب‌بندی، نظرات متا یا متن اضافی.");

            builder.Delete("Smartstore.AI.Prompts.JustHtml");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Translator.TranslateTextContentOnly",
                "Translate text content between HTML tags as well as plain text without HTML structure.",
                "Übersetze Textinhalte zwischen HTML-Tags sowie reinen Fließtext ohne HTML-Struktur.",
                "فقط محتوای متنی بین تگ‌های HTML و متن ساده بدون ساختار HTML را ترجمه کنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Translator.PreserveHtmlStructure",
                "Do not alter any HTML tags. Do not add, remove, or restructure tags in any way.",
                "Verändere keine HTML-Tags. Füge keine Tags hinzu, entferne keine und ändere keine Verschachtelung.",
                "تگ‌های HTML را تغییر ندهید. هیچ تگی اضافه، حذف یا بازسازی نکنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Translator.IgnoreTechnicalAttributes",
                "Do not translate attribute values that serve technical purposes, such as href, src, id, class, style, or data-*.",
                "Übersetze keine Attributwerte, die technische Funktionen erfüllen, z.B.: 'href, src, id, class, style, data-*'.",
                "مقادیر ویژگی‌هایی که اهداف فنی دارند، مانند href، src، id، class، style یا data-* را ترجمه نکنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Translator.KeepHtmlEntitiesIntact",
                "Do not modify HTML entities (e.g.,  , ©, –) in meaning or form.",
                "Verändere HTML ACHTUNGEN (z.B.  , ©, –) nicht – weder semantisch noch formal.",
                "موجودیت‌های HTML (مانند  ، ©، –) را از نظر معنا یا فرم تغییر ندهید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Translator.TranslateWithContext",
                "When translating text within inline tags (e.g., <strong>, <em>, <span>, <a>, …), always preserve the full sentence context.",
                "Berücksichtige beim Übersetzen von Textteilen in Inline-Tags (z.B. <strong>, <em>, <span>, <a>, …) immer den vollständigen Satzkontext.",
                "هنگام ترجمه متن در تگ‌های درون‌خطی (مانند <strong>، <em>، <span>، <a>، …)، همیشه زمینه کامل جمله را حفظ کنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Translator.TranslateDescriptiveAttributes",
                "Translate attribute values that convey information to the reader, such as alt and title.",
                "Übersetze Attributwerte, die dem Leser Informationen vermitteln, z.B. alt und title.",
                "مقادیر ویژگی‌هایی که به خواننده اطلاعات می‌دهند، مانند alt و title را ترجمه کنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Translator.PreserveToneAndStyle",
                "Preserve the tone and style of the original text. Do not simplify, paraphrase, or smooth the language.",
                "Behalte den Tonfall und Stil des Ausgangstexts bei. Verwende keine stilistischen Glättungen, Umschreibungen oder Vereinfachungen.",
                "لحن و سبک متن اصلی را حفظ کنید. زبان را ساده‌سازی، بازنویسی یا روان نکنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Translator.SkipAlreadyTranslated",
                "If the text is already in the target language, return it unchanged.",
                "Wenn der Text bereits in der Zielsprache vorliegt, gib ihn unverändert zurück.",
                "اگر متن از قبل به زبان مقصد باشد، آن را بدون تغییر بازگردانید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Translator.NoMetaComments",
                "Do not add explanations or meta comments (e.g., 'The text is already in English.').",
                "Füge keine Erklärungen oder Meta-Kommentare hinzu (z.B. 'Der Text ist schon Englisch.').",
                "توضیحات یا نظرات متا اضافه نکنید (مثلاً 'متن از قبل به انگلیسی است.').");

            builder.AddOrUpdate("Smartstore.AI.Prompts.Product.NoAssumptions",
                "Only describe what is clearly known about the product. Do not make any assumptions about the product.",
                "Beschreibe nur, was von dem Produkt eindeutig bekannt ist. Stelle keine Vermutungen über das Produkt an.",
                "فقط آنچه که به‌طور واضح درباره محصول شناخته شده است را توصیف کنید. هیچ فرضی درباره محصول نکنید.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.OneParagraph",
                "The text contains only one section, which is enclosed by a p-tag.",
                "Der Text beinhaltet nur einen Abschnitt, der von einem p-Tag umschlossen ist.",
                "متن فقط شامل یک بخش است که توسط یک تگ p محصور شده است.");

            builder.AddOrUpdate("Admin.Customers.DeleteCustomer",
                "Delete customer",
                "Kunde löschen",
                "حذف مشتری");
            builder.Delete("Account.PasswordRecovery.EmailNotFound");

            builder.AddOrUpdate("Account.PasswordRecovery.EmailHasBeenSent",
      "We have sent you an email with further instructions if an account exists with your email address.",
      "Wir haben Ihnen eine E-Mail mit weiteren Anweisungen geschickt, falls ein Konto mit Ihrer E-Mail-Adresse existiert.",
      "اگر حسابی با آدرس ایمیل شما وجود داشته باشد، ایمیلی با دستورالعمل‌های بیشتر برای شما ارسال کرده‌ایم.");

            builder.AddOrUpdate("Admin.DataExchange.Import.UpdateAllKeyFieldMatches",
                "Update all that match a key field value",
                "Alle aktualisieren, die dem Wert eines Schlüsselfelds entsprechen",
                "همه مواردی که با مقدار یک فیلد کلیدی مطابقت دارند را به‌روزرسانی کنید",
                "Specifies that all records matching the value of a key field are updated. By default, only the first matching record is updated." +
                " Enable this option if, for example, you have assigned an MPN multiple times and want to update all products with that MPN in a consistent manner." +
                " Enabling this option may reduce the performance of the import.",
                "Legt fest, dass alle mit dem Wert eines Schlüsselfeldes übereinstimmenden Datensätze aktualisiert werden. Standardmäßig wird nur der erste übereinstimmende" +
                " Datensatz aktualisiert. Aktivieren Sie diese Option, wenn Sie z.B. eine MPN mehrfach vergeben haben und alle Produkte mit dieser MPN einheitlich aktualisieren möchten." +
                " Die Aktivierung dieser Option kann die Performance des Imports beeinträchtigen.",
                "مشخص می‌کند که همه رکوردها که با مقدار یک فیلد کلیدی مطابقت دارند به‌روزرسانی شوند. به‌طور پیش‌فرض، فقط اولین رکورد منطبق به‌روزرسانی می‌شود." +
                " این گزینه را فعال کنید اگر، به‌عنوان مثال، یک MPN را چندین بار تخصیص داده‌اید و می‌خواهید همه محصولات با آن MPN را به‌صورت یکنواخت به‌روزرسانی کنید." +
                " فعال کردن این گزینه ممکن است عملکرد فرآیند وارد کردن را کاهش دهد.");

            // Fix:
            builder.AddOrUpdate("Admin.DataExchange.Import.KeyFieldNames.Note")
                .Value("de", "Bitte verwenden Sie das Feld ID nur dann als Schlüsselfeld, wenn die Daten aus derselben Datenbank stammen, in die sie importiert werden sollen. Andernfalls können falsche Datensätze aktualisiert werden.")
                .Value("fa", "لطفاً از فیلد ID فقط زمانی به‌عنوان فیلد کلیدی استفاده کنید که داده‌ها از همان پایگاه داده‌ای باشند که قرار است به آن وارد شوند. در غیر این صورت، ممکن است رکوردهای نادرست به‌روزرسانی شوند.");

            builder.AddOrUpdate("Smartstore.AI.Prompts.ReserveSpaceForShopName",
                "When creating text for title tags, do not use the name of the website, as this will be added later - Reserve {0} characters for this.",
                "Verwende bei der Erstellung von Texten für title-Tags nicht den Namen der Website, da dieser später hinzugefügt wird - Reserviere dafür {0} Zeichen.",
                "هنگام ایجاد متن برای تگ‌های title، از نام وب‌سایت استفاده نکنید، زیرا بعداً اضافه خواهد شد - {0} کاراکتر برای این منظور رزرو کنید.");
        }
    }
}

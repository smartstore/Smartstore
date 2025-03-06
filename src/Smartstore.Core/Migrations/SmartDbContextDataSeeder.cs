using Org.BouncyCastle.Utilities;
using Smartstore.Data.Migrations;
using static Smartstore.Core.Security.Permissions;

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


            builder.AddOrUpdate("Admin.Common.RestartHint",
      "Changes to the following settings only take effect after the application has been restarted.",
      "Änderungen an den folgenden Einstellungen werden erst nach einem Neustart der Anwendung wirksam.",
      "تغییرات در تنظیمات زیر تنها پس از راه‌اندازی مجدد برنامه اعمال می‌شوند.");

            #region Performance settings
            var prefix = "Admin.Configuration.Settings.Performance";

            builder.AddOrUpdate($"{prefix}", "Performance", "Leistung", "عملکرد");
            builder.AddOrUpdate($"{prefix}.Resiliency", "Overload protection", "Überlastungsschutz", "حفاظت در برابر اضافه‌بار");
            builder.AddOrUpdate($"{prefix}.Cache", "Cache", "Cache", "حافظه نهان");

            builder.AddOrUpdate($"{prefix}.Hint",
                "For technically experienced users only. Pay attention to the CPU and memory usage when changing these settings.",
                "Nur für technisch erfahrene Benutzer. Achten Sie auf die CPU- und Speicherauslastung, wenn Sie diese Einstellungen ändern.",
                "فقط برای کاربران با تجربه فنی. هنگام تغییر این تنظیمات به مصرف CPU و حافظه توجه کنید.");

            builder.AddOrUpdate($"{prefix}.CacheSegmentSize",
                "Cache segment size",
                "Cache Segment Größe",
                "اندازه بخش حافظه نهان",
                "The number of entries in a single cache segment when greedy loading is disabled. The larger the catalog, the smaller this value should be. We recommend segment size of 500 for catalogs with less than 100.000 items.",
                "Die Anzahl der Einträge in einem einzelnen Cache-Segment, wenn Greedy Loading deaktiviert ist. Je größer der Katalog ist, desto kleiner sollte dieser Wert sein. Wir empfehlen eine Segmentgröße von 500 für Kataloge mit weniger als 100.000 Einträgen.",
                "تعداد ورودی‌ها در یک بخش حافظه نهان زمانی که بارگذاری حریصانه غیرفعال است. هرچه کاتالوگ بزرگ‌تر باشد، این مقدار باید کوچک‌تر باشد. ما اندازه بخش ۵۰۰ را برای کاتالوگ‌هایی با کمتر از ۱۰۰,۰۰۰ مورد توصیه می‌کنیم.");

            builder.AddOrUpdate($"{prefix}.AlwaysPrefetchTranslations",
                "Always prefetch translations",
                "Übersetzungen immer vorladen (Prefetch)",
                "همیشه ترجمه‌ها را پیش‌بارگذاری کن",
                "By default, only Instant Search prefetches translations. All other product listings work against the segmented cache. For very large multilingual catalogs (> 500,000), enabling this can improve query performance and reduce resource usage.",
                "Standardmäßig werden nur bei der Sofortsuche Übersetzungen vorgeladen. Alle anderen Produktauflistungen arbeiten mit dem segmentierten Cache. Bei sehr großen mehrsprachigen Katalogen (> 500.000) kann die Aktivierung dieser Option die Abfrageleistung verbessern und die Ressourcennutzung verringern.",
                "به طور پیش‌فرض، فقط جستجوی فوری ترجمه‌ها را پیش‌بارگذاری می‌کند. سایر فهرست‌های محصولات با حافظه نهان بخش‌بندی‌شده کار می‌کنند. برای کاتالوگ‌های چندزبانه بسیار بزرگ (> ۵۰۰,۰۰۰)، فعال کردن این گزینه می‌تواند عملکرد پرس‌وجو را بهبود داده و مصرف منابع را کاهش دهد.");

            builder.AddOrUpdate($"{prefix}.AlwaysPrefetchUrlSlugs",
                "Always prefetch URL slugs",
                "URL Slugs immer vorladen  (Prefetch)",
                "همیشه اسلاگ‌های URL را پیش‌بارگذاری کن",
                "By default, only Instant Search prefetches URL slugs. All other product listings work against the segmented cache. For very large multilingual catalogs (> 500,000), enabling this can improve query performance and reduce resource usage.",
                "Standardmäßig werden nur bei der Sofortsuche URL slugs vorgeladen. Alle anderen Produktauflistungen arbeiten mit dem segmentierten Cache. Bei sehr großen mehrsprachigen Katalogen (> 500.000) kann die Aktivierung dieser Option die Abfrageleistung verbessern und die Ressourcennutzung verringern.",
                "به طور پیش‌فرض، فقط جستجوی فوری اسلاگ‌های URL را پیش‌بارگذاری می‌کند. سایر فهرست‌های محصولات با حافظه نهان بخش‌بندی‌شده کار می‌کنند. برای کاتالوگ‌های چندزبانه بسیار بزرگ (> ۵۰۰,۰۰۰)، فعال کردن این گزینه می‌تواند عملکرد پرس‌وجو را بهبود داده و مصرف منابع را کاهش دهد.");

            builder.AddOrUpdate($"{prefix}.MaxUnavailableAttributeCombinations",
                "Max. unavailable attribute combinations",
                "Max. nicht verfügbare Attributkombinationen",
                "حداکثر ترکیب‌های ویژگی‌های ناموجود",
                "Maximum number of attribute combinations that will be loaded and parsed to make them unavailable for selection on the product detail page.",
                "Maximale Anzahl von Attributkombinationen, die geladen und analysiert werden, um nicht verfügbare Kombinationen zu ermitteln.",
                "حداکثر تعداد ترکیب‌های ویژگی‌هایی که بارگذاری و تجزیه می‌شوند تا در صفحه جزئیات محصول برای انتخاب غیرقابل دسترس شوند.");

            builder.AddOrUpdate($"{prefix}.MediaDupeDetectorMaxCacheSize",
                "Media Duplicate Detector max. cache size",
                "Max. Cache-Größe für Medien-Duplikat-Detektor",
                "حداکثر اندازه حافظه نهان تشخیص‌دهنده تکراری رسانه",
                "Maximum number of MediaFile entities to cache for duplicate file detection. If a media folder contains more files, no caching is done for scalability reasons and the MediaFile entities are loaded directly from the database.",
                "Maximale Anzahl der MediaFile-Entitäten, die für die Duplikat-Erkennung zwischengespeichert werden. Enthält ein Medienordner mehr Dateien, erfolgt aus Gründen der Skalierbarkeit keine Zwischenspeicherung und die MediaFile-Entitäten werden direkt aus der Datenbank geladen.",
                "حداکثر تعداد موجودیت‌های MediaFile که برای تشخیص فایل‌های تکراری در حافظه نهان ذخیره می‌شوند. اگر پوشه رسانه حاوی فایل‌های بیشتری باشد، به دلایل مقیاس‌پذیری، ذخیره‌سازی در حافظه نهان انجام نمی‌شود و موجودیت‌های MediaFile مستقیماً از پایگاه داده بارگذاری می‌شوند.");

            prefix = "Admin.Configuration.Settings.Resiliency";

            builder.AddOrUpdate($"{prefix}.Description",
                @"Overload protection is used to keep server resources under control, prevent latencies from getting out of hand and keep the system performant and available 
in the event of increased traffic (e.g. due to unidentifiable ""Bad Bots"", peaks, sales events, sudden high visitor numbers).
Limits only apply to guest accounts and bots, not to registered users.",
                @"Überlastungsschutz dient dazu, die Server-Ressourcen unter Kontrolle zu halten, Latenzen nicht ausufern zu lassen und im Falle von erhöhtem Ansturm 
(z.B. durch nicht identifizierbare ""Bad-Bots"", Peaks, Sales Events, plötzlich hohe Nutzerzahlen) das System performant und verfügbar zu halten.
Limits gelten nur für Gastkonten und Bots, nicht für registrierte User.",
                @"حفاظت در برابر اضافه‌بار برای کنترل منابع سرور، جلوگیری از افزایش بیش‌ازحد تأخیرها و حفظ عملکرد و در دسترس بودن سیستم در زمان افزایش ترافیک (مثلاً به دلیل ""ربات‌های بد"" ناشناخته، اوج‌ها، رویدادهای فروش، یا تعداد ناگهانی بالای بازدیدکنندگان) استفاده می‌شود.
محدودیت‌ها فقط برای حساب‌های مهمان و ربات‌ها اعمال می‌شود، نه برای کاربران ثبت‌شده.");

            builder.AddOrUpdate($"{prefix}.LongTraffic", "Traffic limit", "Besucherlimit", "محدودیت ترافیک");
            builder.AddOrUpdate($"{prefix}.LongTrafficNotes",
                "Configuration of the long traffic window. Use these settings to monitor traffic restrictions over a longer period of time, such as a minute or longer.",
                "Konfiguration des langen Zeitfensters. Verwenden Sie diese Einstellungen, um Limits über einen längeren Zeitraum zu überwachen, z.B. eine Minute oder länger.",
                "پیکربندی پنجره ترافیک طولانی. از این تنظیمات برای نظارت بر محدودیت‌های ترافیک در یک بازه زمانی طولانی‌تر، مانند یک دقیقه یا بیشتر، استفاده کنید.");

            builder.AddOrUpdate($"{prefix}.PeakTraffic", "Peak", "Lastspitzen", "اوج ترافیک");
            builder.AddOrUpdate($"{prefix}.PeakTrafficNotes",
                "The peak traffic window defines the shorter period used for detecting sudden traffic spikes. These settings are useful for reacting to bursts of traffic that occur in a matter of seconds.",
                "Den kürzere Zeitraum, der für die Erkennung plötzlicher Lastspitzen verwendet wird. Diese Einstellungen sind nützlich, um auf Lastspitzen zu reagieren, die innerhalb weniger Sekunden auftreten.",
                "پنجره اوج ترافیک، بازه کوتاه‌تری را تعریف می‌کند که برای تشخیص جهش‌های ناگهانی ترافیک استفاده می‌شود. این تنظیمات برای واکنش به انفجارهای ترافیکی که در چند ثانیه رخ می‌دهند مفید هستند.");

            builder.AddOrUpdate($"{prefix}.TrafficTimeWindow",
                "Time window",
                "Zeitfenster",
                "پنجره زمانی",
                "The duration of the traffic window, which defines the period of time during which sustained traffic is measured.",
                "Die Dauer des Zeitfensters, das den Zeitraum definiert, in dem der anhaltende Traffic gemessen wird.",
                "مدت زمان پنجره ترافیک که دوره‌ای را تعریف می‌کند که در آن ترافیک پایدار اندازه‌گیری می‌شود.");

            builder.AddOrUpdate($"{prefix}.TrafficLimitGuest",
                "Guest limit",
                "Gäste-Grenzwert",
                "محدودیت مهمان",
                "The maximum number of requests allowed from guest users within the duration of the defined time window. Empty value means there is no limit applied for guest users.",
                "Die maximale Anzahl von Gastbenutzern innerhalb des festgelegten Zeitfensters. Ein leerer Wert bedeutet: keine Begrenzung.",
                "حداکثر تعداد درخواست‌های مجاز از کاربران مهمان در مدت زمان پنجره تعریف‌شده. مقدار خالی به این معناست که هیچ محدودیتی برای کاربران مهمان اعمال نمی‌شود.");

            builder.AddOrUpdate($"{prefix}.TrafficLimitBot",
                "Bot limit",
                "Bot-Grenzwert",
                "محدودیت ربات",
                "The maximum number of requests allowed from bots within the duration of the defined time window. Empty value means there is no limit applied for bots.",
                "Die maximale Anzahl von Bots innerhalb des festgelegten Zeitfensters. Ein leerer Wert bedeutet: keine Begrenzung.",
                "حداکثر تعداد درخواست‌های مجاز از ربات‌ها در مدت زمان پنجره تعریف‌شده. مقدار خالی به این معناست که هیچ محدودیتی برای ربات‌ها اعمال نمی‌شود.");

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
          @"محدودیت جهانی ترافیک برای مهمان‌ها و ربات‌ها به صورت ترکیبی. این محدودیت به ترافیک ترکیبی از مهمان‌ها و ربات‌ها اعمال می‌شود. 
این اطمینان را می‌دهد که بار کلی سیستم در محدوده‌های قابل قبول باقی بماند، صرف‌نظر از توزیع درخواست‌ها بین انواع خاص کاربران. 
برخلاف محدودکننده‌های مهمان یا ربات، این محدودیت جهانی به عنوان محافظ کل سیستم عمل می‌کند. اگر مجموع درخواست‌ها از هر دو نوع این محدودیت را در پنجره مشاهده‌شده превыابد، 
درخواست‌های اضافی ممکن است رد شوند، حتی اگر محدودیت‌های خاص نوع کاربر هنوز به حد نرسیده باشند. مقدار خالی به این معناست که هیچ محدودیت جهانی وجود ندارد.");

            builder.AddOrUpdate($"{prefix}.EnableOverloadProtection",
                "Enable overload protection",
                "Überlastungsschutz aktivieren",
                "فعال کردن حفاظت در برابر اضافه‌بار",
                "When enabled, the system applies the defined traffic limits and overload protection policies. If disabled, no traffic limits are enforced.",
                "Wendet die festgelegten Richtlinien an. Wenn diese Option deaktiviert ist, werden keine Begrenzungen erzwungen.",
                "هنگامی که فعال باشد، سیستم محدودیت‌های ترافیک تعریف‌شده و سیاست‌های حفاظت در برابر اضافه‌بار را اعمال می‌کند. اگر غیرفعال باشد، هیچ محدودیتی اعمال نمی‌شود.");

            builder.AddOrUpdate($"{prefix}.ForbidNewGuestsIfSubRequest",
                "If sub request, forbid \"new\" guests",
                "Wenn Sub-Request, \"neue\" Gäste blockieren",
                "در صورت درخواست فرعی، مهمان‌های \"جدید\" را ممنوع کن",
                @"Forbids ""new"" guest users if the request is a sub/secondary request, e.g., an AJAX request, POST, script, media file, etc. This setting can be used to restrict 
the creation of new guest sessions on successive (secondary) resource requests. A ""bad bot"" that does not accept cookies is difficult to identify 
as a bot and may create a new guest session with each (sub)-request, especially if it varies its client IP address and user agent string with each request. 
If enabled, new guests will be blocked under these circumstances.",
                @"Blockiert ""neue"" Gastbenutzer, wenn es sich bei der Anforderung um eine untergeordnete/sekundäre Anforderung handelt, z. B. AJAX, POST, Skript, Mediendatei usw. 
Diese Einstellung kann verwendet werden, um die Erstellung neuer Gastsitzungen bei aufeinander folgenden (sekundären) Ressourcenanfragen einzuschränken. 
Ein ""Bad Bot"", der keine Cookies akzeptiert, ist schwer als Bot zu erkennen und kann bei jeder (Unter-)Anfrage eine neue Gastsitzung erzeugen, 
insbesondere wenn er seine Client-IP-Adresse und den User-Agent-String bei jeder Anfrage ändert. 
Wenn diese Option aktiviert ist, werden neue Gäste unter diesen Umständen blockiert.",
                @"مهمان‌های ""جدید"" را در صورتی که درخواست یک درخواست فرعی/ثانویه باشد (مثلاً درخواست AJAX، POST، اسکریپت، فایل رسانه‌ای و غیره) ممنوع می‌کند. 
این تنظیم می‌تواند برای محدود کردن ایجاد جلسات مهمان جدید در درخواست‌های متوالی (ثانویه) منابع استفاده شود. یک ""ربات بد"" که کوکی‌ها را قبول نمی‌کند، 
به سختی به عنوان ربات قابل شناسایی است و ممکن است با هر درخواست (فرعی) یک جلسه مهمان جدید ایجاد کند، به‌خصوص اگر آدرس IP مشتری و رشته عامل کاربر را با هر درخواست تغییر دهد. 
اگر این گزینه فعال باشد، مهمان‌های جدید در این شرایط مسدود خواهند شد.");

            #endregion

            builder.AddOrUpdate("Tax.LegalInfoShort3",
                "Prices {0}, {1}",
                "Preise {0}, {1}",
                "قیمت‌ها {0}, {1}");

            builder.AddOrUpdate("Admin.Configuration.Settings.Shipping.CalculateShippingAtCheckout",
                "Calculate shipping costs during checkout",
                "Versandkosten während des Checkouts berechnen",
                "محاسبه هزینه‌های حمل‌ونقل در هنگام پرداخت",
                "Specifies whether shipping costs are displayed on the shopping cart page as long as the customer has not yet entered a shipping address. If activated, a note appears instead that the calculation will only take place at checkout.",
                "Legt fest, ob Versandkosten auf der Warenkorbseite angezeigt werden, solange der Kunde noch keine Lieferanschrift eingegeben hat. Wenn aktiviert, erscheint stattdessen ein Hinweis, dass die Berechnung erst beim Checkout erfolgt.",
                "مشخص می‌کند که آیا هزینه‌های حمل‌ونقل در صفحه سبد خرید نمایش داده شوند، در حالی که مشتری هنوز آدرس حمل‌ونقل را وارد نکرده است. اگر فعال شود، به جای آن یادداشتی ظاهر می‌شود که محاسبه فقط در زمان پرداخت انجام خواهد شد.");
        }
    }
}
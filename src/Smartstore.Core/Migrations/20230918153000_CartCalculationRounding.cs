using FluentMigrator;
using Smartstore.Core.Common;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2023-09-18 15:30:00", "Core: shopping cart calculation rounding")]
    internal class CartCalculationRounding : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string CurrencyTable = nameof(Currency);
        const string MidpointRoundingColumn = nameof(Currency.MidpointRounding);
        const string RoundOrderItemsEnabledColumn = nameof(Currency.RoundOrderItemsEnabled);
        const string RoundNetPricesColumn = nameof(Currency.RoundNetPrices);
        const string RoundUnitPricesColumn = nameof(Currency.RoundUnitPrices);

        public override void Up()
        {
            // Make nullable.
            Alter.Column(RoundOrderItemsEnabledColumn).OnTable(CurrencyTable).AsBoolean().Nullable();

            if (!Schema.Table(CurrencyTable).Column(MidpointRoundingColumn).Exists())
            {
                Create.Column(MidpointRoundingColumn).OnTable(CurrencyTable).AsInt32().NotNullable().WithDefaultValue((int)CurrencyMidpointRounding.AwayFromZero);
            }

            if (!Schema.Table(CurrencyTable).Column(RoundNetPricesColumn).Exists())
            {
                Create.Column(RoundNetPricesColumn).OnTable(CurrencyTable).AsBoolean().Nullable();
            }

            if (!Schema.Table(CurrencyTable).Column(RoundUnitPricesColumn).Exists())
            {
                Create.Column(RoundUnitPricesColumn).OnTable(CurrencyTable).AsBoolean().Nullable();
            }
        }

        public override void Down()
        {
            if (Schema.Table(CurrencyTable).Column(MidpointRoundingColumn).Exists())
            {
                Delete.Column(MidpointRoundingColumn).FromTable(CurrencyTable);
            }

            if (Schema.Table(CurrencyTable).Column(RoundNetPricesColumn).Exists())
            {
                Delete.Column(RoundNetPricesColumn).FromTable(CurrencyTable);
            }

            if (Schema.Table(CurrencyTable).Column(RoundUnitPricesColumn).Exists())
            {
                Delete.Column(RoundUnitPricesColumn).FromTable(CurrencyTable);
            }
        }

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            // Apply default for backward compatibility.
            await context.Currencies
                .Where(x => x.RoundOrderItemsEnabled != null && x.RoundOrderItemsEnabled.Value == true)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(c => c.RoundNetPrices, p => true)
                    .SetProperty(c => c.RoundUnitPrices, p => true), cancelToken);

            // Set RoundOrderItemsEnabled to "null" to more easily apply CurrencySettings.RoundOrderItemsEnabled (default is "false").
            await context.Currencies
                .Where(x => x.RoundOrderItemsEnabled != null && x.RoundOrderItemsEnabled.Value == false)
                .ExecuteUpdateAsync(setter => setter.SetProperty(c => c.RoundOrderItemsEnabled, p => null), cancelToken);

            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            #region Currency

            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.AwayFromZero",
     "Commercial rounding (recommended)",
     "Kaufmännisches Runden (empfohlen)",
     "گرد کردن تجاری (توصیه شده)");

            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.ToEven",
                "Banker's rounding",
                "Mathematisches Runden",
                "گرد کردن بانکی");

            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.AwayFromZero.Example",
                "1.225 is rounded up to 1.23. 1.224 is rounded down to 1.22.",
                "1,225 wird auf 1,23 aufgerundet. 1,224 wird auf 1,22 abgerundet.",
                "1.225 به 1.23 گرد می‌شود (به بالا). 1.224 به 1.22 گرد می‌شود (به پایین).");

            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.ToEven.Example",
                "1.225 is rounded down to 1.22. 1.235 is rounded up to 1.24.",
                "1,225 wird auf 1,22 abgerundet. 1,235 wird auf 1,24 aufgerundet.",
                "1.225 به 1.22 گرد می‌شود (به پایین). 1.235 به 1.24 گرد می‌شود (به بالا).");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.MidpointRounding",
                "Midpoint rounding",
                "Mittelwertrundung",
                "گرد کردن میانی",
                "Specifies the rounding strategy of the midway between two amounts. Default is comercial rounding (round midpoint to the nearest amount that is away from zero).",
                "Legt die Rundungsstrategie für die Mitte zwischen zwei Beträgen fest. Standard ist kaufmännisches Runden (Mittelwert auf den nächstgelegenen, von Null entfernten Betrag runden).",
                "استراتژی گرد کردن برای مقدار میانی بین دو مقدار را مشخص می‌کند. پیش‌فرض گرد کردن تجاری است (گرد کردن مقدار میانی به نزدیک‌ترین مقدار دور از صفر).");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.RoundNetPrices",
                "Round when net prices are displayed",
                "Bei Nettopreisanzeige runden",
                "گرد کردن هنگام نمایش قیمت‌های خالص",
                "Specifies whether to round during shopping cart calculation even if net prices are displayed to the customer.",
                "Legt fest, ob bei der Warenkorbberechnung auch dann gerundet werden soll, wenn dem Kunden Nettopreise angezeigt werden.",
                "مشخص می‌کند که آیا در محاسبه سبد خرید، حتی اگر قیمت‌های خالص به مشتری نمایش داده شود، باید گرد شود یا خیر.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.RoundUnitPrices",
                "Round unit price",
                "Einzelpreis runden",
                "گرد کردن قیمت واحد",
                "Specifies whether the product price should be rounded before or after quantity multiplication during shopping cart calculation. If enabled, the unit price is rounded and then multiplied by the quantity. If disabled, the unit price is multiplied by the quantity and then rounded.",
                "Legt fest, ob der Produktpreis bei der Warenkorbberechnung vor oder nach der Mengenmultiplikation gerundet werden soll. Falls aktiviert wird der Einzelpreis gerundet und dann mit der Menge multipliziert. Falls deaktiviert wird der Einzelpreis mit der Menge multipliziert und erst danach gerundet.",
                "مشخص می‌کند که آیا قیمت محصول در محاسبه سبد خرید باید قبل یا بعد از ضرب در مقدار گرد شود. اگر فعال باشد، قیمت واحد گرد شده و سپس در مقدار ضرب می‌شود. اگر غیرفعال باشد، قیمت واحد در مقدار ضرب شده و سپس گرد می‌شود.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.RoundOrderItemsEnabled",
                "Round all order item amounts",
                "Beträge aller Bestellpositionen runden",
                "گرد کردن تمام مبالغ اقلام سفارش",
                "Specifies whether to round all order item amounts (prices, tax, fees etc.). The general rounding settings are applied, if not specified here.",
                "Legt fest, ob die Beträge aller Bestellpositionen gerundet werden sollen (Preise, Steuern, Gebühren etc.). Es gelten die allgemeinen Rundungseinstellungen, sofern hier nicht festgelegt.",
                "مشخص می‌کند که آیا تمام مبالغ اقلام سفارش (قیمت‌ها، مالیات، هزینه‌ها و غیره) باید گرد شوند یا خیر. در صورت عدم تعیین در اینجا، تنظیمات عمومی گرد کردن اعمال می‌شود.");
            #endregion

            #region Currency settings

            builder.Delete(
                "Admin.Configuration.Currencies.Fields.ExchangeRateProvider",
                "Admin.Configuration.Currencies.Fields.ExchangeRateProvider.Hint",
                "Admin.Configuration.Currencies.Fields.CurrencyRateAutoUpdateEnabled",
                "Admin.Configuration.Currencies.Fields.CurrencyRateAutoUpdateEnabled.Hint",
                "Admin.Configuration.Settings.Tax"
            );
            builder.AddOrUpdate("Common.Finance",
                "Finance",
                "Finanzen",
                "مالی");

            builder.AddOrUpdate("Admin.Configuration.Settings.Currency.ExchangeRateProvider",
                "Online exchange rate service",
                "Online Wechselkursdienst",
                "سرویس نرخ ارز آنلاین");

            builder.AddOrUpdate("Admin.Configuration.Settings.Currency.AutoUpdateEnabled",
                "Automatically update exchange rates",
                "Wechselkurse automatisch aktualisieren",
                "به‌روزرسانی خودکار نرخ‌های ارز",
                "Specifies whether exchange rates should be automatically updated via the associated scheduled task.",
                "Legt fest, ob Wechselkurse über die zugehörige geplante Aufgabe automatisch aktualisiert werden sollen.",
                "مشخص می‌کند که آیا نرخ‌های ارز باید از طریق وظیفه زمان‌بندی‌شده مرتبط به‌صورت خودکار به‌روزرسانی شوند یا خیر.");

            builder.AddOrUpdate("Admin.Configuration.Settings.Currency.RoundOrderItemsEnabled",
                "Round all order item amounts",
                "Beträge aller Bestellpositionen runden",
                "گرد کردن تمام مبالغ اقلام سفارش",
                "Specifies whether to round all order item amounts (prices, tax, fees etc.). Rounding settings can optionally also be specified for the respective currency.",
                "Legt fest, ob die Beträge aller Bestellpositionen gerundet werden sollen (Preise, Steuern, Gebühren etc.). Rundungseinstellungen können optional auch bei der jeweiligen Währung festgelegt werden.",
                "مشخص می‌کند که آیا تمام مبالغ اقلام سفارش (قیمت‌ها، مالیات، هزینه‌ها و غیره) باید گرد شوند یا خیر. تنظیمات گرد کردن می‌تواند به‌صورت اختیاری برای ارز مربوطه نیز مشخص شود.");
            #endregion
        }
    }
}

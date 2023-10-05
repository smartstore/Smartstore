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
                Create.Column(MidpointRoundingColumn).OnTable(CurrencyTable).AsInt32().NotNullable().WithDefaultValue((int)CurrencyMidpointRounding.ToEven);
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

        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.Currencies
                .Where(x => x.RoundOrderItemsEnabled != null && x.RoundOrderItemsEnabled.Value == true)
                .ExecuteUpdateAsync(setter => setter
                    .SetProperty(c => c.RoundNetPrices, p => true)
                    .SetProperty(c => c.RoundUnitPrices, p => true), cancelToken);

            await context.Currencies
                .Where(x => x.RoundOrderItemsEnabled != null && x.RoundOrderItemsEnabled.Value == false)
                .ExecuteUpdateAsync(setter => setter.SetProperty(c => c.RoundOrderItemsEnabled, p => null), cancelToken);

            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            #region Currency

            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.ToEven",
                "Banker's rounding (round midpoint to the nearest even number)",
                "Mathematisches Runden (Mittelwert auf den nächstgelegenen geraden Betrag runden)");

            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.AwayFromZero",
                "Commercial rounding (round midpoint to the nearest amount that is away from zero)",
                "Kaufmännisches Runden (Mittelwert auf den nächstgelegenen von Null entfernten Betrag runden)");

            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.ToEven.Example",
                "1.2250 is rounded down to 1.22. 1.2350 is rounded up to 1.24.",
                "1,2250 wird auf 1,22 abgerundet. 1,2350 wird auf 1,24 aufgerundet.");

            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.AwayFromZero.Example",
                "1.2250 is rounded up to 1.23. 1.2240 is rounded down to 1.22.",
                "1,2250 wird auf 1,23 aufgerundet. 1,2240 wird auf 1,22 abgerundet.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.MidpointRounding",
                "Midpoint rounding",
                "Mittelwertrundung",
                "Specifies the rounding strategy of the midway between two amounts. Default is banker's rounding.",
                "Legt die Rundungsstrategie für die Mitte zwischen zwei Beträgen fest. Standard ist mathematisches Runden.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.RoundNetPrices",
                "Round when net prices are displayed",
                "Bei Nettopreisanzeige runden",
                "Specifies whether to round during shopping cart calculation even if net prices are displayed to the customer.",
                "Legt fest, ob bei der Warenkorbberechnung auch dann gerundet werden soll, wenn dem Kunden Nettopreise angezeigt werden.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.RoundUnitPrices",
                "Round unit price",
                "Einzelpreis runden",
                "Specifies whether to round the product unit price before or after quantity multiplication during shopping cart calculation. If activated, rounding takes place before, otherwise after the quantity multiplication.",
                "Legt fest, ob bei der Warenkorbberechnung der Einzelpreis eines Produktes vor oder nach der Mengenmultiplikation gerundet werden soll. Falls aktiviert wird vor, sonst nach der Mengenmultiplikation gerundet.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.RoundOrderItemsEnabled",
                "Round all order item amounts",
                "Beträge aller Bestellpositionen runden",
                "Specifies whether to round all order item amounts (products, tax, fees etc.). The currency settings will be applied if this setting is not specified.",
                "Legt fest, ob die Beträge aller Bestellpositionen gerundet werden sollen (Produkte, Steuern, Gebühren etc.). Es werden die gleichnamigen Währungseinstellungen angewendet, sofern diese Einstellung nicht festgelegt ist.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.RoundOrderItemsNote",
                "The <a href=\"{0}\" class=\"alert-link\">currency settings</a> of the same name will be applied unless settings for rounding order items are specified for this currency.",
                "Es werden die gleichnamigen <a href=\"{0}\" class=\"alert-link\">Währungseinstellungen</a> angewendet, sofern bei dieser Währung keine Einstellungen zum Runden von Bestellpositionen festgelegt sind.");

            #endregion

            #region Currency settings

            builder.Delete(
                "Admin.Configuration.Currencies.Fields.ExchangeRateProvider",
                "Admin.Configuration.Currencies.Fields.ExchangeRateProvider.Hint",
                "Admin.Configuration.Currencies.Fields.CurrencyRateAutoUpdateEnabled",
                "Admin.Configuration.Currencies.Fields.CurrencyRateAutoUpdateEnabled.Hint",
                "Admin.Configuration.Settings.Tax"
            );

            builder.AddOrUpdate("Common.Finance", "Finance", "Finanzen");

            builder.AddOrUpdate("Admin.Configuration.Settings.Currency.ExchangeRateProvider",
                "Online exchange rate service",
                "Online Wechselkursdienst");

            builder.AddOrUpdate("Admin.Configuration.Settings.Currency.AutoUpdateEnabled",
                "Automatically update exchange rates",
                "Wechselkurse automatisch aktualisieren",
                "Specifies whether exchange rates should be automatically updated via the associated scheduled task.",
                "Legt fest, ob Wechselkurse über die zugehörige geplante Aufgabe automatisch aktualisiert werden sollen.");

            #endregion
        }
    }
}

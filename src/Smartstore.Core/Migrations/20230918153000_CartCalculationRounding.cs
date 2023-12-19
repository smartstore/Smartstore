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

            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.AwayFromZero", "Commercial rounding (recommended)", "Kaufmännisches Runden (empfohlen)");
            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.ToEven", "Banker's rounding", "Mathematisches Runden");

            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.AwayFromZero.Example",
                "1.225 is rounded up to 1.23. 1.224 is rounded down to 1.22.",
                "1,225 wird auf 1,23 aufgerundet. 1,224 wird auf 1,22 abgerundet.");

            builder.AddOrUpdate("Enums.CurrencyMidpointRounding.ToEven.Example",
                "1.225 is rounded down to 1.22. 1.235 is rounded up to 1.24.",
                "1,225 wird auf 1,22 abgerundet. 1,235 wird auf 1,24 aufgerundet.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.MidpointRounding",
                "Midpoint rounding",
                "Mittelwertrundung",
                "Specifies the rounding strategy of the midway between two amounts. Default is comercial rounding (round midpoint to the nearest amount that is away from zero).",
                "Legt die Rundungsstrategie für die Mitte zwischen zwei Beträgen fest. Standard ist kaufmännisches Runden (Mittelwert auf den nächstgelegenen, von Null entfernten Betrag runden).");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.RoundNetPrices",
                "Round when net prices are displayed",
                "Bei Nettopreisanzeige runden",
                "Specifies whether to round during shopping cart calculation even if net prices are displayed to the customer.",
                "Legt fest, ob bei der Warenkorbberechnung auch dann gerundet werden soll, wenn dem Kunden Nettopreise angezeigt werden.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.RoundUnitPrices",
                "Round unit price",
                "Einzelpreis runden",
                "Specifies whether the product price should be rounded before or after quantity multiplication during shopping cart calculation. If enabled, the unit price is rounded and then multiplied by the quantity. If disabled, the unit price is multiplied by the quantity and then rounded.",
                "Legt fest, ob der Produktpreis bei der Warenkorbberechnung vor oder nach der Mengenmultiplikation gerundet werden soll. Falls aktiviert wird der Einzelpreis gerundet und dann mit der Menge multipliziert. Falls deaktiviert wird der Einzelpreis mit der Menge multipliziert und erst danach gerundet.");

            builder.AddOrUpdate("Admin.Configuration.Currencies.Fields.RoundOrderItemsEnabled",
                "Round all order item amounts",
                "Beträge aller Bestellpositionen runden",
                "Specifies whether to round all order item amounts (prices, tax, fees etc.). The general rounding settings are applied, if not specified here.",
                "Legt fest, ob die Beträge aller Bestellpositionen gerundet werden sollen (Preise, Steuern, Gebühren etc.). Es gelten die allgemeinen Rundungseinstellungen, sofern hier nicht festgelegt.");

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

            builder.AddOrUpdate("Admin.Configuration.Settings.Currency.RoundOrderItemsEnabled",
                "Round all order item amounts",
                "Beträge aller Bestellpositionen runden",
                "Specifies whether to round all order item amounts (prices, tax, fees etc.). Rounding settings can optionally also be specified for the respective currency.",
                "Legt fest, ob die Beträge aller Bestellpositionen gerundet werden sollen (Preise, Steuern, Gebühren etc.). Rundungseinstellungen können optional auch bei der jeweiligen Währung festgelegt werden.");

            #endregion
        }
    }
}

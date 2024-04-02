using FluentMigrator;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2024-03-29 18:00:00", "Core: cart item enabled")]
    internal class CartItemEnabled : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string CartItemTableName = nameof(ShoppingCartItem);
        const string EnabledColumn = nameof(ShoppingCartItem.Enabled);
        const string EnabledIndex = "IX_CartItemEnabled";

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public override void Up()
        {
            if (!Schema.Table(CartItemTableName).Column(EnabledColumn).Exists())
            {
                Create.Column(EnabledColumn).OnTable(CartItemTableName).AsBoolean().NotNullable().WithDefaultValue(true);
            }

            if (!Schema.Table(CartItemTableName).Index(EnabledIndex).Exists())
            {
                Create.Index(EnabledIndex)
                    .OnTable(CartItemTableName)
                    .OnColumn(EnabledColumn)
                    .Ascending()
                    .WithOptions()
                    .NonClustered();
            }
        }

        public override void Down()
        {
            var table = Schema.Table(CartItemTableName);

            if (table.Index(EnabledIndex).Exists())
            {
                Delete.Index(EnabledIndex).OnTable(CartItemTableName);
            }

            if (table.Column(EnabledColumn).Exists())
            {
                Delete.Column(EnabledColumn).FromTable(CartItemTableName);
            }
        }

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Configuration.Settings.ShoppingCart.AllowCartItemsToBeDisabled",
                "Allow cart items to be disabled",
                "Deaktivierung von Warenkorbartikeln zulassen",
                "Specifies whether shopping cart items can be deactivated. Deactivated items are not ordered and remain in the shopping cart after the order is received.",
                "Legt fest, ob Warenkorbartikel deaktiviert werden können. Deaktivierte Artikel werden nicht mitbestellt und verbleiben nach Auftragseingang im Warenkorb.");
        }
    }
}

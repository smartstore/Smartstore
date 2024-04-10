using FluentMigrator;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2024-03-29 18:00:00", "Core: cart item active")]
    internal class CartItemActive : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string CartItemTableName = nameof(ShoppingCartItem);
        const string ActiveColumn = nameof(ShoppingCartItem.Active);
        const string ActiveIndex = "IX_CartItemActive";

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public override void Up()
        {
            if (!Schema.Table(CartItemTableName).Column(ActiveColumn).Exists())
            {
                Create.Column(ActiveColumn).OnTable(CartItemTableName).AsBoolean().NotNullable().WithDefaultValue(true);
            }

            if (!Schema.Table(CartItemTableName).Index(ActiveIndex).Exists())
            {
                Create.Index(ActiveIndex)
                    .OnTable(CartItemTableName)
                    .OnColumn(ActiveColumn)
                    .Ascending()
                    .WithOptions()
                    .NonClustered();
            }
        }

        public override void Down()
        {
            var table = Schema.Table(CartItemTableName);

            if (table.Index(ActiveIndex).Exists())
            {
                Delete.Index(ActiveIndex).OnTable(CartItemTableName);
            }

            if (table.Column(ActiveColumn).Exists())
            {
                Delete.Column(ActiveColumn).FromTable(CartItemTableName);
            }
        }

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            const string cartSelectionLink = "<a href=\"{{0}}\" class=\"{1}\" rel=\"nofollow\">{0}</a>.";

            builder.AddOrUpdate("ShoppingCart.NoProductsSelectedSelectAll",
                "No products selected. " + cartSelectionLink.FormatInvariant("Select all products", "select-cart-items"),
                "Keine Artikel ausgewählt. " + cartSelectionLink.FormatInvariant("Alle Artikel auswählen", "select-cart-items"));

            builder.AddOrUpdate("ShoppingCart.SelectAllProducts",
                cartSelectionLink.FormatInvariant("Select all products", "select-cart-items"),
                cartSelectionLink.FormatInvariant("Alle Artikel auswählen", "select-cart-items"));

            builder.AddOrUpdate("ShoppingCart.DeselectAllProducts",
                cartSelectionLink.FormatInvariant("Deselect all products", "deselect-cart-items"),
                cartSelectionLink.FormatInvariant("Auswahl aller Artikel aufheben", "deselect-cart-items"));

            builder.AddOrUpdate("ShoppingCart.Totals.SubTotalSelectedProducts",
                "Subtotal <span class=\"text-nowrap\">({0} products)</span>",
                "Zwischensumme <span class=\"text-nowrap\">({0} Artikel)</span>");

            builder.AddOrUpdate("ShoppingCart.SelectAtLeastOneProduct",
                "Please select at least one product to proceed to checkout.",
                "Bitte wählen Sie mindestens einen Artikel aus, um zur Kasse zu gehen.");
        }
    }
}

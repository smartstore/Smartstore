using FluentMigrator;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-10-23 13:00:00", "Core: Withdrawal waiver acceptance")]
internal class WithdrawalWaiverAcceptedOn : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
{
    const string TableName = nameof(OrderItem);
    const string ColumnName = nameof(OrderItem.WithdrawalWaiverAcceptedOn);

    public override void Up()
    {
        if (!Schema.Table(TableName).Column(ColumnName).Exists())
        {
            Create.Column(ColumnName)
                .OnTable(TableName)
                .AsDateTime2()
                .Nullable();
        }
    }

    public override void Down()
    {
        if (Schema.Table(TableName).Column(ColumnName).Exists())
        {
            Delete.Column(ColumnName).FromTable(TableName);
        }
    }

    public DataSeederStage Stage => DataSeederStage.Early;
    public bool AbortOnFailure => false;

    public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        => await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);

    public void MigrateLocaleResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("Admin.Orders.Products.WithdrawalWaiver",
            "Withdrawal waiver",
            "Widerrufsverzicht",
            "Indicates whether and when the customer accepted the withdrawal waiver for this order item.",
            "Zeigt an, ob und wann der Kunde den Widerrufsverzicht für diese Auftragsposition erklärt hat.");
    }
}

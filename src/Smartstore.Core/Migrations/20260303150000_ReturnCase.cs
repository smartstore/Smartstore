using FluentMigrator;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Data.Migrations;
using RcEntity = Smartstore.Core.Checkout.Orders.ReturnCase;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-03-03 15:00:00", "Core: ReturnCase")]
internal class ReturnCase : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
{
    const string TableName = nameof(Checkout.Orders.ReturnCase);

    public override void Up()
    {
        Rename.Table("ReturnRequest").To(TableName);

        Rename.Column("ReturnRequestStatusId").OnTable(TableName)
            .To(nameof(RcEntity.ReturnCaseStatusId));

        Alter.Column(nameof(RcEntity.ReasonForReturn)).OnTable(TableName)
            .AsString(4000)
            .Nullable();

        Alter.Column(nameof(RcEntity.RequestedAction)).OnTable(TableName)
            .AsString(4000)
            .Nullable();

        Alter.Column(nameof(RcEntity.ReturnCaseStatusId)).OnTable(TableName)
            .AsInt32()
            .Nullable();

        if (!Schema.Table(TableName).Column(nameof(RcEntity.Kind)).Exists())
        {
            Create.Column(nameof(RcEntity.Kind)).OnTable(TableName)
                .AsInt32()
                .NotNullable()
                .WithDefaultValue((int)ReturnCaseKind.Return);
        }
    }

    public override void Down()
    {
        // Due to the renaming of the database table, the migration cannot be automatically reset.
    }

    public DataSeederStage Stage => DataSeederStage.Early;
    public bool AbortOnFailure => false;

    public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
    {
        // Rename permission names.
        var permissions = await context.PermissionRecords
            .Where(x => x.SystemName.StartsWith("order.returnrequest"))
            .ToListAsync(cancelToken);
        permissions.Each(x => x.SystemName = x.SystemName.Replace("order.returnrequest", "order.returncase"));

        // Rename enum resource names.
        var stringResources = await context.LocaleStringResources
            .Where(x => x.ResourceName.StartsWith("Enums.ReturnRequestStatus."))
            .ToListAsync(cancelToken);
        stringResources.Each(x => x.ResourceName = x.ResourceName.Replace("Enums.ReturnRequestStatus.", "Enums.ReturnCaseStatus."));

        await context.SaveChangesAsync(cancelToken);

        await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
    }

    public void MigrateLocaleResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("Enums.ReturnCaseKind.Return", "Return", "Retoure");
        builder.AddOrUpdate("Enums.ReturnCaseKind.Withdrawal", "Withdrawal", "Widerruf");
    }
}

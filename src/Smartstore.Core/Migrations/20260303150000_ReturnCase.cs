using FluentMigrator;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Logging;
using Smartstore.Data.Migrations;
using RcEntity = Smartstore.Core.Checkout.Orders.ReturnCase;

namespace Smartstore.Core.Migrations;

[MigrationVersion("2026-03-03 15:00:00", "Core: ReturnCase")]
internal class ReturnCase : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
{
    const string TableName = nameof(Checkout.Orders.ReturnCase);

    public override void Up()
    {
        if (!Schema.Table(TableName).Exists())
        {
            Rename.Table("ReturnRequest").To(TableName);
        }

        if (!Schema.Table(TableName).Column(nameof(RcEntity.ReturnCaseStatusId)).Exists())
        {
            Rename.Column("ReturnRequestStatusId").OnTable(TableName)
                .To(nameof(RcEntity.ReturnCaseStatusId));
        }

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
        var defaultLanguage = await context.Languages.OrderBy(x => x.DisplayOrder).FirstOrDefaultAsync(cancelToken);
        var isDeDefault = defaultLanguage?.UniqueSeoCode == "de";
        var isEnDefault = defaultLanguage?.UniqueSeoCode == "en";

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

        // Poor migration of message templates.
        var templateNames = new string[] { "NewReturnRequest.StoreOwnerNotification", "ReturnRequestStatusChanged.CustomerNotification" };
        var messageTemplates = await context.MessageTemplates
            .Where(x => templateNames.Contains(x.Name))
            .ToListAsync(cancelToken);

        foreach (var template in messageTemplates)
        {
            template.Name = template.Name.Replace("ReturnRequest", "ReturnCase");
            template.Body = template.Body.Replace("ReturnRequest.", "ReturnCase.");
            template.ModelTypes = template.ModelTypes.Replace("ReturnRequest", "ReturnCase");
            template.LastModelTree = null;
        }

        // Rename activity log types.
        var logTypes = await context.ActivityLogTypes
            .Where(x => x.SystemKeyword == "EditReturnRequest" || x.SystemKeyword == "DeleteReturnRequest")
            .ToListAsync(cancelToken);

        foreach (var logType in logTypes)
        {
            if (logType.SystemKeyword == "EditReturnRequest")
            {
                logType.SystemKeyword = KnownActivityLogTypes.EditReturnCase;
                if (isDeDefault)
                    logType.Name = "Retoure bearbeitet";
                else if (isEnDefault)
                    logType.Name = "Edited a return";
            }
            else if (logType.SystemKeyword == "DeleteReturnRequest")
            {
                logType.SystemKeyword = KnownActivityLogTypes.DeleteReturnCase;
                if (isDeDefault)
                    logType.Name = "Retoure gelöscht";
                else if (isEnDefault)
                    logType.Name = "Deleted a return";
            }
        }

        await context.SaveChangesAsync(cancelToken);

        await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
    }

    public void MigrateLocaleResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate("Enums.ReturnCaseKind.Return", "Return", "Retoure");
        builder.AddOrUpdate("Enums.ReturnCaseKind.Withdrawal", "Withdrawal", "Widerruf");

        // Only update DE and EN.
        builder.AddOrUpdate("ActivityLog.EditReturnRequest")
            .Value("en", "Edited a return (ID = {0})");
        builder.AddOrUpdate("ActivityLog.EditReturnRequest")
            .Value("de", "Retoure (ID = {0}) bearbeitet");

        builder.AddOrUpdate("ActivityLog.DeleteReturnRequest")
            .Value("en", "Deleted a return (ID = {0})");
        builder.AddOrUpdate("ActivityLog.DeleteReturnRequest")
            .Value("de", "Retoure (ID = {0}) gelöscht");
    }
}

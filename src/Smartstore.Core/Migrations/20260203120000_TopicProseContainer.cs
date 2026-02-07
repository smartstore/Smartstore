using FluentMigrator;
using Smartstore.Core.Content.Topics;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations;

[MigrationVersion("2026-02-03 12:00:00", "Core: Topic ProseContainer")]
internal class TopicProseContainer : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
{
    const string TableName = nameof(Topic);
    const string ColumnName = nameof(Topic.EnableProseContainer);

    public override void Up()
    {
        if (!Schema.Table(TableName).Column(ColumnName).Exists())
        {
            Create.Column(ColumnName).OnTable(TableName)
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);
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
    {
        await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        await MigrateTopics(context, cancelToken);
    }

    public void MigrateLocaleResources(LocaleResourcesBuilder builder)
    {
        builder.AddOrUpdate(
            "Admin.ContentManagement.Topics.Fields.EnableProseContainer",
            "Enable narrow text container",
            "Schmalen Text-Container aktivieren",
            "When enabled, the topic is rendered in a narrow prose container (not full page width).",
            "Wenn aktiviert, wird das Topic in einem schmalen Prosa-Container (nicht in voller Seitenbreite) dargestellt.");
    }

    private async Task MigrateTopics(SmartDbContext db, CancellationToken cancelToken = default)
    {
        var systemNames = new[]
        {
            "ForumWelcomeMessage",
            "AboutUs",
            "ConditionsOfUse",
            "PrivacyInfo",
            "ShippingInfo",
            "Imprint",
            "Disclaimer",
            "PaymentInfo"
        };

        await db.Topics
            .Where(x => systemNames.Contains(x.SystemName) && !x.EnableProseContainer)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EnableProseContainer, true), cancelToken);
    }
}
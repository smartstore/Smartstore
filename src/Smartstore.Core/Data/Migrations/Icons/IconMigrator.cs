using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Topics;
using Smartstore.IconKit;
using Smartstore.Events;

namespace Smartstore.Core.Data.Migrations.Icons;

/// <summary>
/// Migrates persisted legacy icons to icon expressions.
/// </summary>
public class IconMigrator : IIconMigrator
{
    private readonly IEventPublisher _eventPublisher;

    private readonly IIconService _icons;

    public IconMigrator(IEventPublisher eventPublisher, IIconService icons)
    {
        _eventPublisher = eventPublisher;
        _icons = icons;
    }

    /// <inheritdoc />
    public async Task<IconMigrationResult> MigrateAsync(
        SmartDbContext context,
        Action<IconMigrationEntry> report = null,
        CancellationToken cancelToken = default)
    {
        Guard.NotNull(context);

        // This service is a singleton: keep the caller-owned DbContext and all run state local.
        // Reuse the complete immutable name index; no search paging or per-icon metadata allocation is needed.
        var brandNames = _icons.GetIconNames("fa", "b");
        var migrationContext = new IconMigrationContext(context, report, new LegacyIconConverter(brandNames));
        var message = new IconMigrationEvent(migrationContext);
        message.Providers.Add(new MenuIconMigrationProvider());
        // Core owns these fields. Installed plugins append their own registrations through the event.
        message.RegisterHtml<Manufacturer>(x => x.Description);
        message.RegisterHtml<Manufacturer>(x => x.BottomDescription);
        message.RegisterHtml<Category>(x => x.Description);
        message.RegisterHtml<Category>(x => x.BottomDescription);
        message.RegisterHtml<Product>(x => x.UserAgreementText);
        message.RegisterHtml<Product>(x => x.FullDescription);
        message.RegisterHtml<Topic>(x => x.Body);
        await _eventPublisher.PublishAsync(message, cancelToken);

        var checkedRecords = 0;
        var migratedIcons = 0;
        var notMigratedIcons = 0;

        // Run specialized migrations first (for example structured menu or Page Builder data).
        foreach (var provider in message.Providers)
        {
            var result = await provider.MigrateAsync(migrationContext, cancelToken);
            checkedRecords += result.CheckedRecords;
            migratedIcons += result.MigratedIcons;
            notMigratedIcons += result.NotMigratedIcons;
        }

        // Only after providers finish, process the simple HTML fields with the shared batch routine.
        foreach (var field in migrationContext.HtmlFields)
        {
            var result = await HtmlIconMigrationUtility.MigrateAsync(migrationContext, field, cancelToken);
            checkedRecords += result.CheckedRecords;
            migratedIcons += result.MigratedIcons;
            notMigratedIcons += result.NotMigratedIcons;
        }

        return new IconMigrationResult
        {
            CheckedRecords = checkedRecords,
            MigratedIcons = migratedIcons,
            NotMigratedIcons = notMigratedIcons
        };
    }
}

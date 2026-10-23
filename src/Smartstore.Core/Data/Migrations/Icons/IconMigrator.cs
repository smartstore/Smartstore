using System.Diagnostics;
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
    private readonly ILogger _logger;

    public IconMigrator(IEventPublisher eventPublisher, IIconService icons, ILoggerFactory loggerFactory)
    {
        _eventPublisher = eventPublisher;
        _icons = icons;
        _logger = loggerFactory.CreateLogger("File/App_Data/Logs/icon-migration-.log");
    }

    /// <inheritdoc />
    public async Task<IconMigrationResult> MigrateAsync(
        SmartDbContext context,
        Action<IconMigrationEntry> report = null,
        CancellationToken cancelToken = default)
    {
        Guard.NotNull(context);

        var runId = Guid.NewGuid();
        var started = Stopwatch.GetTimestamp();
        _logger.Info("Icon migration {RunId} started.", runId);

        try
        {
            // This service is a singleton: keep the caller-owned DbContext and all run state local.
            // Reuse the complete immutable name index; no search paging or per-icon metadata allocation is needed.
            var brandNames = _icons.GetIconNames("fa", "b");
            var migrationContext = new IconMigrationContext(context, ReportEntry, new LegacyIconConverter(brandNames));
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

            _logger.Info("Icon migration {RunId} completed in {Elapsed}: {CheckedRecords} records checked, {MigratedIcons} icons migrated, {NotMigratedIcons} icons not migrated.",
                runId, Stopwatch.GetElapsedTime(started), checkedRecords, migratedIcons, notMigratedIcons);

            return new IconMigrationResult
            {
                CheckedRecords = checkedRecords,
                MigratedIcons = migratedIcons,
                NotMigratedIcons = notMigratedIcons
            };
        }
        catch (OperationCanceledException) when (cancelToken.IsCancellationRequested)
        {
            _logger.Warn("Icon migration {RunId} cancelled after {Elapsed}. Previously saved batches remain migrated.",
                runId, Stopwatch.GetElapsedTime(started));
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Icon migration {RunId} failed after {Elapsed}.", runId, Stopwatch.GetElapsedTime(started));
            throw;
        }

        void ReportEntry(IconMigrationEntry entry)
        {
            var level = entry.Status is "Warning" or "NotMigrated" ? LogLevel.Warning : LogLevel.Information;
            _logger.Log(level, "Icon migration {RunId}: {@Entry}", runId, entry);
            report?.Invoke(entry);
        }
    }
}

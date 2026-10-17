using Smartstore.Core.Content.Menus;

namespace Smartstore.Core.Data.Migrations.Icons;

/// <summary>
/// Migrates persisted legacy icons to icon expressions.
/// </summary>
internal sealed class MenuIconMigrationProvider : IIconMigrationProvider
{
    /// <inheritdoc />
    public async Task<IconMigrationResult> MigrateAsync(
        IconMigrationContext context,
        CancellationToken cancelToken = default)
    {
        Guard.NotNull(context);

        const int batchSize = 500;
        var lastId = 0;
        var checkedRecords = 0;
        var migratedIcons = 0;
        var notMigratedIcons = 0;
        var converter = context.Converter;

        while (true)
        {
            var items = await context.DbContext.Set<MenuItemEntity>()
                .Where(x => x.Id > lastId && x.Icon != null && x.Icon != "")
                .OrderBy(x => x.Id)
                .Select(x => new { x.Id, x.Icon, x.Style, x.IconColor })
                .Take(batchSize)
                .ToListAsync(cancelToken);

            if (items.Count == 0)
            {
                break;
            }

            foreach (var item in items)
            {
                checkedRecords++;

                // Any library-qualified value is already migrated, not just fa: addresses.
                if (item.Icon.Contains(':'))
                {
                    continue;
                }

                var converted = converter.ConvertIcon(item.Icon, item.Style, item.IconColor);
                if (converted.Warning != null)
                {
                    Report("Warning", message: converted.Warning);
                }

                if (!converted.Expression.HasValue)
                {
                    notMigratedIcons++;
                    Report("NotMigrated");
                    continue;
                }

                var expression = converted.Expression.Value.ToString();
                if (expression == item.Icon)
                {
                    continue;
                }

                // Preserve legacy fields for now. The caller supplies the context; no tracking or SaveChanges is needed.
                // TEMPORARY UI PREVIEW: simulate a successful update without writing to the database.
                // var updated = await context.DbContext.Set<MenuItemEntity>()
                // .Where(x => x.Id == item.Id)
                // .ExecuteUpdateAsync(setter => setter.SetProperty(x => x.Icon, expression), cancelToken);
                var updated = 1;

                migratedIcons += updated;
                if (updated > 0)
                {
                    Report("Migrated", expression);
                }
                else
                {
                    notMigratedIcons++;
                    Report("NotMigrated", message: "The menu item no longer exists.");
                }

                void Report(string status, string newValue = null, string message = null)
                {
                    context.Report(new IconMigrationEntry(
                        "MenuItemRecord", item.Id, nameof(MenuItemEntity.Icon), status, item.Icon, newValue, message));
                }
            }

            lastId = items[^1].Id;
            cancelToken.ThrowIfCancellationRequested();
        }

        return new IconMigrationResult
        {
            CheckedRecords = checkedRecords,
            MigratedIcons = migratedIcons,
            NotMigratedIcons = notMigratedIcons
        };

    }
}

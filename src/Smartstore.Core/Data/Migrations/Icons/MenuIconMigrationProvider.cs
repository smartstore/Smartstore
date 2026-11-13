using Smartstore.Data;
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
        var checkedRecords = 0;
        var migratedIcons = 0;
        var notMigratedIcons = 0;
        var converter = context.Converter;

        var pager = context.DbContext.Set<MenuItemEntity>()
            .Where(x => x.Icon != null && x.Icon != "")
            .ToFastPager(batchSize);

        // Project only migration fields; FastPager maintains the cursor without tracking entity instances.
#pragma warning disable CS0618 // Migration must read the legacy icon style and color.
        while ((await pager.ReadNextPageAsync(
            x => new { x.Id, x.Icon, x.Style, x.IconColor }, x => x.Id, cancelToken)).Out(out var items))
#pragma warning restore CS0618
        {
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

                // Replace the complete legacy icon representation in one update.
#pragma warning disable CS0618 // Clear legacy fields after successfully converting their values.
                var updated = await context.DbContext.Set<MenuItemEntity>()
                    .Where(x => x.Id == item.Id)
                    .ExecuteUpdateAsync(setter => setter
                        .SetProperty(x => x.Icon, expression)
                        .SetProperty(x => x.Style, (string)null)
                        .SetProperty(x => x.IconColor, (string)null), cancelToken);
#pragma warning restore CS0618

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

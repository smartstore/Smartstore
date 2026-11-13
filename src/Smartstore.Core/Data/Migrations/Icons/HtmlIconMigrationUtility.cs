using Smartstore.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Smartstore.Core.Data.Migrations.Icons;

/// <summary>
/// Executes registered HTML field migrations using caller-owned migration state.
/// </summary>
internal static class HtmlIconMigrationUtility
{
    private static readonly MethodInfo MigrateFieldMethod = typeof(HtmlIconMigrationUtility)
        .GetMethod(nameof(MigrateFieldAsync), BindingFlags.NonPublic | BindingFlags.Static);

    public static Task<IconMigrationResult> MigrateAsync(
        IconMigrationContext context, LambdaExpression field, CancellationToken cancelToken)
    {
        // Dispatch once per registered field; rows are processed by the typed EF query below.
        return (Task<IconMigrationResult>)MigrateFieldMethod
            .MakeGenericMethod(field.Parameters[0].Type)
            .Invoke(null, [context, field, cancelToken]);
    }

    /// <summary>
    /// Migrates one HTML property in batches without tracking entities or retaining HTML in the report.
    /// </summary>
    /// <param name="context">The state and services for this migration run.</param>
    /// <param name="field">A direct, mapped string property of the entity.</param>
    /// <param name="cancelToken">The cancellation token.</param>
    /// <returns>The counts for this field migration.</returns>
    private static async Task<IconMigrationResult> MigrateFieldAsync<TEntity>(
        IconMigrationContext context,
        Expression<Func<TEntity, string>> field,
        CancellationToken cancelToken = default)
        where TEntity : BaseEntity
    {
        Guard.NotNull(field);
        const int batchSize = 100;
        Guard.IsTrue(field.Body is MemberExpression member && member.Expression == field.Parameters[0],
            message: "Select a direct entity property, for example x => x.Body.");
        // EF.Property uses the selected CLR property name; the original lambda also drives the update.
        var property = Guard.NotNull(field.ExtractPropertyInfo());
        var fieldName = property.Name;
        var entityName = typeof(TEntity).Name;
        var source = context.DbContext.Set<TEntity>();
        var checkedRecords = 0;
        var migratedIcons = 0;
        var notMigratedIcons = 0;

        // The broad SQL filter selects candidates; the converter decides which tags can be replaced.
        var pager = source
            .Where(x => EF.Property<string>(x, fieldName) != null &&
                EF.Property<string>(x, fieldName).Contains("fa-"))
            .ToFastPager(batchSize);

        // FastPager remains stable when converted rows leave the filter. Only ID and HTML are materialized.
        while ((await pager.ReadNextPageAsync(
            x => new { x.Id, Html = EF.Property<string>(x, fieldName) }, x => x.Id, cancelToken)).Out(out var batch))
        {
            foreach (var item in batch)
            {
                checkedRecords++;
                var converted = context.Converter.ConvertHtml(item.Html);
                // Report locations and diagnostics, never retain full HTML bodies in the report.
                foreach (var warning in converted.Warnings)
                {
                    context.Report(new IconMigrationEntry(entityName, item.Id, fieldName, "Warning", Message: warning));
                }

                notMigratedIcons += converted.NotConvertedIcons;
                if (converted.ConvertedIcons == 0)
                {
                    continue;
                }

                // Write only changed fields immediately. Transaction ownership remains with the caller;
                // no SaveChanges, entity hooks or request-scoped cache invalidation are involved.
                var updated = await source.Where(x => x.Id == item.Id)
                    .ExecuteUpdateAsync(setter => setter.SetProperty(field, converted.Html), cancelToken);
                if (updated > 0)
                {
                    migratedIcons += converted.ConvertedIcons;
                    context.Report(new IconMigrationEntry(entityName, item.Id, fieldName, "Migrated",
                        Message: $"Converted {converted.ConvertedIcons} legacy icons."));
                }
                else
                {
                    notMigratedIcons += converted.ConvertedIcons;
                    context.Report(new IconMigrationEntry(entityName, item.Id, fieldName, "NotMigrated",
                        Message: "The record no longer exists or no longer matches the query."));
                }
            }

            // Explicit cancellation checkpoint once per batch; database operations also receive the token.
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

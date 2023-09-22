namespace Smartstore.Core.Data.Migrations
{
    public static class MigrationBuilderDbContextExtensions
    {
        public static Task MigrateLocaleResourcesAsync(this SmartDbContext ctx, Action<LocaleResourcesBuilder> buildAction, bool updateTouchedResources = false)
        {
            Guard.NotNull(ctx);
            Guard.NotNull(buildAction);

            var builder = new LocaleResourcesBuilder();
            buildAction(builder);
            var entries = builder.Build();

            var migrator = new LocaleResourcesMigrator(ctx);
            return migrator.MigrateAsync(entries, updateTouchedResources);
        }

        public static Task MigrateSettingsAsync(this SmartDbContext ctx, Action<SettingsBuilder> buildAction)
        {
            Guard.NotNull(ctx);
            Guard.NotNull(buildAction);

            var builder = new SettingsBuilder();
            buildAction(builder);
            var entries = builder.Build();

            var migrator = new SettingsMigrator(ctx);
            return migrator.MigrateAsync(entries);
        }
    }
}

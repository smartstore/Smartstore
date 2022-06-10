namespace Smartstore.Core.Data.Migrations
{
    /// <summary>
    /// Seeds new or updated locale resources after a migration has run. 
    /// This interface is usually applied to auto-generated migration classes.
    /// </summary>
    public interface ILocaleResourcesProvider
    {
        void MigrateLocaleResources(LocaleResourcesBuilder builder);
    }
}

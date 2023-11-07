using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Content.Media;
using Smartstore.Core.DataExchange.Import;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            //await MigrateSettingsAsync(context, cancelToken);
        }

        public async Task MigrateSettingsAsync(SmartDbContext db, CancellationToken cancelToken = default)
        {

        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            
        }
    }
}
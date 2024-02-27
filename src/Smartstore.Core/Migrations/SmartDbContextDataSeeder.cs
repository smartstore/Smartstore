using Smartstore.Core.Checkout.Payment;
using Smartstore.Core.Content.Media;
using Smartstore.Core.DataExchange.Import;
using Microsoft.AspNetCore.Authentication.Cookies;
using Smartstore.Core.Configuration;
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
            

            builder.AddOrUpdate("CookieManager.Dialog.AdUserDataConsent.Heading",
                "Marketing",
                "Marketing");

            builder.AddOrUpdate("CookieManager.Dialog.AdUserDataConsent.Intro",
                "With your consent, our advertising partners can set cookies to create an interest profile for you so that we can offer you targeted advertising. For this purpose, we pass on an identifier unique to your customer account to these services.",
                "Unsere Werbepartner können mit Ihrer Einwilligung Cookies setzen, um ein Interessenprofil für Sie zu erstellen, damit wir Ihnen gezielt Werbung anbieten können. Dazu geben wir eine für Ihr Kundenkonto eindeutige Kennung an diese Dienste weiter. ");

            builder.AddOrUpdate("CookieManager.Dialog.AdPersonalizationConsent.Heading",
                "Personalization",
                "Personalisierung");

            builder.AddOrUpdate("CookieManager.Dialog.AdPersonalizationConsent.Intro",
                "Consent to personalisation enables us to offer enhanced functionality and personalisation. They can be set by us or by third-party providers whose services we use on our pages.",
                "Die Zustimmung zur Personalisierung ermöglicht es uns, erweiterte Funktionalität und Personalisierung anzubieten. Sie können von uns oder von Drittanbietern gesetzt werden, deren Dienste wir auf unseren Seiten nutzen.");

        }
    }
}
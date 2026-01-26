using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
            await MigrateSettingsAsync(context, cancelToken);
        }

        public Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            return Task.CompletedTask;
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Orders.Products.AppliedDiscounts",
                "The following discounts were applied to the products: {0}.",
                "Auf die Produkte wurden die folgenden Rabatte gewährt: {0}.");

            builder.AddOrUpdate("Identity.Error.PasswordRequiresDigit",
                "At least one number (0–9)", 
                "Mindestens eine Ziffer (0–9)");

            builder.AddOrUpdate("Identity.Error.PasswordRequiresLower",
                "At least one lowercase letter (a–z)",
                "Mindestens ein Kleinbuchstabe (a–z)");

            builder.AddOrUpdate("Identity.Error.PasswordRequiresNonAlphanumeric",
                "At least one special character (e.g. !@#$)",
                "Mindestens ein Sonderzeichen (z.B. !@#$)");

            builder.AddOrUpdate("Identity.Error.PasswordRequiresUniqueChars",
                "At least {0} unique characters",
                "Mindestens {0} eindeutige Zeichen");

            builder.AddOrUpdate("Identity.Error.PasswordRequiresUpper",
                "At least one uppercase letter (A–Z)",
                "Mindestens ein Großbuchstabe (A–Z)");

            builder.AddOrUpdate("Identity.Error.PasswordTooShort",
                "At least {0} characters",
                "Mindestens {0} Zeichen");

            builder.AddOrUpdate("Account.Register.Result.MeetPasswordRequirements",
                "Your password must meet the following requirements: {0}.",
                "Ihr Passwort muss die folgenden Anforderungen erfüllen: {0}.");
        }
    }
}
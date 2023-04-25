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

        //public async Task MigrateSettingsAsync(SmartDbContext context, CancellationToken cancelToken = default)
        //{
        //    await context.SaveChangesAsync(cancelToken);
        //}

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Account.Fields.Password", "Password", "Passwort");
            builder.AddOrUpdate("Account.Fields.PasswordSecurity", "Password security", "Passwortsicherheit");

            builder.AddOrUpdate("Account.Register.Result.AlreadyRegistered", "You are already registered.", "Sie sind bereits registriert.");
            builder.AddOrUpdate("Admin.Common.Cleanup", "Cleanup", "Aufräumen");

            builder.AddOrUpdate("Admin.System.QueuedEmails.DeleteAll.Confirm",
                "Are you sure you want to delete all sent or undeliverable emails?",
                "Sind Sie sicher, dass alle gesendeten oder unzustellbaren E-Mails gelöscht werden sollen?");

            builder.AddOrUpdate("Admin.ContentManagement.Menus.Item.InvalidTargetLink",
                "The target \"{0}\" of menu link \"{1}\" is invalid.",
                "Das Ziel \"{0}\" von Menü-Link \"{1}\" ist ungültig.");
        }
    }
}
using FluentMigrator;
using Smartstore.Core.Data;
using Smartstore.Core.Data.Migrations;
using Smartstore.Core.Messaging;
using Smartstore.Data.Migrations;
using Smartstore.Net.Mail;

namespace Smartstore.Core.Migrations
{
    [MigrationVersion("2023-03-21 20:00:00", "Core: EmailAccountSecureOption")]
    internal class EmailAccountSecureOption : Migration, ILocaleResourcesProvider, IDataSeeder<SmartDbContext>
    {
        const string EmailAccountTable = nameof(EmailAccount);
        const string SecureOptionColumn = nameof(EmailAccount.SecureOption);

        public DataSeederStage Stage => DataSeederStage.Early;
        public bool AbortOnFailure => false;

        public override void Up()
        {
            if (!Schema.Table(EmailAccountTable).Column(SecureOptionColumn).Exists())
            {
                Create.Column(SecureOptionColumn)
                    .OnTable(EmailAccountTable)
                    .AsInt32()
                    .NotNullable()
                    .WithDefaultValue((int)MailSecureOption.Auto);
            }

            Alter.Table(EmailAccountTable).AlterColumn(nameof(EmailAccount.Username)).AsString(255).Nullable();
            Alter.Table(EmailAccountTable).AlterColumn(nameof(EmailAccount.Password)).AsString(255).Nullable();
        }

        public override void Down()
        {
            if (Schema.Table(EmailAccountTable).Column(SecureOptionColumn).Exists())
            {
                Delete.Column(SecureOptionColumn).FromTable(EmailAccountTable);
            }
        }

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            var emailAccounts = await context.EmailAccounts.ToListAsync(cancelToken);

#pragma warning disable 612, 618
            emailAccounts.Each(x => x.MailSecureOption = x.EnableSsl ? MailSecureOption.SslOnConnect : MailSecureOption.StartTlsWhenAvailable);
#pragma warning restore 612, 618 

            await context.SaveChangesAsync(cancelToken);

            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            builder.AddOrUpdate("Admin.Configuration.EmailAccounts.Fields.MailSecureOption",
                "Encryption",
                "Verschlüsselung",
                "Specifies the encryption for the SMTP connection.",
                "Legt die Verschlüsselung für die SMTP-Verbindung fest.");

            builder.AddOrUpdate("Enums.MailSecureOption.None",
                "No encryption",
                "Keine Verschlüsselung");

            builder.AddOrUpdate("Enums.MailSecureOption.Auto",
                "Automatic (SSL or TLS if available, otherwise no encryption)",
                "Automatisch (SSL oder TLS, falls verfügbar, sonst nicht verschlüsseln)");

            builder.AddOrUpdate("Enums.MailSecureOption.SslOnConnect",
                "Immediately (encrypt connection immediately with SSL or TLS)",
                "Sofort (Verbindung sofort mit SSL oder TLS verschlüsseln)");

            builder.AddOrUpdate("Enums.MailSecureOption.StartTls",
                "StartTLS (always encrypt with TLS after greeting)",
                "StartTLS (immer nach Begrüßung mit TLS verschlüsseln)");

            builder.AddOrUpdate("Enums.MailSecureOption.StartTlsWhenAvailable",
                "StartTLS optional (encrypt with TLS after greeting, if available)",
                "StartTLS optional (nach Begrüßung mit TLS verschlüsseln, falls verfügbar)");

            builder.Delete(
                "Admin.Configuration.EmailAccounts.Fields.EnableSsl",
                "Admin.Configuration.EmailAccounts.Fields.EnableSsl.Hint");
        }
    }
}

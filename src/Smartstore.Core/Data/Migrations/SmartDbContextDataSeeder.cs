using System.Threading;
using System.Threading.Tasks;
using Smartstore.Data.Migrations;

namespace Smartstore.Core.Data.Migrations
{
    public class SmartDbContextDataSeeder : IDataSeeder<SmartDbContext>
    {
        public bool RollbackOnFailure => false;

        public async Task SeedAsync(SmartDbContext context, CancellationToken cancelToken = default)
        {
            await context.MigrateLocaleResourcesAsync(MigrateLocaleResources);
        }

        public static void MigrateLocaleResources(LocaleResourcesBuilder builder)
        {
            #region Identity
            
            builder.Delete("Account.EmailUsernameErrors.UsernameAlreadyExists");    // Isn't used
            builder.Delete("Account.Register.Errors.UsernameAlreadyExists");        // Now is Identity.Error.DuplicateUserName
            builder.Delete("Account.EmailUsernameErrors.EmailAlreadyExists");       // Isn't used
            builder.Delete("Account.Register.Errors.EmailAlreadyExists");           // Now is Identity.Error.DuplicateEmail

            builder.Delete("Account.ChangePassword.Fields.NewPassword.EnteredPasswordsDoNotMatch");     // One resource is enough for this
            builder.Delete("Account.Fields.Password.EnteredPasswordsDoNotMatch");                       // Now is Identity.Error.PasswordMismatch
            builder.Delete("Account.PasswordRecovery.NewPassword.EnteredPasswordsDoNotMatch");          // One resource is enough for this

            builder.Delete("Account.Fields.Password.Digits");                       // Old password validation
            builder.Delete("Account.Fields.Password.SpecialChars");                 // Old password validation
            builder.Delete("Account.Fields.Password.UppercaseChars");               // Old password validation
            builder.Delete("Account.Fields.Password.MustContainChars");             // Old password validation

            builder.Delete("Account.ChangePassword.Errors.EmailIsNotProvided");     // Isn't used
            builder.Delete("Account.ChangePassword.Errors.EmailNotFound");          // Isn't used
            builder.Delete("Account.ChangePassword.Errors.OldPasswordDoesntMatch"); // Isn't used
            builder.Delete("Account.ChangePassword.Errors.PasswordIsNotProvided");  // Isn't used

            builder.AddOrUpdate("Identity.Error.ConcurrencyFailure",
                "A concurrency failure has occured while trying to save your data.",
                "Beim Speichern Ihrer Daten ist ein Fehler durch gleichzeitigen Zugriff aufgetreten.");
            builder.AddOrUpdate("Identity.Error.DefaultError",
                "An error has occurred. Please retry the operation.",
                "Es ist ein Fehler aufgetreten. Bitte führen Sie den Vorgang erneut durch.");
            builder.AddOrUpdate("Identity.Error.DuplicateRoleName",
                "The rolename '{0}' already exists.",
                "Der Name '{0}' wird bereits für eine andere Kundengruppe verwendet.");
            builder.AddOrUpdate("Identity.Error.DuplicateUserName",
                "The username '{0}' already exists.",
                "Der Benutzername '{0}' wird bereits verwendet.");
            builder.AddOrUpdate("Identity.Error.DuplicateEmail",
                "The email '{0}' already exists",
                "Die E-Mail-Adresse '{0}' wird bereits verwendet.");
            builder.AddOrUpdate("Identity.Error.InvalidEmail",
                "Email is not valid.",
                "Keine gültige E-Mail-Adresse.");
            builder.AddOrUpdate("Identity.Error.InvalidRoleName",
                "The name '{0}' is not valid for customer roles.",
                "Der Name '{0}' ist für Kundengruppen nicht gültig.");
            builder.AddOrUpdate("Identity.Error.InvalidToken",
                "Token is not valid.",
                "Das Token ist nicht gültig.");
            builder.AddOrUpdate("Identity.Error.InvalidUserName",
                "The username '{0}' is not valid.",
                "Der Benutzername '{0}' ist nicht gültig.");
            builder.AddOrUpdate("Identity.Error.LoginAlreadyAssociated",
                "The customer is already registered.",
                "Der Kunde ist bereits registriert.");
            builder.AddOrUpdate("Identity.Error.PasswordMismatch",
                "The password and confirmation do not match.",
                "Passwort und Bestätigung stimmen nicht überein.");
            builder.AddOrUpdate("Identity.Error.PasswordRequiresDigit",
                "The password must contain a digit.",
                "Das Passwort muss mind. eine Ziffer enthalten.");
            builder.AddOrUpdate("Identity.Error.PasswordRequiresLower",
                "The password must contain a lowercase letter.",
                "Das Passwort muss mind. einen Kleinbuchstaben enthalten.");
            builder.AddOrUpdate("Identity.Error.PasswordRequiresNonAlphanumeric",
                "The password must contain a non alphanumeric character.",
                "Das Passwort muss mind. ein Sonderzeichen enthalten.");
            builder.AddOrUpdate("Identity.Error.PasswordRequiresUniqueChars",
                "The password must contain at least {0} unique characters.",
                "Das Passwort muss mindestens {0} eindeutige Zeichen enthalten.");
            builder.AddOrUpdate("Identity.Error.PasswordRequiresUpper",
                "The password must contain a capital letter.",
                "Das Passwort muss mind. einen Großbuchstaben enthalten.");
            builder.AddOrUpdate("Identity.Error.PasswordTooShort",
                "The password is too short. It must contain at least {0} characters.",
                "Das Passwort ist zu kurz. Es muss mindestens {0} Zeichen enthalten.");
            builder.AddOrUpdate("Identity.Error.RecoveryCodeRedemptionFailed",
                "The redemption of the recovery code failed.",
                "Die Eingabe des Wiederherstellungscodes ist fehlgeschlagen.");
            builder.AddOrUpdate("Identity.Error.UserAlreadyHasPassword",
                "The user already has a password.",
                "Der Benutzer verfügt bereits über ein Passwort.");
            builder.AddOrUpdate("Identity.Error.UserAlreadyInRole",
                "The user has already been assigned to the customer role '{0}'.",
                "Der Benutzer wurde der Kundengruppe '{0}' bereits zugewiesen.");
            builder.AddOrUpdate("Identity.Error.UserLockoutNotEnabled",
                "User lockout is not enabled.",
                "Die Benutzersperrung ist nicht aktiviert.");
            builder.AddOrUpdate("Identity.Error.UserNotInRole",
                "You do not have the necessary permissions to perform this operation.",
                "Sie verfügen nicht über die erforderlichen Rechte, diesen Vorgang durchzuführen.");

            builder.AddOrUpdate("Account.Register.Result.Disabled",
                "Registration is not allowed at the moment.",
                "Die Registrierung ist momentan nicht erlaubt.");
            

            // INFO: New resources.
            builder.AddOrUpdate("ActivityLog.PublicStore.LoginExternal", "Logged in with {0}", "Eingeloggt mit {0}");
            builder.AddOrUpdate("Account.Login.CheckEmailAccount",
                "The credentials provided are incorrect or you have not activated your account yet. Please check your email inbox and confirm the registration.",
                "Die eingegebenen Benutzerdaten sind nicht korrekt oder Sie haben Ihr Konto noch nicht aktiviert. Bitte prüfen Sie Ihren Email-Posteingang und bestätigen Sie die Registrierung.");

            #endregion
        }
    }
}

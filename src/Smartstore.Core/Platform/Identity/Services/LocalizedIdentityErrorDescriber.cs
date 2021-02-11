using System;
using Microsoft.AspNetCore.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Identity
{
    public class LocalizedIdentityErrorDescriber : IdentityErrorDescriber
    {
        // TODO: (mg) (core) LocalizedIdentityErrorDescriber: override all remaining methods and provide localized error messages.
        // TODO: (mg) (core) LocalizedIdentityErrorDescriber: adapt to Asp.Net Identity way of error description by fixing existing resource entries.
        // TODO: (mg) (core) Migrate Password settings from CustomerSettings to another dedicated Identity config settings class (and adopt Asp.Net Identity password options)

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateEmail),
                Description = T("Account.Register.Errors.EmailAlreadyExists", email)
            };
        }

        public override IdentityError DuplicateUserName(string userName)
        {
            return new IdentityError
            {
                Code = nameof(DuplicateUserName),
                Description = T("Account.Register.Errors.UsernameAlreadyExists", userName)
            };
        }

        public override IdentityError PasswordMismatch()
        {
            return new IdentityError
            {
                Code = nameof(PasswordMismatch),
                Description = T("Account.Fields.Password.EnteredPasswordsDoNotMatch")
            };
        }

        // [...] more to come. See TODOs above.
    }
}

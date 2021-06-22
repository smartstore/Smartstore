using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Bootstrapping
{
    public sealed class IdentityOptionsConfigurer : IConfigureOptions<IdentityOptions>
    {
        private readonly CustomerSettings _customerSettings;

        public IdentityOptionsConfigurer(CustomerSettings customerSettings)
        {
            _customerSettings = customerSettings;
        }

        public void Configure(IdentityOptions options)
        {
            var usr = options.User;
            usr.RequireUniqueEmail = false;
            // INFO: Add space to default list of allowed chars.
            usr.AllowedUserNameCharacters += ' ';

            var pwd = options.Password;
            pwd.RequiredLength = _customerSettings.PasswordMinLength;
            pwd.RequireDigit = _customerSettings.PasswordRequireDigit;
            pwd.RequireUppercase = _customerSettings.PasswordRequireUppercase;
            pwd.RequiredUniqueChars = _customerSettings.PasswordRequiredUniqueChars;
            pwd.RequireLowercase = _customerSettings.PasswordRequireLowercase;
            pwd.RequireNonAlphanumeric = _customerSettings.PasswordRequireNonAlphanumeric;

            var signIn = options.SignIn;
            signIn.RequireConfirmedAccount = false;
            signIn.RequireConfirmedPhoneNumber = false;
            signIn.RequireConfirmedEmail = false;

            // TODO: (mh) (core) Read and apply more IdentityOptions from settings.
            // TODO: (mh) (core) Update IdentityOptions whenever settings change by calling this method from controller with current options.
            //                   This must also be called when setting is changing via all settings grid.
        }
    }
}

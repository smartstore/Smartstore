using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Bootstrapping
{
    internal sealed class IdentityOptionsConfigurer : IConfigureOptions<IdentityOptions>
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
            // INFO: (mh) (core) This is the default value plus empty space.
            // TODO: (mh) (core) Remove comments once reviewed.
            usr.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";

            var pwd = options.Password;
            // TODO: (core) Convert MinDigitsInPassword & MinUppercaseCharsInPassword & MinSpecialCharsInPassword to Identity somehow.
            pwd.RequiredLength = _customerSettings.PasswordMinLength;
            pwd.RequireDigit = _customerSettings.MinDigitsInPassword > 0;
            pwd.RequireUppercase = _customerSettings.MinUppercaseCharsInPassword > 0;
            pwd.RequiredUniqueChars = 1;
            pwd.RequireLowercase = true;
            pwd.RequireNonAlphanumeric = false;

            var signIn = options.SignIn;
            signIn.RequireConfirmedAccount = false;
            signIn.RequireConfirmedPhoneNumber = false;
            signIn.RequireConfirmedEmail =
                _customerSettings.UserRegistrationType == UserRegistrationType.EmailValidation ||
                _customerSettings.UserRegistrationType == UserRegistrationType.AdminApproval;
            signIn.RequireConfirmedEmail = false;

            // TODO: (core) Read and apply more IdentityOptions from settings.
            // TODO: (core) Update IdentityOptions whenever settings change by calling this method from controller with current options.
        }
    }
}

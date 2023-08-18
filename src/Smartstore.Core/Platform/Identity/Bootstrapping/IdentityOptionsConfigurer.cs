using Autofac;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Smartstore.Core.Identity;

namespace Smartstore.Core.Bootstrapping
{
    internal sealed class IdentityOptionsConfigurer : IConfigureOptions<IdentityOptions>
    {
        private readonly IApplicationContext _appContext;

        public IdentityOptionsConfigurer(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public void Configure(IdentityOptions options)
        {
            var customerSettings = _appContext.Services.Resolve<CustomerSettings>();

            var usr = options.User;
            usr.RequireUniqueEmail = true;

            if (customerSettings.CustomerNameAllowedCharacters.HasValue())
            {
                // Add space to default list of allowed characters & characters which were explicitly allowed for customer names.
                // INFO: We don't use += to add special characters to the default list,
                // because the default list may have been reseted by the else case where we set the allowed characters to null.
                usr.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ " + customerSettings.CustomerNameAllowedCharacters;
            }
            else
            {
                // If no characters were explicitly allowed for customer names, then we allow all characters.
                usr.AllowedUserNameCharacters = null;
            }
            
            var pwd = options.Password;
            pwd.RequiredLength = customerSettings.PasswordMinLength;
            pwd.RequireDigit = customerSettings.PasswordRequireDigit;
            pwd.RequireUppercase = customerSettings.PasswordRequireUppercase;
            pwd.RequiredUniqueChars = customerSettings.PasswordRequiredUniqueChars;
            pwd.RequireLowercase = customerSettings.PasswordRequireLowercase;
            pwd.RequireNonAlphanumeric = customerSettings.PasswordRequireNonAlphanumeric;

            var signIn = options.SignIn;
            signIn.RequireConfirmedAccount = false;
            signIn.RequireConfirmedPhoneNumber = false;
            signIn.RequireConfirmedEmail = false;
        }
    }
}

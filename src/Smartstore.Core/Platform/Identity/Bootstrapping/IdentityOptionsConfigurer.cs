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
            // INFO: Add space to default list of allowed chars.
            usr.AllowedUserNameCharacters += ' ';

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

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Smartstore.Core.Data;

namespace Smartstore.Core.Identity
{
    public class SmartSignInManager : SignInManager<Customer>
    {
        private readonly Lazy<SmartDbContext> _db;
        private readonly CustomerSettings _customerSettings;

        public SmartSignInManager(UserManager<Customer> userManager,
            IHttpContextAccessor contextAccessor,
            IUserClaimsPrincipalFactory<Customer> claimsFactory,
            IOptions<IdentityOptions> optionsAccessor,
            ILogger<SignInManager<Customer>> logger,
            IAuthenticationSchemeProvider schemes,
            IUserConfirmation<Customer> confirmation,
            Lazy<SmartDbContext> db,
            CustomerSettings customerSettings)
            : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
        {
            _db = db;
            _customerSettings = customerSettings;
        }

        /// <inheritdoc />
        public override async Task<SignInResult> PasswordSignInAsync(string userNameOrEmail, string password, bool isPersistent, bool lockoutOnFailure)
        {
            Customer user;

            if (_customerSettings.CustomerLoginType == CustomerLoginType.Email)
            {
                user = await UserManager.FindByEmailAsync(userNameOrEmail);
            }
            else if (_customerSettings.CustomerLoginType == CustomerLoginType.Username)
            {
                user = await UserManager.FindByNameAsync(userNameOrEmail);
            }
            else
            {
                user = await UserManager.FindByEmailAsync(userNameOrEmail) ?? await UserManager.FindByNameAsync(userNameOrEmail);
            }

            if (user == null)
            {
                return SignInResult.Failed;
            }

            return await PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);
        }

        /// <inheritdoc />
        public override async Task<SignInResult> PasswordSignInAsync(Customer user, string password, bool isPersistent, bool lockoutOnFailure)
        {
            if (user == null || user.Deleted)
            {
                return SignInResult.Failed;
            }

            if (!user.Active)
            {
                return SignInResult.NotAllowed;
            }

            if (!user.IsRegistered())
            {
                return SignInResult.NotAllowed;
            }

            var result = await base.PasswordSignInAsync(user, password, isPersistent, lockoutOnFailure);

            if (result.Succeeded)
            {
                user.LastLoginDateUtc = DateTime.UtcNow;
                _db.Value.TryUpdate(user);
                await _db.Value.SaveChangesAsync();
            }

            return result;
        }
    }
}

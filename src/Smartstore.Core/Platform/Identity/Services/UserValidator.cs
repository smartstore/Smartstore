using Microsoft.AspNetCore.Identity;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Identity
{
    public class UserValidator : PasswordValidator<Customer>, IUserValidator<Customer>
    {
        private readonly Lazy<SmartDbContext> _db;
        private readonly CustomerSettings _customerSettings;

        public UserValidator(Lazy<SmartDbContext> db, CustomerSettings customerSettings, IdentityErrorDescriber errors = null)
            : base(errors)
        {
            _db = db;
            _customerSettings = customerSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        // UserName & Email validator
        async Task<IdentityResult> IUserValidator<Customer>.ValidateAsync(UserManager<Customer> manager, Customer user)
        {
            Guard.NotNull(manager, nameof(manager));
            Guard.NotNull(user, nameof(user));

            var registeredRole = _db.Value.CustomerRoles
                .AsNoTracking()
                .Where(x => x.SystemName == SystemCustomerRoleNames.Registered)
                .FirstOrDefault();

            if (registeredRole == null)
            {
                throw new InvalidOperationException(T("Admin.Customers.CustomerRoles.CannotFoundRole", "Registered"));
            }

            if (user.IsSearchEngineAccount())
            {
                return Failed(T("Account.Register.Errors.CannotRegisterSearchEngine"));
            }

            if (user.IsBackgroundTaskAccount())
            {
                return Failed(T("Account.Register.Errors.CannotRegisterTaskAccount"));
            }

            // INFO: (mh) (core) Will fail for _userManager.ResetPasswordAsync 
            // In this case the customer is IsRegistered = true
            // TODO: (mh) (core) Remove comment once reviewed by mc
            // RE: Reviewing this takes too much. Must discuss in personal. Please TBD.
            //if (user.IsRegistered())
            //{
            //    return Failed(T("Account.Register.Errors.AlreadyRegistered"));
            //}

            if (user.Email.IsEmpty() || !user.Email.IsEmail())
            {
                return Failed(Describer.InvalidEmail(user.Email));
            }

            // INFO: Unique emails & usernames are always required, because CustomerLoginType can be switched any time.
            var owner = await manager.FindByEmailAsync(user.Email);
            if (owner != null && owner.Id != user.Id)
            {
                return Failed(Describer.DuplicateEmail(user.Email));
            }

            var userName = user.Username;
            owner = await manager.FindByNameAsync(userName);
            if (owner != null && owner.Id != user.Id)
            {
                return Failed(Describer.DuplicateUserName(userName));
            }

            if (_customerSettings.CustomerLoginType != CustomerLoginType.Email)
            {
                if (userName.IsEmpty())
                {
                    return Failed(Describer.InvalidUserName(userName));
                }
                else if (manager.Options.User.AllowedUserNameCharacters.HasValue() &&
                    userName.Any(c => !manager.Options.User.AllowedUserNameCharacters.Contains(c)))
                {
                    return Failed(Describer.InvalidUserName(userName));
                }
            }

            return IdentityResult.Success;
        }

        // INFO: Password validation was configured with out-of-the-box settings of Identity Framework.
        // But since this custom UserValidator implements PasswordValidator we must implement ValidateAsync and return Success in order to not get duplicate error messages
        // Password validator
        public override Task<IdentityResult> ValidateAsync(UserManager<Customer> manager, Customer user, string password)
        {
            return Task.FromResult(IdentityResult.Success);
        }

        private static IdentityResult Failed(string message)
        {
            return message.IsEmpty() ? IdentityResult.Failed() : IdentityResult.Failed(new IdentityError { Description = message });
        }

        private static IdentityResult Failed(params IdentityError[] errors)
            => IdentityResult.Failed(errors);
    }
}

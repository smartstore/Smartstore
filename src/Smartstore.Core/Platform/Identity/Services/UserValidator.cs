using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Identity
{
    public class UserValidator : PasswordValidator<Customer>, IUserValidator<Customer>
    {
        private readonly CustomerSettings _customerSettings;

        public UserValidator(CustomerSettings customerSettings, IdentityErrorDescriber errors = null)
            : base(errors)
        {
            _customerSettings = customerSettings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        // UserName & Email validator
        async Task<IdentityResult> IUserValidator<Customer>.ValidateAsync(UserManager<Customer> manager, Customer user)
        {
            Guard.NotNull(manager, nameof(manager));
            Guard.NotNull(user, nameof(user));

            // TODO: (core) Check this very early (?)
            //var registeredRole = _customerService.GetCustomerRoleBySystemName(SystemCustomerRoleNames.Registered);
            //if (registeredRole == null)
            //{
            //    throw new SmartException(T("Admin.Customers.CustomerRoles.CannotFoundRole", "Registered"));
            //}

            if (user.IsSearchEngineAccount())
            {
                return Failed(T("Account.Register.Errors.CannotRegisterSearchEngine"));
            }

            if (user.IsBackgroundTaskAccount())
            {
                return Failed(T("Account.Register.Errors.CannotRegisterTaskAccount"));
            }

            if (user.IsRegistered())
            {
                return Failed(T("Account.Register.Errors.AlreadyRegistered"));
            }

            if (user.Email.IsEmpty() || !user.Email.IsEmail())
            {
                return Failed(Describer.InvalidEmail(user.Email));
            }

            if (manager.Options.User.RequireUniqueEmail)
            {
                var owner = await manager.FindByEmailAsync(user.Email);
                if (owner != null && owner.Id != user.Id)
                {
                    return Failed(Describer.DuplicateEmail(user.Email));
                }
            }

            if (_customerSettings.CustomerLoginType != CustomerLoginType.Email)
            {
                var userName = user.Username;

                if (userName.IsEmpty())
                {
                    return Failed(Describer.InvalidUserName(userName));
                }
                else if (!string.IsNullOrEmpty(manager.Options.User.AllowedUserNameCharacters) && 
                    userName.Any(c => !manager.Options.User.AllowedUserNameCharacters.Contains(c)))
                {
                    return Failed(Describer.InvalidUserName(userName));
                }
                else
                {
                    var owner = await manager.FindByNameAsync(userName);
                    if (owner != null && owner.Id != user.Id)
                    {
                        return Failed(Describer.DuplicateUserName(userName));
                    }
                }
            }

            return IdentityResult.Success;
        }

        // Password validator
        public override async Task<IdentityResult> ValidateAsync(UserManager<Customer> manager, Customer user, string password)
        {
            var result = await base.ValidateAsync(manager, user, password);
            if (!result.Succeeded)
            {
                return result;
            }

            // TODO: (mg) (core) Perform more app specific password validation

            return result;
        }

        private static IdentityResult Failed(string message)
        {
            return message.IsEmpty() ? IdentityResult.Failed() : IdentityResult.Failed(new IdentityError { Description = message });
        }

        private static IdentityResult Failed(params IdentityError[] errors) 
            => IdentityResult.Failed(errors);
    }
}

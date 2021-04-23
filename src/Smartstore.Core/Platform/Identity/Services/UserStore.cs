using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Identity
{
    public interface IUserStore : 
        IQueryableUserStore<Customer>,
        IUserEmailStore<Customer>,
        IUserRoleStore<Customer>,
        IUserPasswordStore<Customer>,
        IUserLoginStore<Customer>,
        IUserTwoFactorStore<Customer>,
        IUserTwoFactorRecoveryCodeStore<Customer>
    {
        /// <summary>
        /// Gets or sets a flag indicating if changes should be persisted after CreateAsync, UpdateAsync and DeleteAsync are called.
        /// </summary>
        /// <value>
        /// True if changes should be automatically persisted, otherwise false.
        /// </value>
        bool AutoSaveChanges { get; set; }
    }

    internal class UserStore : AsyncDbSaveHook<Customer>, IUserStore
    {
        private readonly SmartDbContext _db;
        private readonly Lazy<IGdprTool> _gdprTool;
        private readonly CustomerSettings _customerSettings;

        private readonly DbSet<Customer> _users;
        private readonly DbSet<CustomerRole> _roles;
        private readonly DbSet<CustomerRoleMapping> _roleMappings;

        public UserStore(SmartDbContext db, Lazy<IGdprTool> gdprTool, CustomerSettings customerSettings, IdentityErrorDescriber errorDescriber)
        {
            _db = db;
            _gdprTool = gdprTool;
            _customerSettings = customerSettings;
            
            ErrorDescriber = errorDescriber;

            _users = _db.Customers;
            _roles = _db.CustomerRoles;
            _roleMappings = _db.CustomerRoleMappings;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;
        public bool AutoSaveChanges { get; set; } = true;

        public void Dispose()
        {
        }

        #region Utils

        protected Task SaveChanges(CancellationToken cancellationToken)
        {
            return AutoSaveChanges ? _db.SaveChangesAsync(cancellationToken) : Task.CompletedTask;
        }

        protected Task<CustomerRole> FindRoleAsync(string normalizedRoleName, bool activeOnly = true, CancellationToken cancellationToken = default)
        {
            return _roles.SingleOrDefaultAsync(x => x.Name == normalizedRoleName && (!activeOnly || x.Active), cancellationToken);
        }

        protected Task<int?> FindRoleIdAsync(string normalizedRoleName, bool activeOnly = true, CancellationToken cancellationToken = default)
        {
            return _roles
                .Where(x => x.Name == normalizedRoleName && (!activeOnly || x.Active))
                .Select(x => (int?)x.Id)
                .SingleOrDefaultAsync(cancellationToken);
        }

        protected async Task<IEnumerable<CustomerRole>> GetOrLoadRolesAsync(Customer user, bool activeOnly = true)
        {
            await _db.LoadCollectionAsync(user, x => x.CustomerRoleMappings, false, q => q.Include(n => n.CustomerRole));

            return user.CustomerRoleMappings
                .Select(x => x.CustomerRole)
                .Where(x => !activeOnly || x.Active);
        }

        protected IdentityErrorDescriber ErrorDescriber { get; set; }

        protected IdentityResult Failed(Exception exception)
        {
            return exception == null ? IdentityResult.Failed() : IdentityResult.Failed(new IdentityError { Description = exception.Message });
        }

        protected IdentityResult Failed(string message)
        {
            return message.IsEmpty() ? IdentityResult.Failed() : IdentityResult.Failed(new IdentityError { Description = message });
        }

        protected IdentityResult Failed(params IdentityError[] errors)
            => IdentityResult.Failed(errors);

        #endregion

        #region Hook

        protected override Task<HookResult> OnUpdatedAsync(Customer entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            bool dirty = false;

            foreach (var entry in entries)
            {
                if (entry.IsSoftDeleted == true && entry.Entity is Customer customer)
                {
                    _gdprTool.Value.AnonymizeData(customer, x => x.LastIpAddress, IdentifierDataType.IpAddress);

                    // TODO: (core) Anonymize ForumPosts.IpAddress per external module.
                    //foreach (var post in user.ForumPosts)
                    //{
                    //    _gdprTool.Value.AnonymizeData(post, x => x.IPAddress, IdentifierDataType.IpAddress);
                    //}

                    // Customer Content
                    foreach (var item in customer.CustomerContent)
                    {
                        _gdprTool.Value.AnonymizeData(item, x => x.IpAddress, IdentifierDataType.IpAddress);
                    }

                    dirty = true;
                }
            }

            if (dirty)
            {
                await _db.SaveChangesAsync(cancelToken);
            }
        }

        #endregion

        #region IUserStore

        public IQueryable<Customer> Users => _users;

        public async Task<IdentityResult> CreateAsync(Customer user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));

            _users.Add(user);
            await SaveChanges(cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(Customer user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));

            if (user.IsSystemAccount)
            {
                throw new SmartException(string.Format("System customer account ({0}) cannot be deleted.", user.SystemName));
            }

            _db.Remove(user);

            try
            {
                await SaveChanges(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Failed(ErrorDescriber.ConcurrencyFailure());
            }

            return IdentityResult.Success;
        }

        public Task<Customer> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _users
                .IncludeCustomerRoles()
                .FindByIdAsync(userId.Convert<int>(), cancellationToken: cancellationToken)
                .AsTask();
        }

        public Task<Customer> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _users
                .IncludeCustomerRoles()
                .FirstOrDefaultAsync(x => x.Username == normalizedUserName, cancellationToken);
        }

        Task<string> IUserStore<Customer>.GetNormalizedUserNameAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            // TODO: (core) Add Customer.NormalizedUserName field or implement normalization somehow.
            return Task.FromResult(user.Username);
        }

        Task<string> IUserStore<Customer>.GetUserIdAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            return Task.FromResult(user.Id.ToString());
        }

        Task<string> IUserStore<Customer>.GetUserNameAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            return Task.FromResult(user.Username);
        }

        Task IUserStore<Customer>.SetNormalizedUserNameAsync(Customer user, string normalizedName, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            // TODO: (core) Add Customer.NormalizedUserName field or implement normalization somehow.
            user.Username = normalizedName;
            return Task.CompletedTask;
        }

        Task IUserStore<Customer>.SetUserNameAsync(Customer user, string userName, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            user.Username = userName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(Customer user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));

            // TODO: (core) Add Customer.ConcurrencyStamp field (?)
            //user.ConcurrencyStamp = Guid.NewGuid().ToString();
            _db.TryUpdate(user);

            try
            {
                await SaveChanges(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Failed(ErrorDescriber.ConcurrencyFailure());
            }

            return IdentityResult.Success;
        }

        #endregion

        #region IUserEmailStore

        public Task<Customer> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _users
                .IncludeCustomerRoles()
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        }

        Task IUserEmailStore<Customer>.SetEmailAsync(Customer user, string email, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));

            user.Email = email;
            return Task.CompletedTask;
        }

        Task<string> IUserEmailStore<Customer>.GetEmailAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            return Task.FromResult(user.Email);
        }

        Task<bool> IUserEmailStore<Customer>.GetEmailConfirmedAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            return Task.FromResult(user.Active);
        }

        Task IUserEmailStore<Customer>.SetEmailConfirmedAsync(Customer user, bool confirmed, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));

            user.Active = confirmed;

            if (confirmed)
            {
                user.GenericAttributes.AccountActivationToken = null;
            }

            return Task.CompletedTask;
        }

        Task<string> IUserEmailStore<Customer>.GetNormalizedEmailAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            return Task.FromResult(user.Email);
        }

        Task IUserEmailStore<Customer>.SetNormalizedEmailAsync(Customer user, string normalizedEmail, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            user.Email = normalizedEmail;
            return Task.CompletedTask;
        }

        #endregion

        #region IUserRoleStore

        public async Task AddToRoleAsync(Customer user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));
            Guard.NotEmpty(normalizedRoleName, nameof(normalizedRoleName));

            var roleId = await FindRoleIdAsync(normalizedRoleName, true, cancellationToken);

            if (roleId == null)
            {
                throw new InvalidOperationException($"Role '{normalizedRoleName}' does not exist.");
            }

            user.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerId = user.Id,
                CustomerRoleId = roleId.Value
            });
        }

        public async Task RemoveFromRoleAsync(Customer user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));
            Guard.NotEmpty(normalizedRoleName, nameof(normalizedRoleName));

            var roleId = await FindRoleIdAsync(normalizedRoleName, false, cancellationToken);

            if (roleId.HasValue)
            {
                var mapping = await _roleMappings.FirstOrDefaultAsync(x => x.CustomerRoleId == roleId.Value && x.CustomerId == user.Id, cancellationToken);
                if (mapping != null)
                {
                    _roleMappings.Remove(mapping);
                }
            }
        }

        public async Task<IList<string>> GetRolesAsync(Customer user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));

            return (await GetOrLoadRolesAsync(user, true)).Select(x => x.Name).ToList();
        }

        public async Task<bool> IsInRoleAsync(Customer user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));
            Guard.NotEmpty(normalizedRoleName, nameof(normalizedRoleName));

            var roleId = await FindRoleIdAsync(normalizedRoleName, false, cancellationToken);

            if (roleId.HasValue)
            {
                return await _roleMappings.AnyAsync(x => x.CustomerRoleId == roleId.Value && x.CustomerId == user.Id, cancellationToken);
            }

            return false;
        }

        public async Task<IList<Customer>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotEmpty(normalizedRoleName, nameof(normalizedRoleName));

            var roleId = await FindRoleIdAsync(normalizedRoleName, false, cancellationToken);

            if (roleId.HasValue)
            {
                return await _roleMappings
                    .Where(x => x.CustomerRoleId == roleId.Value)
                    .Select(x => x.Customer)
                    .ToListAsync(cancellationToken);
            }

            return new List<Customer>();
        }

        #endregion

        #region IUserPasswordStore

        public Task SetPasswordHashAsync(Customer user, string passwordHash, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(user, nameof(user));
            user.Password = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string> GetPasswordHashAsync(Customer user, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(user, nameof(user));
            return Task.FromResult(user.Password);
        }

        public Task<bool> HasPasswordAsync(Customer user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(user.Password != null);
        }

        #endregion

        #region IUserLoginStore

        // TODO: (core) Implement IUserLoginStore<Customer> in UserStore --> ExternalAuthenticationRecords

        public Task AddLoginAsync(Customer user, UserLoginInfo login, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task RemoveLoginAsync(Customer user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(Customer user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Customer> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IUserTwoFactorStore

        public Task SetTwoFactorEnabledAsync(Customer user, bool enabled, CancellationToken cancellationToken)
        {
            // TODO: (mh) (core) This isn't handled on Customer level. It's a global setting which can't be changed here. 
            return Task.CompletedTask;
        }

        public Task<bool> GetTwoFactorEnabledAsync(Customer user, CancellationToken cancellationToken)
        {
            return Task.FromResult(_customerSettings.UserRegistrationType == UserRegistrationType.EmailValidation);
        }

        #endregion

        #region IUserTwoFactorRecoveryCodeStore

        public Task ReplaceCodesAsync(Customer user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            user.GenericAttributes.PasswordRecoveryToken = recoveryCodes.FirstOrDefault();
            return Task.CompletedTask;
        }

        public Task<bool> RedeemCodeAsync(Customer user, string code, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            return Task.FromResult(user.GenericAttributes.PasswordRecoveryToken == code);
        }

        public Task<int> CountCodesAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user, nameof(user));
            return Task.FromResult(user.GenericAttributes.PasswordRecoveryToken.HasValue() ? 1 : 0);
        }

        #endregion
    }
}

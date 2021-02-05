using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Identity
{
    public interface IUserStore : 
        IQueryableUserStore<Customer>, 
        IQueryableRoleStore<CustomerRole>
    {
        /// <summary>
        /// Gets or sets a flag indicating if changes should be persisted after CreateAsync, UpdateAsync and DeleteAsync are called.
        /// </summary>
        /// <value>
        /// True if changes should be automatically persisted, otherwise false.
        /// </value>
        bool AutoSaveChanges { get; set; }
    }

    public class UserStore : Disposable, IUserStore
    {
        private readonly SmartDbContext _db;
        private readonly CustomerSettings _customerSettings;

        private readonly DbSet<Customer> _users;
        private readonly DbSet<CustomerRole> _roles;

        public UserStore(SmartDbContext db, CustomerSettings customerSettings, IdentityErrorDescriber errorDescriber)
        {
            _db = db;
            _customerSettings = customerSettings;
            
            ErrorDescriber = errorDescriber;

            _users = _db.Customers;
            _roles = _db.CustomerRoles;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;
        public ILogger Logger { get; set; } = NullLogger.Instance;
        public bool AutoSaveChanges { get; set; }

        protected IdentityErrorDescriber ErrorDescriber { get; set; }

        protected IdentityResult Failed(Exception exception)
        {
            return exception == null  ? IdentityResult.Failed() : IdentityResult.Failed(new IdentityError { Description = exception.Message });
        }

        protected IdentityResult Failed(string message)
        {
            return message.IsEmpty() ? IdentityResult.Failed() : IdentityResult.Failed(new IdentityError { Description = message });
        }

        protected IdentityResult Failed(params IdentityError[] errors) 
            => IdentityResult.Failed(errors);

        protected Task SaveChanges(CancellationToken cancellationToken)
        {
            return AutoSaveChanges ? _db.SaveChangesAsync(cancellationToken) : Task.CompletedTask;
        }

        protected override void OnDispose(bool disposing)
        {
        }

        #region IUserStore

        public IQueryable<Customer> Users => _users;

        public async Task<IdentityResult> CreateAsync(Customer user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));

            //// TODO: (core) Create a validator for these and put to UserManager's validators collection (IUserValidator<TUser>: must replace default registration)
            //if (user.Email.HasValue() && await _users.AnyAsync(x => x.Email == user.Email))
            //{
            //    return Failed(ErrorDescriber.DuplicateEmail(user.Email));
            //}

            //if (user.Username.HasValue() && _customerSettings.CustomerLoginType != CustomerLoginType.Email && await _users.AnyAsync(x => x.Username == user.Username))
            //{
            //    return Failed(ErrorDescriber.DuplicateUserName(user.Username));
            //}

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

            user.Deleted = true;
            _db.TryChangeState(user, EntityState.Modified);

            // TODO: (core) Soft delete customer and anonymize data with IGdprTool

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
                .Include(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole)
                .FindByIdAsync(userId.Convert<int>(), cancellationToken: cancellationToken)
                .AsTask();
        }

        public Task<Customer> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _users
                .Include(x => x.CustomerRoleMappings)
                .ThenInclude(x => x.CustomerRole)
                .FirstOrDefaultAsync(x => x.Username == normalizedUserName);
        }

        Task<string> IUserStore<Customer>.GetNormalizedUserNameAsync(Customer user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));

            // TODO: (core) Add Customer.NormalizedUserName field or implement normalization somehow.
            return Task.FromResult(user.Username);
        }

        Task<string> IUserStore<Customer>.GetUserIdAsync(Customer user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));

            return Task.FromResult(user.Id.ToString());
        }

        Task<string> IUserStore<Customer>.GetUserNameAsync(Customer user, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));

            return Task.FromResult(user.Username);
        }

        Task IUserStore<Customer>.SetNormalizedUserNameAsync(Customer user, string normalizedName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user, nameof(user));

            // TODO: (core) Add Customer.NormalizedUserName field or implement normalization somehow.
            //user.Username = normalizedName;
            return Task.CompletedTask;
        }

        Task IUserStore<Customer>.SetUserNameAsync(Customer user, string userName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
            _db.TryChangeState(user, EntityState.Modified);

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

        #region IRoleStore

        public IQueryable<CustomerRole> Roles => _roles;

        public async Task<IdentityResult> CreateAsync(CustomerRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(role, nameof(role));

            _roles.Add(role);
            await SaveChanges(cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(CustomerRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(role, nameof(role));

            // TODO: (core) Add CustomerRole.ConcurrencyStamp field (?)
            //role.ConcurrencyStamp = Guid.NewGuid().ToString();
            _db.TryChangeState(role, EntityState.Modified);

            try
            {
                await SaveChanges(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Failed(ErrorDescriber.ConcurrencyFailure());
            }

            return IdentityResult.Success;

            // TODO: (core) Invalidate PermissionService.PERMISSION_TREE_KEY by hook
        }

        public async Task<IdentityResult> DeleteAsync(CustomerRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(role, nameof(role));

            if (role.IsSystemRole)
            {
                throw new SmartException("System roles cannot be deleted");
            }

            _roles.Remove(role);

            try
            {
                await SaveChanges(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Failed(ErrorDescriber.ConcurrencyFailure());
            }

            return IdentityResult.Success;

            // TODO: (core) Invalidate PermissionService.PERMISSION_TREE_KEY by hook
        }

        Task<string> IRoleStore<CustomerRole>.GetRoleIdAsync(CustomerRole role, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(role, nameof(role));

            return Task.FromResult(role.Id.ToString());
        }

        Task<string> IRoleStore<CustomerRole>.GetRoleNameAsync(CustomerRole role, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(role, nameof(role));

            return Task.FromResult(role.Name);
        }

        Task IRoleStore<CustomerRole>.SetRoleNameAsync(CustomerRole role, string roleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(role, nameof(role));

            role.Name = roleName;
            return Task.CompletedTask;
        }

        Task<string> IRoleStore<CustomerRole>.GetNormalizedRoleNameAsync(CustomerRole role, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(role, nameof(role));

            // TODO: (core) Add CustomerRole.NormalizedUserName field or implement normalization somehow.
            return Task.FromResult(role.Name);
        }

        Task IRoleStore<CustomerRole>.SetNormalizedRoleNameAsync(CustomerRole role, string normalizedName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(role, nameof(role));

            // TODO: (core) Add Customer.NormalizedUserName field or implement normalization somehow.
            //role.Name = normalizedName;
            return Task.CompletedTask;
        }

        Task<CustomerRole> IRoleStore<CustomerRole>.FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _roles.FindByIdAsync(roleId.Convert<int>(), cancellationToken: cancellationToken).AsTask();
        }

        Task<CustomerRole> IRoleStore<CustomerRole>.FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _roles.FirstOrDefaultAsync(x => x.Name == normalizedRoleName);
        }

        #endregion
    }
}

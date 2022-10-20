using Microsoft.AspNetCore.Identity;
using Smartstore.Core.Data;

namespace Smartstore.Core.Identity
{
    public interface IRoleStore : IQueryableRoleStore<CustomerRole>
    {
        /// <summary>
        /// Gets or sets a flag indicating if changes should be persisted after CreateAsync, UpdateAsync and DeleteAsync are called.
        /// </summary>
        /// <value>
        /// True if changes should be automatically persisted, otherwise false.
        /// </value>
        bool AutoSaveChanges { get; set; }
    }

    internal class RoleStore : AsyncDbSaveHook<CustomerRole>, IRoleStore
    {
        private readonly Lazy<SmartDbContext> _db;

        public RoleStore(Lazy<SmartDbContext> db, IdentityErrorDescriber errorDescriber)
        {
            _db = db;

            ErrorDescriber = errorDescriber;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;
        public bool AutoSaveChanges { get; set; } = true;

        public void Dispose()
        {
        }

        protected IdentityErrorDescriber ErrorDescriber { get; set; }

        protected IdentityResult Failed(params IdentityError[] errors)
            => IdentityResult.Failed(errors);

        protected Task SaveChanges(CancellationToken cancellationToken)
        {
            return AutoSaveChanges ? _db.Value.SaveChangesAsync(cancellationToken) : Task.CompletedTask;
        }

        public IQueryable<CustomerRole> Roles => _db.Value.CustomerRoles;

        public Task<CustomerRole> FindByIdAsync(string roleId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _db.Value.CustomerRoles
                .Include(x => x.RuleSets)
                .FindByIdAsync(roleId.Convert<int>(), cancellationToken: cancellationToken)
                .AsTask();
        }

        public Task<CustomerRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _db.Value.CustomerRoles
                .Include(x => x.RuleSets)
                .FirstOrDefaultAsync(x => x.Name == normalizedRoleName, cancellationToken: cancellationToken);
        }

        public async Task<IdentityResult> CreateAsync(CustomerRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(role, nameof(role));

            _db.Value.CustomerRoles.Add(role);
            await SaveChanges(cancellationToken);
            return IdentityResult.Success;
        }

        public async Task<IdentityResult> UpdateAsync(CustomerRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(role, nameof(role));

            // TODO: (core) Add CustomerRole.ConcurrencyStamp field (?)
            //role.ConcurrencyStamp = Guid.NewGuid().ToString();
            _db.Value.TryUpdate(role);

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

        public async Task<IdentityResult> DeleteAsync(CustomerRole role, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(role, nameof(role));

            if (role.IsSystemRole)
            {
                throw new InvalidOperationException("System roles cannot be deleted");
            }

            _db.Value.CustomerRoles.Remove(role);

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

        Task<string> IRoleStore<CustomerRole>.GetRoleIdAsync(CustomerRole role, CancellationToken cancellationToken)
        {
            Guard.NotNull(role, nameof(role));
            return Task.FromResult(role.Id.ToString());
        }

        Task<string> IRoleStore<CustomerRole>.GetRoleNameAsync(CustomerRole role, CancellationToken cancellationToken)
        {
            Guard.NotNull(role, nameof(role));
            return Task.FromResult(role.Name);
        }

        Task IRoleStore<CustomerRole>.SetRoleNameAsync(CustomerRole role, string roleName, CancellationToken cancellationToken)
        {
            Guard.NotNull(role, nameof(role));
            role.Name = roleName;
            return Task.CompletedTask;
        }

        Task<string> IRoleStore<CustomerRole>.GetNormalizedRoleNameAsync(CustomerRole role, CancellationToken cancellationToken)
        {
            Guard.NotNull(role, nameof(role));
            // TODO: (core) Add CustomerRole.NormalizedUserName field or implement normalization somehow.
            return Task.FromResult(role.Name);
        }

        Task IRoleStore<CustomerRole>.SetNormalizedRoleNameAsync(CustomerRole role, string normalizedName, CancellationToken cancellationToken)
        {
            Guard.NotNull(role, nameof(role));
            // TODO: (core) Add Customer.NormalizedUserName field or implement normalization somehow.
            role.Name = normalizedName;
            return Task.CompletedTask;
        }
    }
}

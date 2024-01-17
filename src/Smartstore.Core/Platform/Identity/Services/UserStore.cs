using Microsoft.AspNetCore.Identity;
using Smartstore.Caching;
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
        IUserLoginStore<Customer>
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
        private readonly IRequestCache _requestCache;
        private readonly CustomerSettings _customerSettings;

        public UserStore(
            SmartDbContext db,
            Lazy<IGdprTool> gdprTool,
            IRequestCache requestCache,
            CustomerSettings customerSettings,
            IdentityErrorDescriber errorDescriber)
        {
            _db = db;
            _gdprTool = gdprTool;
            _requestCache = requestCache;
            _customerSettings = customerSettings;
            ErrorDescriber = errorDescriber;
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
            var cacheKey = $"roles.{normalizedRoleName}.{activeOnly}";
            var roleTask = _requestCache.GetAsync(cacheKey, () => 
            {
                var query = from x in _db.CustomerRoles
                            where
                                (x.SystemName == normalizedRoleName || x.Name == normalizedRoleName) &&
                                (!activeOnly || x.Active)
                            select x;

                return query.FirstOrDefaultAsync(cancellationToken);
            });

            return roleTask;
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

                    // Customer Content.
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

        public IQueryable<Customer> Users => _db.Customers;

        public async Task<IdentityResult> CreateAsync(Customer user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user);

            try
            {
                _db.Customers.Add(user);
                await SaveChanges(cancellationToken);
            }
            catch (Exception ex)
            {
                return Failed(ex);
            }

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(Customer user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user);

            if (user.IsSystemAccount)
            {
                throw new InvalidOperationException($"System customer account ({user.SystemName}) cannot be deleted.");
            }

            try
            {
                _db.Remove(user);
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

            return _db.Customers
                .IncludeCustomerRoles()
                .FindByIdAsync(userId.Convert<int>(), cancellationToken: cancellationToken)
                .AsTask();
        }

        public Task<Customer> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _db.Customers
                .IgnoreQueryFilters()
                .IncludeCustomerRoles()
                .FirstOrDefaultAsync(x => x.Username == normalizedUserName, cancellationToken);
        }

        Task<string> IUserStore<Customer>.GetNormalizedUserNameAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user);
            // TODO: (core) Add Customer.NormalizedUserName field or implement normalization somehow.
            return Task.FromResult(user.Username);
        }

        Task<string> IUserStore<Customer>.GetUserIdAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user);
            return Task.FromResult(user.Id.ToStringInvariant());
        }

        Task<string> IUserStore<Customer>.GetUserNameAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user);

            var userName = user.Username;
            if (string.IsNullOrEmpty(userName) && _customerSettings.CustomerLoginType != CustomerLoginType.Username)
            {
                userName = user.Email;
                user.Username = userName;
            }

            return Task.FromResult(userName);
        }

        Task IUserStore<Customer>.SetNormalizedUserNameAsync(Customer user, string normalizedName, CancellationToken cancellationToken)
        {
            Guard.NotNull(user);
            // TODO: (core) Add Customer.NormalizedUserName field or implement normalization somehow.
            user.Username = normalizedName;
            return Task.CompletedTask;
        }

        Task IUserStore<Customer>.SetUserNameAsync(Customer user, string userName, CancellationToken cancellationToken)
        {
            Guard.NotNull(user);
            user.Username = userName;
            return Task.CompletedTask;
        }

        public async Task<IdentityResult> UpdateAsync(Customer user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user);

            // TODO: (core) Add Customer.ConcurrencyStamp field (?)
            //user.ConcurrencyStamp = Guid.NewGuid().ToString();

            try
            {
                _db.TryUpdate(user);
                await SaveChanges(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Failed(ErrorDescriber.ConcurrencyFailure());
            }
            catch (Exception ex)
            {
                return Failed(ex);
            }

            return IdentityResult.Success;
        }

        #endregion

        #region IUserEmailStore

        public Task<Customer> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return _db.Customers
                .IgnoreQueryFilters()
                .IncludeCustomerRoles()
                .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        }

        Task IUserEmailStore<Customer>.SetEmailAsync(Customer user, string email, CancellationToken cancellationToken)
        {
            Guard.NotNull(user);
            user.Email = email;
            return Task.CompletedTask;
        }

        Task<string> IUserEmailStore<Customer>.GetEmailAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user);
            return Task.FromResult(user.Email);
        }

        Task<bool> IUserEmailStore<Customer>.GetEmailConfirmedAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user);
            return Task.FromResult(user.Active);
        }

        Task IUserEmailStore<Customer>.SetEmailConfirmedAsync(Customer user, bool confirmed, CancellationToken cancellationToken)
        {
            Guard.NotNull(user);

            user.Active = confirmed;

            if (confirmed)
            {
                user.GenericAttributes.AccountActivationToken = null;
            }

            return Task.CompletedTask;
        }

        Task<string> IUserEmailStore<Customer>.GetNormalizedEmailAsync(Customer user, CancellationToken cancellationToken)
        {
            Guard.NotNull(user);
            return Task.FromResult(user.Email);
        }

        Task IUserEmailStore<Customer>.SetNormalizedEmailAsync(Customer user, string normalizedEmail, CancellationToken cancellationToken)
        {
            Guard.NotNull(user);
            user.Email = normalizedEmail;
            return Task.CompletedTask;
        }

        #endregion

        #region IUserRoleStore

        public async Task AddToRoleAsync(Customer user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user);
            Guard.NotEmpty(normalizedRoleName);

            var role = await FindRoleAsync(normalizedRoleName, true, cancellationToken);
            if (role == null)
            {
                throw new InvalidOperationException($"Role '{normalizedRoleName}' does not exist.");
            }

            user.CustomerRoleMappings.Add(new CustomerRoleMapping
            {
                CustomerId = user.Id,
                CustomerRoleId = role.Id
            });
        }

        public async Task RemoveFromRoleAsync(Customer user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user);
            Guard.NotEmpty(normalizedRoleName);

            var role = await FindRoleAsync(normalizedRoleName, false, cancellationToken);
            
            if (role == null)
            {
                return;
            }

            if (_db.IsCollectionLoaded(user, c => c.CustomerRoleMappings))
            {
                var mappings = user.CustomerRoleMappings.Where(x => x.CustomerRoleId == role.Id);
                _db.CustomerRoleMappings.RemoveRange(mappings);
            }
            else
            {
                var mappings = await _db.CustomerRoleMappings
                    .Where(x => x.CustomerId == user.Id && x.CustomerRoleId == role.Id)
                    .ToListAsync(cancellationToken);
                _db.CustomerRoleMappings.RemoveRange(mappings);
            }
        }

        public async Task<IList<string>> GetRolesAsync(Customer user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user);

            return (await GetOrLoadRolesAsync(user, true))
                .Select(x => x.SystemName.NullEmpty() ?? x.Name)
                .ToList();
        }

        public async Task<bool> IsInRoleAsync(Customer user, string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotNull(user);
            Guard.NotEmpty(normalizedRoleName);

            var role = await FindRoleAsync(normalizedRoleName, false, cancellationToken);

            if (role == null)
            {
                return false;
            }

            if (_db.IsCollectionLoaded(user, c => c.CustomerRoleMappings))
            {
                return user.CustomerRoleMappings.Any(x => x.CustomerRoleId == role.Id);
            }
            else
            {
                return await _db.CustomerRoleMappings.AnyAsync(x => x.CustomerId == user.Id && x.CustomerRoleId == role.Id, cancellationToken);
            } 
        }

        public async Task<IList<Customer>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Guard.NotEmpty(normalizedRoleName);

            var role = await FindRoleAsync(normalizedRoleName, false, cancellationToken);

            if (role == null)
            {
                return new List<Customer>();
            }

            return await _db.CustomerRoleMappings
                .Where(x => x.CustomerRoleId == role.Id)
                .Select(x => x.Customer)
                .ToListAsync(cancellationToken);
        }

        #endregion

        #region IUserPasswordStore

        public Task SetPasswordHashAsync(Customer user, string passwordHash, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(user);
            user.Password = passwordHash;
            return Task.CompletedTask;
        }

        public Task<string> GetPasswordHashAsync(Customer user, CancellationToken cancellationToken = default)
        {
            Guard.NotNull(user);
            return Task.FromResult(user.Password);
        }

        public Task<bool> HasPasswordAsync(Customer user, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(user.Password != null);
        }

        #endregion

        #region IUserLoginStore

        public Task AddLoginAsync(Customer user, UserLoginInfo login, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _db.ExternalAuthenticationRecords.Add(new ExternalAuthenticationRecord
            {
                CustomerId = user.Id,
                Email = user.Email,
                ExternalIdentifier = login.ProviderKey,
                ProviderSystemName = TranslateProviderToSystemName(login.LoginProvider)
            });

            return SaveChanges(cancellationToken);
        }

        public async Task RemoveLoginAsync(Customer user, string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var providerSystemName = TranslateProviderToSystemName(loginProvider);
            var record = await _db.ExternalAuthenticationRecords
                .Where(x => x.CustomerId == user.Id 
                    && x.ProviderSystemName == providerSystemName
                    && x.ExternalIdentifier == providerKey)
                .FirstOrDefaultAsync(cancellationToken);

            if (record != null)
            {
                _db.ExternalAuthenticationRecords.Remove(record);
                await SaveChanges(cancellationToken);
            }
        }

        public async Task<IList<UserLoginInfo>> GetLoginsAsync(Customer user, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var records = await _db.ExternalAuthenticationRecords
                .Where(x => x.CustomerId == user.Id)
                .ToListAsync(cancellationToken);

            var infos = records.Select(x =>
            {
                return new UserLoginInfo
                (
                    TranslateSystemNameToProvider(x.ProviderSystemName),
                    x.ExternalDisplayIdentifier,
                    x.ExternalIdentifier
                );
            }).ToList();

            return infos;
        }

        public async Task<Customer> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var providerSystemName = TranslateProviderToSystemName(loginProvider);
            var record = await _db.ExternalAuthenticationRecords
                .FirstOrDefaultAsync(x => x.ProviderSystemName == providerSystemName
                    && x.ExternalIdentifier == providerKey, cancellationToken);

            if (record != null)
            {
                return await _db.Customers
                    .Where(x => x.Id == record.CustomerId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            return null;
        }

        #endregion

        #region Helpers

        public static string TranslateProviderToSystemName(string provider)
        {
            // INFO: clarification. This maps in the opposite direction than expected, from new to old, so that
            // ExternalAuthenticationRecord.ProviderSystemName always contains the old system name, even for new logins.
            // This means that the database contains "Smartstore.FacebookAuth" or "SmartStore.FacebookAuth", but never "Facebook" or "Smartstore.Facebook.Auth".

            // TODO: (mh) (core) This may change in future as systemnames will probably be changing.
            // But for now we keep it this way for compatibility.
            switch (provider.EmptyNull().ToLowerInvariant())
            {
                case "facebook":
                    return "Smartstore.FacebookAuth";
                case "twitter":
                    return "Smartstore.TwitterAuth";
                case "google":
                    return "Smartstore.GoogleAuth";
                default:
                    return provider;
            }
        }

        public static string TranslateSystemNameToProvider(string systemName)
        {
            // TODO: (mh) (core) This may change in future as systemnames will probably be changing.
            // But for now we keep it this way for compatibility.
            switch (systemName.EmptyNull().ToLowerInvariant())
            {
                case "smartstore.facebookauth":
                    return "Facebook";
                case "smartstore.twitterauth":
                    return "Twitter";
                case "smartstore.googleauth":
                    return "Google";
                default:
                    return systemName;
            }
        }

        #endregion
    }
}

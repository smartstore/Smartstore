using System.Text;
using Smartstore.Caching;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using EState = Smartstore.Data.EntityState;

namespace Smartstore.Core.Security
{
    public partial class PermissionService : AsyncDbSaveHook<CustomerRole>, IPermissionService
    {
        // {0} = roleId
        private readonly static CompositeFormat PERMISSION_TREE_KEY = CompositeFormat.Parse("permission:tree-{0}");
        internal const string PERMISSION_TREE_PATTERN_KEY = "permission:tree-*";

        private static readonly Dictionary<string, string> _displayNameResourceKeys = new()
        {
            { "read", "Common.Read" },
            { "update", "Common.Edit" },
            { "create", "Common.Create" },
            { "delete", "Common.Delete" },
            { "catalog", "Admin.Catalog" },
            { "product", "Admin.Catalog.Products" },
            { "category", "Admin.Catalog.Categories" },
            { "manufacturer", "Admin.Catalog.Manufacturers" },
            { "variant", "Admin.Catalog.Attributes.ProductAttributes" },
            { "attribute", "Admin.Catalog.Attributes.SpecificationAttributes" },
            { "customer", "Admin.Customers" },
            { "impersonate", "Admin.Customers.Customers.Impersonate" },
            { "role", "Admin.Customers.CustomerRoles" },
            { "order", "Admin.Orders" },
            { "giftcard", "Admin.GiftCards" },
            { "notify", "Common.Notify" },
            { "returnrequest", "Admin.ReturnRequests" },
            { "accept", "Admin.ReturnRequests.Accept" },
            { "promotion", "Admin.Catalog.Products.Promotion" },
            { "affiliate", "Admin.Affiliates" },
            { "campaign", "Admin.Promotions.Campaigns" },
            { "discount", "Admin.Promotions.Discounts" },
            { "newsletter", "Admin.Promotions.NewsletterSubscriptions" },
            { "cms", "Admin.ContentManagement" },
            { "poll", "Admin.ContentManagement.Polls" },
            { "news", "Admin.ContentManagement.News" },
            { "blog", "Admin.ContentManagement.Blog" },
            { "widget", "Admin.ContentManagement.Widgets" },
            { "topic", "Admin.ContentManagement.Topics" },
            { "menu", "Admin.ContentManagement.Menus" },
            { "forum", "Admin.ContentManagement.Forums" },
            { "messagetemplate", "Admin.ContentManagement.MessageTemplates" },
            { "configuration", "Admin.Configuration" },
            { "country", "Admin.Configuration.Countries" },
            { "language", "Admin.Configuration.Languages" },
            { "setting", "Admin.Configuration.Settings" },
            { "paymentmethod", "Admin.Configuration.Payment.Methods" },
            { "activate", "Admin.Common.Activate" },
            { "authentication", "Admin.Configuration.ExternalAuthenticationMethods" },
            { "currency", "Admin.Configuration.Currencies" },
            { "deliverytime", "Admin.Configuration.DeliveryTimes" },
            { "theme", "Admin.Configuration.Themes" },
            { "measure", "Admin.Configuration.Measures.Dimensions" },
            { "activitylog", "Admin.Configuration.ActivityLog.ActivityLogType" },
            { "acl", "Admin.Configuration.ACL" },
            { "emailaccount", "Admin.Configuration.EmailAccounts" },
            { "store", "Admin.Common.Stores" },
            { "shipping", "Admin.Configuration.Shipping.Methods" },
            { "tax", "Admin.Configuration.Tax.Providers" },
            { "plugin", "Admin.Configuration.Plugins" },
            { "upload", "Common.Upload" },
            { "install", "Admin.Configuration.Plugins.Fields.Install" },
            { "license", "Admin.Common.License" },
            { "export", "Common.Export" },
            { "execute", "Admin.Common.Go" },
            { "import", "Common.Import" },
            { "system", "Admin.System" },
            { "log", "Admin.System.Log" },
            { "message", "Admin.System.QueuedEmails" },
            { "send", "Common.Send" },
            { "maintenance", "Admin.System.Maintenance" },
            { "scheduletask", "Admin.System.ScheduleTasks" },
            { "urlrecord", "Admin.System.SeNames" },
            { "cart", "ShoppingCart" },
            { "checkoutattribute", "Admin.Catalog.Attributes.CheckoutAttributes" },
            { "media", "Admin.Plugins.KnownGroup.Media" },
            { "download", "Common.Downloads" },
            { "productreview", "Admin.Catalog.ProductReviews" },
            { "approve", "Common.Approve" },
            { "rule", "Common.Rules" },
        };

        private readonly SmartDbContext _db;
        private readonly Lazy<IWorkContext> _workContext;
        private readonly ILocalizationService _localizationService;
        private readonly ICacheManager _cache;

        private string _hookErrorMessage;

        public PermissionService(
            SmartDbContext db,
            Lazy<IWorkContext> workContext,
            ILocalizationService localizationService,
            ICacheManager cache)
        {
            _db = db;
            _workContext = workContext;
            _localizationService = localizationService;
            _cache = cache;
        }

        #region Hook

        protected override Task<HookResult> OnUpdatedAsync(CustomerRole entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        protected override Task<HookResult> OnDeletedAsync(CustomerRole entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.Entity is CustomerRole role && role.IsSystemRole && entry.InitialState == EState.Deleted)
            {
                _hookErrorMessage = $"System customer role '{role.SystemName ?? role.Name}' cannot not be deleted.";
                entry.State = EState.Unchanged;
            }

            return Task.FromResult(HookResult.Ok);
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_hookErrorMessage.HasValue())
            {
                var message = new string(_hookErrorMessage);
                _hookErrorMessage = null;

                throw new HookException(message);
            }

            return Task.CompletedTask;
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var roleIds = entries
                .Select(x => x.Entity)
                .OfType<CustomerRole>()
                .Select(x => x.Id)
                .Distinct()
                .ToArray();

            foreach (var roleId in roleIds)
            {
                await _cache.RemoveAsync(PERMISSION_TREE_KEY.FormatInvariant(roleId));
            }
        }

        #endregion

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public virtual bool Authorize(string permissionSystemName, Customer customer = null, bool allowByDescendantPermission = false)
        {
            if (string.IsNullOrEmpty(permissionSystemName))
            {
                return false;
            }

            customer ??= _workContext.Value.CurrentCustomer;

            var cacheKey = $"permission:{customer.Id}.{allowByDescendantPermission}.{permissionSystemName}";

            var authorized = _cache.Get(cacheKey, o =>
            {
                o.ExpiresIn(TimeSpan.FromSeconds(30));

                var roles = customer.CustomerRoleMappings
                    .Where(x => x.CustomerRole?.Active ?? false)
                    .Select(x => x.CustomerRole)
                    .ToArray();

                foreach (var role in roles)
                {
                    var tree = GetPermissionTree(role);

                    if (AuthorizeInternal(tree, permissionSystemName, allowByDescendantPermission))
                    {
                        return true;
                    }
                }

                return false;
            });

            return authorized;
        }

        public virtual async Task<bool> AuthorizeAsync(string permissionSystemName, Customer customer = null, bool allowByDescendantPermission = false)
        {
            if (string.IsNullOrEmpty(permissionSystemName))
            {
                return false;
            }

            customer ??= _workContext.Value.CurrentCustomer;

            var cacheKey = $"permission:{customer.Id}.{allowByDescendantPermission}.{permissionSystemName}";

            var authorized = await _cache.GetAsync(cacheKey, async o =>
            {
                o.ExpiresIn(TimeSpan.FromSeconds(30));

                var roles = customer.CustomerRoleMappings
                    .Where(x => x.CustomerRole?.Active ?? false)
                    .Select(x => x.CustomerRole)
                    .ToArray();

                foreach (var role in roles)
                {
                    var tree = await GetPermissionTreeAsync(role);

                    if (AuthorizeInternal(tree.Permissions, permissionSystemName, allowByDescendantPermission))
                    {
                        return true;
                    }
                }

                return false;
            }, independent: true, allowRecursion: true);

            return authorized;
        }

        private TreeNode<IPermissionNode> GetPermissionTree(CustomerRole role)
        {
            Guard.NotNull(role, nameof(role));

            var tree = _cache.Get(PERMISSION_TREE_KEY.FormatInvariant(role.Id), () =>
            {
                var root = new TreeNode<IPermissionNode>(new PermissionNode());

                var allPermissions = _db.PermissionRecords
                    .AsNoTracking()
                    .Include(x => x.PermissionRoleMappings)
                    .ToList();

                AddPermissions(root, GetChildren(null, allPermissions), allPermissions, role);

                return root;
            });

            return tree;
        }

        public virtual async Task<PermissionTree> GetPermissionTreeAsync(CustomerRole role, bool addDisplayNames = false)
        {
            Guard.NotNull(role, nameof(role));

            var tree = await _cache.GetAsync(PERMISSION_TREE_KEY.FormatInvariant(role.Id), async () =>
            {
                var root = new TreeNode<IPermissionNode>(new PermissionNode());

                var allPermissions = await _db.PermissionRecords
                    .AsNoTracking()
                    .Include(x => x.PermissionRoleMappings)
                    .ToListAsync();

                await AddPermissions(root, GetChildren(null, allPermissions), allPermissions, null, role);

                return root;
            });

            if (addDisplayNames)
            {
                var languageId = _workContext.Value.WorkingLanguage.Id;
                var displayNamesLookup = await GetDisplayNameLookup(languageId);

                return new PermissionTree(tree, displayNamesLookup, languageId);
            }

            return new PermissionTree(tree);
        }

        public virtual async Task<PermissionTree> BuildCustomerPermissionTreeAsync(Customer customer, bool addDisplayNames = false)
        {
            Guard.NotNull(customer, nameof(customer));

            var tree = new TreeNode<IPermissionNode>(new PermissionNode());
            var allPermissions = await _db.PermissionRecords
                .AsNoTracking()
                .ToListAsync();

            await AddPermissions(tree, GetChildren(null, allPermissions), allPermissions, customer, null);

            if (addDisplayNames)
            {
                var languageId = _workContext.Value.WorkingLanguage.Id;
                var displayNamesLookup = await GetDisplayNameLookup(languageId);

                return new PermissionTree(tree, displayNamesLookup, languageId);
            }

            return new PermissionTree(tree);
        }

        public virtual async Task<Dictionary<string, string>> GetAllSystemNamesAsync()
        {
            var result = new Dictionary<string, string>();
            var resourcesLookup = await GetDisplayNameLookup(_workContext.Value.WorkingLanguage.Id);

            var systemNames = await _db.PermissionRecords
                .AsQueryable()
                .Select(x => x.SystemName)
                .ToListAsync();

            foreach (var systemName in systemNames)
            {
                var safeSytemName = systemName.EmptyNull().ToLower();
                var tokens = safeSytemName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

                result[safeSytemName] = GetDisplayName(tokens, resourcesLookup);
            }

            return result;
        }

        public virtual async Task<string> GetDisplayNameAsync(string permissionSystemName)
        {
            var tokens = permissionSystemName.EmptyNull().ToLower().Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Any())
            {
                var resourcesLookup = await GetDisplayNameLookup(_workContext.Value.WorkingLanguage.Id);

                return GetDisplayName(tokens, resourcesLookup);
            }

            return string.Empty;
        }

        public virtual async Task<string> GetUnauthorizedMessageAsync(string permissionSystemName)
        {
            var displayName = await GetDisplayNameAsync(permissionSystemName);
            var message = _localizationService.GetResource("Admin.AccessDenied.DetailedDescription");

            return message.FormatInvariant(displayName.NaIfEmpty(), permissionSystemName.NaIfEmpty());
        }

        public virtual async Task InstallPermissionsAsync(IPermissionProvider[] permissionProviders, bool removeUnusedPermissions = false)
        {
            if (!(permissionProviders?.Any() ?? false))
            {
                return;
            }

            var allPermissionNames = await _db.PermissionRecords
                .AsQueryable()
                .Select(x => x.SystemName)
                .ToListAsync();

            Dictionary<string, CustomerRole> existingRoles = null;
            var existing = new HashSet<string>(allPermissionNames, StringComparer.InvariantCultureIgnoreCase);
            var added = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var providerPermissions = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            var log = existing.Any();
            var clearCache = false;

            if (existing.Any())
            {
                var permissionsMigrated = existing.Contains(Permissions.System.AccessShop) && !existing.Contains("PublicStoreAllowNavigation");
                if (!permissionsMigrated)
                {
                    // Migrations must have been completed before permissions can be added or deleted.
                    return;
                }
            }

            try
            {
                using (var scope = new DbContextScope(_db, minHookImportance: HookImportance.Important))
                {
                    // Add new permissions.
                    foreach (var provider in permissionProviders)
                    {
                        try
                        {
                            var systemNames = provider.GetPermissions().Select(x => x.SystemName);
                            var missingSystemNames = systemNames.Except(existing);

                            if (removeUnusedPermissions)
                            {
                                providerPermissions.AddRange(systemNames);
                            }

                            if (missingSystemNames.Any())
                            {
                                var defaultPermissions = provider.GetDefaultPermissions();
                                foreach (var systemName in missingSystemNames)
                                {
                                    var roleNames = defaultPermissions
                                        .Where(x => x.PermissionRecords.Any(y => y.SystemName == systemName))
                                        .Select(x => x.CustomerRoleSystemName);

                                    var newPermission = new PermissionRecord { SystemName = systemName };

                                    foreach (var roleName in new HashSet<string>(roleNames, StringComparer.InvariantCultureIgnoreCase))
                                    {
                                        if (existingRoles == null)
                                        {
                                            existingRoles = new Dictionary<string, CustomerRole>();

                                            var rolesPager = _db.CustomerRoles
                                                .AsNoTracking()
                                                .Where(x => !string.IsNullOrEmpty(x.SystemName))
                                                .ToFastPager(500);

                                            while ((await rolesPager.ReadNextPageAsync<CustomerRole>()).Out(out var roles))
                                            {
                                                roles.Each(x => existingRoles[x.SystemName] = x);
                                            }
                                        }

                                        if (!existingRoles.TryGetValue(roleName, out var role))
                                        {
                                            role = new CustomerRole
                                            {
                                                Active = true,
                                                Name = roleName,
                                                SystemName = roleName
                                            };

                                            _db.CustomerRoles.Add(role);
                                            await scope.CommitAsync();

                                            existingRoles[roleName] = role;
                                        }

                                        newPermission.PermissionRoleMappings.Add(new PermissionRoleMapping
                                        {
                                            Allow = true,
                                            CustomerRoleId = role.Id
                                        });
                                    }

                                    _db.PermissionRecords.Add(newPermission);

                                    clearCache = true;
                                    added.Add(newPermission.SystemName);
                                    existing.Add(newPermission.SystemName);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);
                        }
                    }

                    await scope.CommitAsync();

                    if (log && added.Any())
                    {
                        var message = _localizationService.GetResource("Admin.Permissions.AddedPermissions");
                        Logger.Info(message.FormatInvariant(string.Join(", ", added)));
                    }

                    // Remove permissions no longer supported by providers.
                    if (removeUnusedPermissions)
                    {
                        var toDelete = existing.Except(providerPermissions).ToList();
                        if (toDelete.Any())
                        {
                            clearCache = true;

                            foreach (var chunk in toDelete.Chunk(500))
                            {
                                await _db.PermissionRecords
                                    .AsQueryable()
                                    .Where(x => chunk.Contains(x.SystemName))
                                    .ExecuteDeleteAsync();
                            }

                            if (log)
                            {
                                var message = _localizationService.GetResource("Admin.Permissions.RemovedPermissions");
                                Logger.Info(message.FormatInvariant(string.Join(", ", toDelete)));
                            }
                        }
                    }
                }
            }
            finally
            {
                if (clearCache)
                {
                    await _cache.RemoveByPatternAsync(PERMISSION_TREE_PATTERN_KEY);
                }
            }
        }

        #region Utilities

        private static bool AuthorizeInternal(TreeNode<IPermissionNode> tree, string permissionSystemName, bool allowByDescendantPermission)
        {
            var node = tree.SelectNodeById(permissionSystemName);
            if (node == null)
            {
                return false;
            }

            if (allowByDescendantPermission && FindAllowByDescendant(node))
            {
                return true;
            }

            while (node != null && !node.Value.Allow.HasValue)
            {
                node = node.Parent;
            }

            if (node?.Value?.Allow ?? false)
            {
                return true;
            }

            return false;

            static bool FindAllowByDescendant(TreeNode<IPermissionNode> n)
            {
                if (n?.Value?.Allow ?? false)
                {
                    return true;
                }

                if (n.HasChildren)
                {
                    foreach (var child in n.Children)
                    {
                        if (FindAllowByDescendant(child))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private static IEnumerable<PermissionRecord> GetChildren(PermissionRecord permission, IEnumerable<PermissionRecord> allPermissions)
        {
            if (permission == null)
            {
                // Get root permissions.
                return allPermissions.Where(x => !x.SystemName.Contains('.'));
            }
            else
            {
                // Get children.
                var tmpPath = permission.SystemName.EnsureEndsWith(".");

                return allPermissions.Where(x => x.SystemName.StartsWith(tmpPath) && x.SystemName.IndexOf('.', tmpPath.Length) == -1);
            }
        }

        private static void AddPermissions(
            TreeNode<IPermissionNode> parent,
            IEnumerable<PermissionRecord> toAdd,
            List<PermissionRecord> allPermissions,
            CustomerRole role)
        {
            foreach (var entity in toAdd)
            {
                var mapping = entity.PermissionRoleMappings.FirstOrDefault(x => x.CustomerRoleId == role.Id);

                var newNode = parent.Append(new PermissionNode
                {
                    PermissionRecordId = entity.Id,
                    Allow = mapping?.Allow ?? null, // null = inherit
                    SystemName = entity.SystemName
                });

                AddPermissions(newNode, GetChildren(entity, allPermissions), allPermissions, role);
            }
        }

        private async Task AddPermissions(
            TreeNode<IPermissionNode> parent,
            IEnumerable<PermissionRecord> toAdd,
            List<PermissionRecord> allPermissions,
            Customer customer,
            CustomerRole role)
        {
            foreach (var entity in toAdd)
            {
                // null = inherit
                bool? allow = null;

                if (role != null)
                {
                    var mapping = entity.PermissionRoleMappings.FirstOrDefault(x => x.CustomerRoleId == role.Id);
                    allow = mapping?.Allow ?? null;
                }
                else
                {
                    allow = await AuthorizeAsync(entity.SystemName, customer);
                }

                var newNode = parent.Append(new PermissionNode
                {
                    PermissionRecordId = entity.Id,
                    Allow = allow,
                    SystemName = entity.SystemName
                });

                await AddPermissions(newNode, GetChildren(entity, allPermissions), allPermissions, customer, role);
            }
        }

        private static string GetDisplayName(string[] tokens, Dictionary<string, string> namesLookup)
        {
            var displayName = string.Empty;

            if (tokens?.Any() ?? false)
            {
                foreach (var token in tokens)
                {
                    if (displayName.Length > 0)
                    {
                        displayName += " » ";
                    }

                    displayName += GetDisplayName(token, namesLookup) ?? token ?? string.Empty;
                }
            }

            return displayName;
        }

        internal static string GetDisplayName(string token, IReadOnlyDictionary<string, string> namesLookup)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            // Try known token of default permissions.
            if (_displayNameResourceKeys.TryGetValue(token, out string key) && namesLookup.TryGetValue(key, out string name))
            {
                return name;
            }

            // Unknown token. Try to find resource by name convention.
            key = "Permissions.DisplayName." + token.Replace("-", "");
            if (namesLookup.TryGetValue(key, out name))
            {
                return name;
            }

            // Try resource provided by plugin.
            key = "Plugins." + key;
            if (namesLookup.TryGetValue(key, out name))
            {
                return name;
            }

            return null;
        }

        private async Task<Dictionary<string, string>> GetDisplayNameLookup(int languageId)
        {
            var displayNames = await _cache.GetAsync("permission:displayname-" + languageId, async o =>
            {
                o.ExpiresIn(TimeSpan.FromDays(1));

                var allKeys = _displayNameResourceKeys.Select(x => x.Value);

                var resources = await _db.LocaleStringResources
                    .AsNoTracking()
                    .Where(x => x.LanguageId == languageId &&
                        (x.ResourceName.StartsWith("Permissions.DisplayName.")
                        || x.ResourceName.StartsWith("Plugins.Permissions.DisplayName.")
                        || x.ResourceName.StartsWith("Modules.Permissions.DisplayName.")
                        || allKeys.Contains(x.ResourceName)) &&
                        !string.IsNullOrEmpty(x.ResourceValue))
                    .ToListAsync();

                return resources.ToDictionarySafe(x => x.ResourceName, x => x.ResourceValue, StringComparer.OrdinalIgnoreCase);
            });

            return displayNames;
        }

        #endregion
    }
}

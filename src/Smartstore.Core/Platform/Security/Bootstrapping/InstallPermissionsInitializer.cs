using Microsoft.AspNetCore.Http;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Engine.Initialization;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Bootstrapping
{
    internal class InstallPermissionsInitializer : IApplicationInitializer
    {
        private readonly SmartDbContext _db;
        private readonly IPermissionService _permissionService;
        private readonly ITypeScanner _typeScanner;

        public InstallPermissionsInitializer(SmartDbContext db, IPermissionService permissionService, ITypeScanner typeScanner)
        {
            _db = db;
            _permissionService = permissionService;
            _typeScanner = typeScanner;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;

        public int Order => 0;
        public bool ThrowOnError => true;
        public int MaxAttempts => 1;

        public async Task InitializeAsync(HttpContext httpContext)
        {
            var removeUnusedPermissions = true;
            var providers = new List<IPermissionProvider>();

            if (ModularState.Instance.HasChanged || !await _db.PermissionRecords.AnyAsync())
            {
                // INFO: even if no module has changed: directly after a DB migration this code block MUST run. It seems awkward
                // that pending migrations exist when binaries has not changed. But after a manual DB reset for a migration rerun
                // nobody touches the binaries usually.

                // Core permission provider and all module providers.
                var types = _typeScanner.FindTypes<IPermissionProvider>().ToList();
                foreach (var type in types)
                {
                    if (Activator.CreateInstance(type) is IPermissionProvider provider)
                    {
                        providers.Add(provider);
                    }
                    else
                    {
                        removeUnusedPermissions = false;
                        Logger.Warn($"Cannot create instance of {nameof(IPermissionProvider)} {type.Name.NaIfEmpty()}.");
                    }
                }
            }
            else
            {
                // Always check core permission provider.
                providers.Add(new StandardPermissionProvider());

                // Keep unused permissions in database (has no negative effects) as long as at least one module changed.
                removeUnusedPermissions = false;
            }

            await _permissionService.InstallPermissionsAsync(providers.ToArray(), removeUnusedPermissions);
        }

        public Task OnFailAsync(Exception exception, bool willRetry)
            => Task.CompletedTask;
    }
}

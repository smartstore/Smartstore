using Smartstore.IO;
using Smartstore.Utilities;

namespace Smartstore.Engine.Modularity
{
    public class ModularState
    {
        #region ModulesHasher

        class ModulesHasher : DirectoryHasher
        {
            private readonly IApplicationContext _appContext;

            public ModulesHasher(IApplicationContext appContext)
                : base(appContext.ModulesRoot.GetDirectory(""), appContext.TenantRoot.GetDirectory(""))
            {
                _appContext = appContext;
            }

            protected override int ComputeHash()
            {
                var hashCombiner = HashCodeCombiner.Start();

                // Add each *.dll and module.json file of compatible modules.
                var arrModules = _appContext.ModuleCatalog.GetInstalledModules()
                    .OrderBy(x => x.SystemName)
                    .ToArray();

                foreach (var m in arrModules)
                {
                    var manifestFile = new FileInfo(Path.Combine(m.PhysicalPath, "module.json"));

                    hashCombiner.Add(manifestFile);

                    var dir = new DirectoryInfo(m.PhysicalPath);
                    var dlls = dir.GetFiles("*.dll", SearchOption.TopDirectoryOnly);
                    dlls.Each(x => hashCombiner.Add(x));
                }

                return hashCombiner.CombinedHash;
            }
        }

        #endregion

        const string FileName = "InstalledModules.txt";
        const string LegacyFileName = "InstalledPlugins.txt";
        const string PendingModulesFileName = "PendingModules.txt";

        private ModulesHasher _hasher;
        private static ModularState _instance;
        private readonly static object _lock = new();

        public static ModularState Instance
        {
            get => LazyInitializer.EnsureInitialized(ref _instance, () => new ModularState());
        }

        private IApplicationContext _appContext;
        private bool? _hasChanged;
        private readonly HashSet<string> _installedModules = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _pendingModules = new(StringComparer.OrdinalIgnoreCase);

        private ModularState()
        {
            _appContext = EngineContext.Current?.Application;
            if (_appContext == null)
            {
                throw new InvalidOperationException("ApplicationContext not available.");
            }

            InternalReload();
        }

        private ModulesHasher Hasher
        {
            get => LazyInitializer.EnsureInitialized(ref _hasher, () => new ModulesHasher(_appContext));
        }

        public void Reload()
        {
            lock (_lock)
            {
                InternalReload();
            }
        }

        private void InternalReload()
        {
            _installedModules.Clear();
            _pendingModules.Clear();
            _hasChanged = null;

            if (_appContext.TenantRoot == null)
            {
                return;
            }

            var isLegacy = false;
            var file = _appContext.TenantRoot.GetFile(FileName);

            if (!file.Exists)
            {
                file = _appContext.TenantRoot.GetFile(LegacyFileName);
                isLegacy = file.Exists;
            }

            if (file.Exists)
            {
                var content = file.ReadAllText();
                var lines = content.ReadLines(true, true)
                    .Select(x => isLegacy ? MapLegacyModuleName(x) : x);

                if (isLegacy)
                {
                    lines = lines.Concat(new[] { "Smartstore.Blog", "Smartstore.Forums", "Smartstore.News", "Smartstore.Polls" });

                    // After first migration, create and save new InstalledModules.txt
                    _appContext.TenantRoot.WriteAllText(FileName, string.Join(Environment.NewLine, lines));
                }

                _installedModules.AddRange(lines);
            }

            // Pending modules
            file = _appContext.TenantRoot.GetFile(PendingModulesFileName);
            if (file.Exists)
            {
                var content = file.ReadAllText();
                _pendingModules.AddRange(content.ReadLines(true, true));
            }
        }

        private static string MapLegacyModuleName(string moduleName)
        {
            moduleName = moduleName.Replace("SmartStore", "Smartstore");

            var map = new List<(string, string)>
            { 
                ("GoogleAnalytics", "Google.Analytics"),
                ("GoogleMerchantCenter", "Google.MerchantCenter"),
                ("GoogleRemarketing", "Google.Remarketing"),
                ("FacebookAuth", "Facebook.Auth"),
                ("TwitterAuth", "Twitter.Auth"),
            };

            foreach (var kvp in map)
            {
                if (moduleName.Contains(kvp.Item1))
                {
                    moduleName = moduleName.Replace(kvp.Item1, kvp.Item2);
                    continue;
                }
            }
            
            return moduleName;
        }

        public void Save()
        {
            // InstalledModules.txt
            if (_installedModules.Count == 0)
            {
                _appContext.TenantRoot.TryDeleteFile(FileName);
            }
            else
            {
                var content = string.Join(Environment.NewLine, _installedModules);
                _appContext.TenantRoot.WriteAllText(FileName, content);
            }

            // PendingModules.txt
            if (_pendingModules.Count == 0)
            {
                _appContext.TenantRoot.TryDeleteFile(PendingModulesFileName);
            }
            else
            {
                var content = string.Join(Environment.NewLine, _pendingModules);
                _appContext.TenantRoot.WriteAllText(PendingModulesFileName, content);
            }
        }

        /// <summary>
        /// Modules that are loaded and installed.
        /// </summary>
        public ISet<string> InstalledModules
            => _installedModules;

        /// <summary>
        /// Modules that are loaded but are not yet installed.
        /// </summary>
        public ISet<string> PendingModules
            => _pendingModules;

        /// <summary>
        /// Modules that should be ignored during installation.
        /// </summary>
        public string[] IgnoredModules
            => _appContext.AppConfiguration.IgnoredModules ?? Array.Empty<string>();

        /// <summary>
        /// <c>true</c> if any module file has changed since previous application start.
        /// </summary>
        public bool HasChanged
        {
            get => _hasChanged ??= Hasher.HasChanged;
        }

        public void SaveStateHash()
            => Hasher.Persist();
    }
}

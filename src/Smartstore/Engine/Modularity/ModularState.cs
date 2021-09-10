using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                var hashCombiner = new HashCodeCombiner();

                // Add each *.dll and module.json file of compatible modules.
                var arrModules = _appContext.ModuleCatalog.Modules
                    .Where(x => !x.Incompatible)
                    .OrderBy(x => x.SystemName)
                    .ToArray();

                foreach (var m in arrModules)
                {
                    var manifestFile = new FileInfo(Path.Combine(m.PhysicalPath, "module.json"));
                    
                    hashCombiner.Add(m.IsInstalled());
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

        private readonly static Lazy<ModularState> _instance = new(() => new ModularState(), true);
        private readonly static object _lock = new();

        public static ModularState Instance
        {
            get => _instance.Value;
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
                var lines = content.GetLines(true, true)
                    .Select(x => isLegacy ? x.Replace("SmartStore", "Smartstore") : x);

                if (isLegacy)
                {
                    lines = lines.Concat(new[] { "Smartstore.Blog", "Smartstore.Forum", "Smartstore.News", "Smartstore.Polls" });
                }

                _installedModules.AddRange(lines);
            }

            // Pending modules
            file = _appContext.TenantRoot.GetFile(PendingModulesFileName);
            if (file.Exists)
            {
                var content = file.ReadAllText();
                _pendingModules.AddRange(content.GetLines(true, true));
            }
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

        public ISet<string> InstalledModules
            => _installedModules;

        public ISet<string> PendingModules
            => _pendingModules;

        public string[] IgnoredModules
            => _appContext.AppConfiguration.IgnoredModules ?? Array.Empty<string>();

        /// <summary>
        /// <c>true</c> if any module file has changed since previous application start.
        /// </summary>
        public bool HasChanged
        {
            get => _hasChanged ??= new ModulesHasher(_appContext).HasChanged;
        }

        public void SaveStateHash()
            => new ModulesHasher(_appContext).Persist();
    }
}

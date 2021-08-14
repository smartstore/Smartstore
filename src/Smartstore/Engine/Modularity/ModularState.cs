using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        private readonly static Lazy<ModularState> _instance = new(() => new ModularState(), true);
        private readonly static object _lock = new();

        public static ModularState Instance
        {
            get => _instance.Value;
        }

        private IApplicationContext _appContext;
        private bool? _hasChanged;
        private readonly HashSet<string> _installedModules = new(StringComparer.OrdinalIgnoreCase);

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

                _installedModules.AddRange(lines);
            }
        }

        public void Save()
        {
            if (_installedModules.Count == 0)
            {
                _appContext.TenantRoot.TryDeleteFile(FileName);
            }
            else
            {
                var content = string.Join(Environment.NewLine, _installedModules);
                _appContext.TenantRoot.WriteAllText(FileName, content);
            }
        }

        public ISet<string> InstalledModules
            => _installedModules;

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

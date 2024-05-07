using System.Collections.Frozen;
using System.Reflection;

namespace Smartstore.Engine.Modularity
{
    public class ModuleCatalog : IModuleCatalog
    {
        private readonly FrozenDictionary<string, IModuleDescriptor> _nameMap;
        private readonly FrozenDictionary<Assembly, IModuleDescriptor> _assemblyMap;
        private readonly FrozenDictionary<string, IModuleDescriptor> _themeMap;

        public ModuleCatalog(IEnumerable<IModuleDescriptor> modules)
        {
            Guard.NotNull(modules);

            var nameMap = new Dictionary<string, IModuleDescriptor>(StringComparer.OrdinalIgnoreCase);
            var assemblyMap = new Dictionary<Assembly, IModuleDescriptor>();
            var themeMap = new Dictionary<string, IModuleDescriptor>(StringComparer.OrdinalIgnoreCase);

            foreach (var module in modules)
            {
                nameMap[module.SystemName] = module;

                if (module.Module?.Assembly != null)
                {
                    assemblyMap[module.Module.Assembly] = module;
                }

                if (module.Theme.HasValue())
                {
                    themeMap[module.Theme] = module;
                }
            }

            _nameMap = nameMap.ToFrozenDictionary();
            _assemblyMap = assemblyMap.ToFrozenDictionary();

            if (themeMap.Count > 0)
            {
                _themeMap = themeMap.ToFrozenDictionary();
            }

            IncompatibleModules = modules
                .Where(x => x.Incompatible)
                .Select(x => x.SystemName)
                .ToArray();
        }

        public IEnumerable<IModuleDescriptor> Modules
        {
            get => _nameMap.Values;
        }

        public IEnumerable<string> IncompatibleModules
        {
            get;
            // For unit testing purposes
            internal set;
        }

        public bool HasModule(string systemName)
        {
            return _nameMap.ContainsKey(systemName);
        }

        public IModuleDescriptor GetModuleByAssembly(Assembly assembly)
        {
            if (assembly != null && _assemblyMap.TryGetValue(assembly, out var descriptor))
            {
                return descriptor;
            }

            return null;
        }

        public IModuleDescriptor GetModuleByName(string name, bool installedOnly = true)
        {
            if (name.HasValue() && _nameMap.TryGetValue(name, out var descriptor))
            {
                if (!installedOnly || descriptor.IsInstalled())
                {
                    return descriptor;
                } 
            }

            return null;
        }

        public IModuleDescriptor GetModuleByTheme(string themeName, bool installedOnly = true)
        {
            if (_themeMap != null && themeName.HasValue() && _themeMap.TryGetValue(themeName, out var descriptor))
            {
                if (!installedOnly || descriptor.IsInstalled())
                {
                    return descriptor;
                }
            }

            return null;
        }
    }
}
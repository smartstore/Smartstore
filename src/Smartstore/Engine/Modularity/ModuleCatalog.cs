using System;
using System.Collections.Generic;
using System.Reflection;

namespace Smartstore.Engine.Modularity
{
    public class ModuleCatalog : IModuleCatalog
    {
        // TODO: (core) Implement Tenant stuff
        private readonly IDictionary<string, ModuleDescriptor> _nameMap = new Dictionary<string, ModuleDescriptor>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<Assembly, ModuleDescriptor> _assemblyMap = new Dictionary<Assembly, ModuleDescriptor>();
        private readonly HashSet<Assembly> _inactiveAssemblies = new();

        public IEnumerable<ModuleDescriptor> Modules
        {
            get => _nameMap.Values;
            internal set
            {
                _nameMap.Clear();
                _assemblyMap.Clear();

                foreach (var module in value)
                {
                    _nameMap[module.SystemName] = module;

                    // TODO: (core) Add module to assembly map
                    //if (module.Assembly.Assembly != null)
                    //{
                    //    _assemblyMap[module.Assembly.Assembly] = module;
                    //}
                }
            }
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

        public bool IsActiveModuleAssembly(Assembly assembly)
        {
            return assembly != null && !_inactiveAssemblies.Contains(assembly);
        }

        public ModuleDescriptor GetModuleByAssembly(Assembly assembly, bool installedOnly = true)
        {
            if (assembly != null && _assemblyMap.TryGetValue(assembly, out var descriptor))
            {
                if (!installedOnly || descriptor.Installed)
                    return descriptor;
            }

            return null;
        }

        public ModuleDescriptor GetModuleByName(string name, bool installedOnly = true)
        {
            if (name.HasValue() && _nameMap.TryGetValue(name, out var descriptor))
            {
                if (!installedOnly || descriptor.Installed)
                    return descriptor;
            }

            return null;
        }
    }
}
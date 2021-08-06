using System;
using System.Collections.Generic;
using System.Reflection;

namespace Smartstore.Engine.Modularity
{
    public class ModuleCatalog : IModuleCatalog
    {
        // TODO: (core) Implement Tenant stuff
        private readonly IDictionary<string, IModuleDescriptor> _nameMap = new Dictionary<string, IModuleDescriptor>(StringComparer.OrdinalIgnoreCase);
        private readonly IDictionary<Assembly, IModuleDescriptor> _assemblyMap = new Dictionary<Assembly, IModuleDescriptor>();
        private readonly HashSet<Assembly> _inactiveAssemblies = new();

        public IEnumerable<IModuleDescriptor> Modules
        {
            get => _nameMap.Values;
            internal set
            {
                _nameMap.Clear();
                _assemblyMap.Clear();

                foreach (var module in value)
                {
                    _nameMap[module.SystemName] = module;

                    if (module.AssemblyInfo?.Assembly != null)
                    {
                        _assemblyMap[module.AssemblyInfo.Assembly] = module;
                    }
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

        public IModuleDescriptor GetModuleByAssembly(Assembly assembly, bool installedOnly = true)
        {
            if (assembly != null && _assemblyMap.TryGetValue(assembly, out var descriptor))
            {
                if (!installedOnly || descriptor.AssemblyInfo?.Installed == true)
                    return descriptor;
            }

            return null;
        }

        public IModuleDescriptor GetModuleByName(string name, bool installedOnly = true)
        {
            if (name.HasValue() && _nameMap.TryGetValue(name, out var descriptor))
            {
                if (!installedOnly || descriptor.AssemblyInfo?.Installed == true)
                    return descriptor;
            }

            return null;
        }
    }
}
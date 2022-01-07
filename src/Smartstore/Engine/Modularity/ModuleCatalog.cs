using System.Reflection;

namespace Smartstore.Engine.Modularity
{
    public class ModuleCatalog : IModuleCatalog
    {
        private readonly Dictionary<string, IModuleDescriptor> _nameMap = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<Assembly, IModuleDescriptor> _assemblyMap = new();

        public ModuleCatalog(IEnumerable<IModuleDescriptor> modules)
        {
            Guard.NotNull(modules, nameof(modules));

            foreach (var module in modules)
            {
                _nameMap[module.SystemName] = module;

                if (module.Module?.Assembly != null)
                {
                    _assemblyMap[module.Module.Assembly] = module;
                }
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
                    return descriptor;
            }

            return null;
        }
    }
}
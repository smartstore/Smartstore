using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;

namespace Smartstore.Engine.Modularity
{
    public interface IModuleReferenceResolver
    {
        Assembly ResolveAssembly(Assembly requestingAssembly, string name);
    }

    /// <summary>
    /// Tries to resolve private references from the requesting module directory.
    /// </summary>
    internal class ModuleReferenceResolver : IModuleReferenceResolver
    {
        private readonly ConcurrentDictionary<Assembly, IModuleDescriptor> _assemblyModuleMap = new();
        private readonly IApplicationContext _appContext;

        public ModuleReferenceResolver(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        /// <summary>
        /// Tries to resolve and load a module reference assembly.
        /// </summary>
        /// <param name="requestingAssembly">The requesting assembly. May be the module main assembly or any dependency of it.</param>
        /// <param name="name">Name of assembly to resolve.</param>
        public Assembly ResolveAssembly(Assembly requestingAssembly, string name)
        {
            if (_appContext.ModuleCatalog == null)
            {
                return null;
            }

            IModuleDescriptor module = null;
            Assembly assembly = null;

            if (!_assemblyModuleMap.TryGetValue(requestingAssembly, out module))
            {
                module = _appContext.ModuleCatalog.GetModuleByAssembly(requestingAssembly);
            }

            if (module != null)
            {
                var requestedAssemblyName = name.Split(',', StringSplitOptions.RemoveEmptyEntries)[0] + ".dll";
                var fullPath = Path.Combine(module.PhysicalPath, requestedAssemblyName);
                if (File.Exists(fullPath))
                {
                    assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
                    _assemblyModuleMap[assembly] = module;
                }
            }
            
            if (assembly == null)
            {
                // Check for assembly already loaded
                assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == name);

                if (assembly == null)
                {
                    // Get assembly from TypeScanner
                    assembly = _appContext.TypeScanner?.Assemblies?.FirstOrDefault(a => a.FullName == name);
                }
            }

            if (assembly != null && module != null)
            {
                module.Module?.AddPrivateReference(assembly);
            }

            return assembly;
        }
    }
}

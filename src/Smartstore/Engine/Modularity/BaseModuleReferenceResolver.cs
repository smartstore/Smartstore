using System.Reflection;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Tries to resolve private references in the dependant base module.
    /// </summary>
    internal class BaseModuleReferenceResolver : IModuleReferenceResolver
    {
        private readonly IApplicationContext _appContext;

        public BaseModuleReferenceResolver(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        /// <summary>
        /// Tries to resolve and load a module reference assembly.
        /// </summary>
        /// <param name="requestingAssembly">The requesting assembly. Most likely a module's private reference assembly.</param>
        /// <param name="name">Name of assembly to resolve. Most likely located in a base module's directory.</param>
        public Assembly ResolveAssembly(Assembly requestingAssembly, string name)
        {
            var path = Path.GetDirectoryName(requestingAssembly?.Location);
            var module = _appContext.ModuleCatalog?.GetModuleByPath(path);

            return LocatePrivateAssembly(module, name);
        }

        private Assembly LocatePrivateAssembly(IModuleDescriptor module, string assemblyName)
        {
            if (module == null || module.DependsOn.IsNullOrEmpty())
            {
                return null;
            }

            // Check if the requested assembly is a dependency of the requesting module
            foreach (var dependency in module.DependsOn)
            {
                module = _appContext.ModuleCatalog.GetModuleByName(dependency);
                if (module != null)
                {
                    try
                    {
                        return module.Module.LoadContext.LoadFromAssemblyName(new AssemblyName(assemblyName));
                    }
                    catch
                    {
                        var assembly = LocatePrivateAssembly(module, assemblyName);
                        if (assembly != null)
                        {
                            return assembly;
                        }

                        continue;
                    }
                }
            }

            return null;
        }
    }
}

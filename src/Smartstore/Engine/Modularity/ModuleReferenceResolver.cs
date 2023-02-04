using System.Reflection;

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

            var module = _appContext.ModuleCatalog.GetModuleByAssembly(requestingAssembly);

            if (module != null)
            {
                var assembly = module.Module.LoadContext.LoadFromAssemblyName(new AssemblyName(name));
                return assembly;
            }

            return null;
        }
    }
}

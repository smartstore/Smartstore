using System.Reflection;
using System.Runtime.Loader;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Tries to resolve references from the app base directory.
    /// </summary>
    internal class AppBaseReferenceResolver : IModuleReferenceResolver
    {
        public Assembly ResolveAssembly(Assembly requestingAssembly, string name)
        {
            var resolver = new AssemblyDependencyResolver(requestingAssembly?.Location ?? AppContext.BaseDirectory);
            var assemblyPath = resolver.ResolveAssemblyToPath(new AssemblyName(name));
            if (assemblyPath != null)
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                return assembly;
            }

            return null;
        }
    }
}

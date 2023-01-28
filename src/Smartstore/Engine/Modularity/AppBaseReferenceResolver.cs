using System.Reflection;
using System.Runtime.Loader;

namespace Smartstore.Engine.Modularity
{
    /// <summary>
    /// Tries to resolve references from the app base directory.
    /// </summary>
    internal class AppBaseReferenceResolver : IModuleReferenceResolver
    {
        private readonly IApplicationContext _appContext;

        public AppBaseReferenceResolver(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public Assembly ResolveAssembly(Assembly requestingAssembly, string name)
        {
            var requestedAssemblyName = name.Split(',', StringSplitOptions.RemoveEmptyEntries)[0] + ".dll";
            var fullPath = Path.Combine(_appContext.RuntimeInfo.BaseDirectory, requestedAssemblyName);
            if (File.Exists(fullPath))
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
                return assembly;
            }

            return null;
        }
    }
}

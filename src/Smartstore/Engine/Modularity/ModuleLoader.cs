using System.Runtime.Loader;

namespace Smartstore.Engine.Modularity
{
    internal class ModuleLoader
    {
        private readonly IApplicationContext _appContext;

        public ModuleLoader(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public void LoadModule(ModuleDescriptor descriptor)
        {
            if (descriptor == null || descriptor.Incompatible)
            {
                return;
            }

            var assemblyPath = Path.Combine(descriptor.PhysicalPath, descriptor.AssemblyName);
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

            var assemblyInfo = new ModuleAssemblyInfo(descriptor)
            {
                Assembly = assembly,
                ModuleType = assembly.GetLoadableTypes()
                    .Where(t => !t.IsInterface && t.IsClass && !t.IsAbstract)
                    .FirstOrDefault(t => typeof(IModule).IsAssignableFrom(t))
            };

            descriptor.Module = assemblyInfo;
        }
    }
}

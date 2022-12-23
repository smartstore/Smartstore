using System.Runtime.Loader;
using Microsoft.Extensions.Logging;
using Smartstore.IO;

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

            if (descriptor.Theme.HasValue() && !PathUtility.HasInvalidPathChars(descriptor.Theme))
            {
                // Module is a theme companion. Create theme symlink: /Themes/{Theme} --> /Modules/{Module}
                var themeDir = _appContext.ThemesRoot.GetDirectory(descriptor.Theme);
                if (!themeDir.Exists) 
                {
                    // Create symlink only if theme dir does not exist
                    try
                    {
                        File.CreateSymbolicLink(themeDir.PhysicalPath, descriptor.PhysicalPath);
                    }
                    catch (Exception ex)
                    {
                        _appContext.Logger.Error(ex);
                    }
                }
            }

            descriptor.Module = assemblyInfo;
        }
    }
}

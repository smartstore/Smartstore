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

            descriptor.Module = new ModuleAssemblyInfo(descriptor);

            if (descriptor.Theme.HasValue() && !PathUtility.HasInvalidPathChars(descriptor.Theme))
            {
                // Module is a theme companion. Create theme symlink: /Themes/{Theme} --> /Modules/{Module}
                var themeDir = _appContext.ThemesRoot.GetDirectory(descriptor.Theme);
                if (!themeDir.Exists) 
                {
                    // Create symlink only if theme dir does not exist
                    try
                    {
                        Directory.CreateSymbolicLink(themeDir.PhysicalPath, descriptor.PhysicalPath);
                    }
                    catch (Exception ex)
                    {
                        _appContext.Logger.Error(ex);
                    }
                }
            }
        }
    }
}

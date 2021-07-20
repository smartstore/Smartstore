using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Packaging
{
    public class PackageManager : IPackageManager
    {
        public Task<IExtensionDescriptor> InstallAsync(ExtensionPackage package)
        {
            throw new NotImplementedException();
        }

        public Task UninstallAsync(IExtensionDescriptor extension)
        {
            throw new NotImplementedException();
        }

        public Task<ExtensionPackage> BuildModulePackageAsync(string moduleName)
        {
            throw new NotImplementedException();
        }

        public Task<ExtensionPackage> BuildThemePackageAsync(string themeName)
        {
            throw new NotImplementedException();
        }
    }
}

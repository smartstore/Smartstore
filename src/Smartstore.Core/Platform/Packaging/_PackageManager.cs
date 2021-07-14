using System;
using System.IO;
using System.Threading.Tasks;

namespace Smartstore.Core.Packaging
{
    public class PackageManager : IPackageManager
    {
        public Task<PackageInfo> InstallAsync(Stream packageStream, string location, string applicationPath)
        {
            throw new NotImplementedException();
        }

        public Task UninstallAsync(string packageId, string applicationPath)
        {
            throw new NotImplementedException();
        }

        public Task<PackagingResult> BuildModulePackageAsync(string pluginName)
        {
            throw new NotImplementedException();
        }

        public Task<PackagingResult> BuildThemePackageAsync(string themeName)
        {
            throw new NotImplementedException();
        }
    }
}

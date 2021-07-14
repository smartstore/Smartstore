using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartstore.Core.Packaging
{
    public interface IPackageManager
    {
        Task<PackageInfo> InstallAsync(Stream packageStream, string location, string applicationPath);
        Task UninstallAsync(string packageId, string applicationPath);

        Task<PackagingResult> BuildModulePackageAsync(string pluginName);
        Task<PackagingResult> BuildThemePackageAsync(string themeName);
    }
}

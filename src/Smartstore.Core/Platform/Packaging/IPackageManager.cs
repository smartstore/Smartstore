using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Packaging
{
    public interface IPackageManager
    {
        Task<IExtensionDescriptor> InstallAsync(ExtensionPackage package);
        Task UninstallAsync(IExtensionDescriptor extension);

        Task<ExtensionPackage> BuildModulePackageAsync(string pluginName);
        Task<ExtensionPackage> BuildThemePackageAsync(string themeName);
    }
}

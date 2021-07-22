using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Engine;
using Smartstore.Engine.Modularity;

namespace Smartstore.Core.Packaging
{
    public partial class PackageInstaller : IPackageInstaller
    {
        private readonly IApplicationContext _appContext;

        public PackageInstaller(IApplicationContext appContext)
        {
            _appContext = appContext;
        }

        public Task<IExtensionDescriptor> InstallPackageAsync(ExtensionPackage package)
        {
            var descriptor = package.Descriptor;

            //using var archive = new ZipArchive(package.ArchiveStream, ZipArchiveMode.Read);
            //archive.ExtractToDirectory(_appContext.ContentRoot.Root);

            return Task.FromResult(descriptor);
        }

        public Task UninstallPackageAsync(IExtensionDescriptor extension)
        {
            throw new NotImplementedException();
        }
    }
}

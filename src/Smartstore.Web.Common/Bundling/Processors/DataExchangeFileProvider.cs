using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Smartstore.Web.Bundling.Processors
{
    /// <summary>
    /// Provides access to publicly accessible export files via the path "App_Data/Tenants/{TenantName}/exchange".
    /// </summary>
    internal class DataExchangeFileProvider : IFileProvider
    {
        const string DirName = "exchange";

        private readonly PhysicalFileProvider _provider;

        public DataExchangeFileProvider(IApplicationContext appContext)
        {
            var root = Guard.NotNull(appContext).TenantRoot.GetDirectory(DirName);
            if (!root.Exists)
            {
                root.Create();
            }

            _provider = new PhysicalFileProvider(root.PhysicalPath);
        }

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return _provider.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            var info = _provider.GetFileInfo(subpath);
            $"- {subpath} {info?.PhysicalPath}".Dump();
            return info;
        }

        public IChangeToken Watch(string filter)
        {
            return _provider.Watch(filter);
        }
    }
}

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;

namespace Smartstore.Core.DataExchange.Export
{
    // TODO: Somehow make it into AssetFileProvider (without moving it to Web.Common).

    /// <summary>
    /// Provides access to publicly accessible export files via the path "App_Data/Tenants/{TenantName}/exchange".
    /// </summary>
    public class DataExportFileProvider(IApplicationContext appContext) : IFileProvider
    {
        const string DirName = "exchange";

        private readonly IApplicationContext _appContext = appContext;
        private PhysicalFileProvider _provider;
        private ExclusionFilters _filters;

        public IDirectoryContents GetDirectoryContents(string subpath)
        {
            return Provider.GetDirectoryContents(subpath);
        }

        public IFileInfo GetFileInfo(string subpath)
        {
            return Provider.GetFileInfo(subpath);
        }

        public IChangeToken Watch(string filter)
        {
            return Provider.Watch(filter);
        }

        private PhysicalFileProvider Provider
        {
            get
            {
                if (_provider == null)
                {
                    var root = _appContext.TenantRoot.GetDirectory(DirName);
                    if (!root.Exists)
                    {
                        root.Create();
                    }

                    _filters = ExclusionFilters.Sensitive;
                    _provider = new PhysicalFileProvider(root.PhysicalPath, _filters);
                }

                return _provider;
            }
        }
    }
}

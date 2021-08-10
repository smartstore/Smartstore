using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Smartstore.Engine;
using Smartstore.Http;
using Smartstore.Utilities;

namespace Smartstore.Web.Razor
{
    internal class RazorRuntimeFileProvider : IFileProvider
    {
        // TODO: (core) Implement DebugFileProvider for module and theme files and replace RazorRuntimeFileProvider.

        private readonly IFileProvider _contentRoot;
        private readonly IFileProvider _modulesRoot;
        private readonly IFileProvider _debugRoot;
        private readonly bool _debugEnabled;

        public RazorRuntimeFileProvider(IApplicationContext appContext)
        {
            _contentRoot = appContext.ContentRoot;
            _modulesRoot = appContext.ModulesRoot;
            _debugEnabled = CommonHelper.IsDevEnvironment && appContext.HostEnvironment.IsDevelopment();
            _debugRoot = _debugEnabled 
                ? new PhysicalFileProvider(Path.GetFullPath(Path.Combine(appContext.HostEnvironment.ContentRootPath, @"..\Smartstore.Modules"))) 
                : _modulesRoot;
        }

        public IFileInfo GetFileInfo(string subpath)
            => ResolveFileProvider(ref subpath).GetFileInfo(subpath);

        public IDirectoryContents GetDirectoryContents(string subpath)
            => ResolveFileProvider(ref subpath).GetDirectoryContents(subpath);

        public IChangeToken Watch(string filter)
            => ResolveFileProvider(ref filter).Watch(filter);

        private IFileProvider ResolveFileProvider(ref string path)
        {
            if (!_debugEnabled)
            {
                return _contentRoot;
            }

            var isModulePath = path.TrimStart('~').StartsWith("/modules/", StringComparison.OrdinalIgnoreCase);
            if (isModulePath)
            {
                path = path.Substring(8);
                return _debugRoot;
            }
            
            return _contentRoot;
        }
    }
}

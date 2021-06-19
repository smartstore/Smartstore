using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Smartstore.Engine;
using WebOptimizer;

namespace Smartstore.Web.Bundling
{
    public class SmartAsset : IAsset
    {
        private readonly object _lock = new();
        private readonly IAsset _inner;
        private string[] _fixedSourceFiles;

        public SmartAsset(IAsset inner)
        {
            _inner = Guard.NotNull(inner, nameof(inner));
        }

        public string ContentType 
            => _inner.ContentType;

        public IList<IProcessor> Processors
            => _inner.Processors;

        public IDictionary<string, object> Items
            => _inner.Items;

        public string Route
            => _inner.Route;

        public IEnumerable<string> SourceFiles
        {
            get
            {
                if (_fixedSourceFiles == null)
                {
                    lock (_lock)
                    {
                        if (_fixedSourceFiles == null)
                        {
                            var appContext = EngineContext.Current.Application;
                            var fileProvider = _inner.GetFileProvider(appContext.HostEnvironment as IWebHostEnvironment);

                            _fixedSourceFiles = _inner.SourceFiles
                                .Select(x => TryFindMinFile(appContext, fileProvider, x))
                                .ToArray();
                        }
                    }
                }
                
                return _fixedSourceFiles;
            }
        }

        public Task<byte[]> ExecuteAsync(HttpContext context, IWebOptimizerOptions options)
        {
            return _inner.ExecuteAsync(context, options);
        }

        public string GenerateCacheKey(HttpContext context)
        {
            return _inner.GenerateCacheKey(context);
        }

        private static string TryFindMinFile(IApplicationContext appContext, IFileProvider fileProvider, string subpath)
        {
            try
            {
                if (appContext.HostEnvironment.IsDevelopment())
                {
                    // Return path as is in dev mode
                    return subpath;
                }

                var extension = Path.GetExtension(subpath);
                if (subpath.EndsWith(".min" + extension, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Is already a minified file, get out!
                    return subpath;
                }

                var minPath = "{0}.min{1}".FormatInvariant(subpath.Substring(0, subpath.Length - extension.Length), extension);
                if (fileProvider.GetFileInfo(minPath).Exists)
                {
                    return minPath;
                }

                return subpath;
            }
            catch
            {
                return subpath;
            }
        }
    }
}

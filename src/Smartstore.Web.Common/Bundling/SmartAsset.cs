using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    [DebuggerDisplay("{Route}")]
    public class SmartAsset : IAsset
    {
        private readonly object _lock = new();
        private readonly IAsset _inner;
        private IEnumerable<string> _fixedSourceFiles;

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

        public virtual async Task<byte[]> ExecuteAsync(HttpContext context, IWebOptimizerOptions options)
        {
            var content = await _inner.ExecuteAsync(context, options);
            return content;
        }

        public virtual string GenerateCacheKey(HttpContext context)
        {
            var cacheKey = _inner.Route.ToLowerInvariant();

            foreach (var processors in Processors)
            {
                try
                {
                    var processorKey = processors.CacheKey(context);
                    if (processorKey.HasValue())
                    {
                        cacheKey += processorKey;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"CacheKey generation exception in {processors.GetType().FullName} processor", ex);
                }
            }

            return cacheKey;
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

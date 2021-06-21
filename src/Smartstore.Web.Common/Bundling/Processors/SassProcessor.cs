using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.FileProviders;
using SharpScss;
using WebOptimizer;
using WebOptimizer.Sass;

namespace Smartstore.Web.Bundling.Processors
{
    /// <summary>
    /// Compiles Sass files
    /// </summary>
    public class SassProcessor : IProcessor
    {
        private static Regex ImportRegex = new Regex("^@import ['\"]([^\"']+)['\"];$");

        /// <summary>
        /// Gets the custom key that should be used when calculating the memory cache key.
        /// </summary>
        public string CacheKey(HttpContext context) => GenerateCacheKey(context);

        private WebOptimazerScssOptions _options;

        private IAsset _asset;

        private List<string> _addedImports;

        private FileVersionProvider _fileVersionProvider;

        /// <summary>
        /// Gets the custom key that should be used when calculating the memory cache key.
        /// </summary>
        public SassProcessor(IAsset asset, WebOptimazerScssOptions options = null)
        {
            _addedImports = new List<string>();
            _asset = asset;
            _options = options;
        }


        /// <summary>
        /// Executes the processor on the specified configuration.
        /// </summary>
        public Task ExecuteAsync(IAssetContext context)
        {
            var content = new Dictionary<string, byte[]>();
            var env = (IWebHostEnvironment)context.HttpContext.RequestServices.GetService(typeof(IWebHostEnvironment));
            IFileProvider fileProvider = context.Asset.GetFileProvider(env);

            foreach (string route in context.Content.Keys)
            {
                IFileInfo file = fileProvider.GetFileInfo(route);
                var settings = new ScssOptions { InputFile = file.PhysicalPath };
                if (_options != null)
                {
                    settings.IncludePaths.AddRange(_options.IncludePaths);
                    settings.GenerateSourceMap = _options.GenerateSourceMap;
                    settings.Indent = _options.Indent;
                    settings.IsIndentedSyntaxSource = _options.IsIndentedSyntaxSource;
                    settings.Linefeed = _options.Linefeed;
                    settings.OmitSourceMapUrl = _options.OmitSourceMapUrl;
                    settings.SourceComments = _options.SourceComments;
                    settings.SourceMapContents = _options.SourceMapContents;
                    settings.SourceMapEmbed = _options.SourceMapEmbed;
                    settings.SourceMapRoot = _options.SourceMapRoot;
                    settings.TryImport = _options.TryImport;
                }

                ScssResult result = Scss.ConvertToCss(context.Content[route].AsString(), settings);

                content[route] = result.Css.AsByteArray();
            }

            context.Content = content;

            return Task.CompletedTask;
        }

        private string GenerateCacheKey(HttpContext context)
        {
            var cacheKey = new StringBuilder();
            var env = (IWebHostEnvironment)context.RequestServices.GetService(typeof(IWebHostEnvironment));
            IFileProvider fileProvider = _asset.GetFileProvider(env);
            if (_fileVersionProvider == null)
            {
                var cache = (IMemoryCache)context.RequestServices.GetService(typeof(IMemoryCache));

                _fileVersionProvider = new FileVersionProvider(
                    fileProvider,
                    cache,
                    context.Request.PathBase);
            }

            foreach (var route in _asset.SourceFiles.Where(f => f.EndsWith(".scss")))
            {
                IFileInfo file = fileProvider.GetFileInfo(route);
                var basePath = GetBasePath(route);
                using var stream = file.CreateReadStream();
                using var reader = new StreamReader(stream);
                for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
                {
                    var match = ImportRegex.Match(line.Trim());
                    if (match.Success)
                    {
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            var subRoute = match.Groups[i].Value;
                            if (!string.IsNullOrEmpty(subRoute) && !Uri.TryCreate(subRoute, UriKind.Absolute, out _))
                            {
                                AppendImportedSassFiles(fileProvider, cacheKey, basePath, subRoute);
                            }
                        }
                    }
                }
            }

            using var algo = SHA1.Create();
            byte[] buffer = Encoding.UTF8.GetBytes(cacheKey.ToString());
            byte[] hash = algo.ComputeHash(buffer);
            return WebEncoders.Base64UrlEncode(hash);
        }

        private void AppendImportedSassFiles(IFileProvider fileProvider, StringBuilder cacheKey, string basePath, string route)
        {
            // Add extension if missing
            if (!Path.HasExtension(route))
            {
                route = $"{route}.scss";
            }

            var filePath = PathCombine(basePath, route);
            IFileInfo file = fileProvider.GetFileInfo(filePath);

            // Add underscore at the start if missing
            if (!file.Exists)
            {
                filePath = PathCombine(basePath, $"_{route}");
                file = fileProvider.GetFileInfo(filePath);
                if (!file.Exists)
                {
                    return;
                }
            }

            // Don't add same file twice
            if (_addedImports.Contains(filePath))
            {
                return;
            }

            // Add file in cache key
            _addedImports.Add(filePath);
            cacheKey.Append(_fileVersionProvider.AddFileVersionToPath(filePath));

            // Add sub files
            using var stream = file.CreateReadStream();
            using var reader = new StreamReader(stream);
            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                var match = ImportRegex.Match(line.Trim());
                if (match.Success)
                {
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        var subRoute = match.Groups[i].Value;
                        if (!string.IsNullOrEmpty(subRoute) && !Uri.TryCreate(subRoute, UriKind.Absolute, out _))
                        {
                            AppendImportedSassFiles(fileProvider, cacheKey, basePath, subRoute);
                        }
                    }
                }
            }
        }

        private static string PathCombine(params string[] args)
        {
            return Path.Combine(args)
                .Replace($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}", string.Empty)
                .Replace("\\", "/");
        }

        private static string GetBasePath(string path)
        {
            return Path.GetDirectoryName(path)?.Replace("\\", "/") ?? string.Empty;
        }
    }
}

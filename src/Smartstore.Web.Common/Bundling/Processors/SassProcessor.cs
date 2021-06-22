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
using Microsoft.Extensions.DependencyInjection;
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
        private static readonly Regex ImportRegex = new("^@import ['\"]([^\"']+)['\"];$");

        private readonly IAsset _asset;
        private readonly WebOptimazerScssOptions _options;
        private readonly HashSet<string> _addedImports = new();
        private FileVersionProvider _fileVersionProvider;

        /// <summary>
        /// Gets the custom key that should be used when calculating the memory cache key.
        /// </summary>
        public SassProcessor(IAsset asset, WebOptimazerScssOptions options = null)
        {
            _asset = Guard.NotNull(asset, nameof(asset));
            _options = options;
        }

        /// <summary>
        /// Executes the processor on the specified configuration.
        /// </summary>
        public Task ExecuteAsync(IAssetContext context)
        {
            var content = new Dictionary<string, byte[]>();
            var env = context.HttpContext.RequestServices.GetService<IWebHostEnvironment>();
            var fileProvider = context.Asset.GetFileProvider(env);

            foreach (string route in context.Content.Keys)
            {
                var file = fileProvider.GetFileInfo(route);
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

                var result = Scss.ConvertToCss(context.Content[route].AsString(), settings);

                content[route] = result.Css.AsByteArray();
            }

            context.Content = content;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the custom key that should be used when calculating the memory cache key.
        /// </summary>
        public string CacheKey(HttpContext context)
            => GenerateCacheKey(context);

        private string GenerateCacheKey(HttpContext context)
        {
            _addedImports.Clear();

            var cacheKey = new StringBuilder();
            var env = context.RequestServices.GetService<IWebHostEnvironment>();
            var fileProvider = _asset.GetFileProvider(env);

            if (_fileVersionProvider == null)
            {
                _fileVersionProvider = new FileVersionProvider(
                    fileProvider,
                    context.RequestServices.GetService<IMemoryCache>(), 
                    context.Request.PathBase);
            }

            foreach (var route in _asset.SourceFiles.Where(f => f.EndsWith(".scss")))
            {
                var file = fileProvider.GetFileInfo(route);
                var basePath = GetBasePath(route);

                ProcessImports(basePath, file, fileProvider, cacheKey);
            }

            using var algo = SHA1.Create();
            byte[] buffer = Encoding.UTF8.GetBytes(cacheKey.ToString());
            byte[] hash = algo.ComputeHash(buffer);
            return WebEncoders.Base64UrlEncode(hash);
        }

        private void ProcessImports(string basePath, IFileInfo file, IFileProvider fileProvider, StringBuilder cacheKey)
        {
            if (!file.Exists)
            {
                return;
            }
            
            using var stream = file.CreateReadStream();
            using var reader = new StreamReader(stream);

            bool hasImport = false;

            for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
            {
                line = line.Trim();

                if (line.IsEmpty())
                {
                    continue;
                }

                var match = ImportRegex.Match(line.Trim());
                if (!match.Success)
                {
                    var isComment = line.StartsWith("//") || line.StartsWith("/*");
                    if (hasImport && !isComment)
                    {
                        // The first non-comment directive after the last import indicates that there are no imports anymore.
                        break;
                    }
                }
                else
                {
                    hasImport = true;
                    for (int i = 1; i < match.Groups.Count; i++)
                    {
                        var subPath = match.Groups[i].Value;
                        if (!string.IsNullOrEmpty(subPath) && !Uri.TryCreate(subPath, UriKind.Absolute, out _))
                        {
                            // Add extension if missing
                            if (!Path.HasExtension(subPath))
                            {
                                subPath = $"{subPath}.scss";
                            }

                            var filePath = PathCombine(basePath, subPath, out basePath, out var fileName);
                            var importedFile = fileProvider.GetFileInfo(filePath);

                            // Add underscore at the start if missing
                            if (!importedFile.Exists)
                            {
                                filePath = PathCombine(basePath, $"_{fileName}", out basePath, out fileName);
                                file = fileProvider.GetFileInfo(filePath);
                                if (!file.Exists)
                                {
                                    continue;
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

                            // Recursive call
                            ProcessImports(basePath, importedFile, fileProvider, cacheKey);
                        }
                    }
                }
            }
        }

        private static string PathCombine(string path1, string path2, out string basePath, out string fileName)
        {
            basePath = path1;
            fileName = path2;

            return Path.Combine(path1, path2)
                .Replace($"{Environment.CurrentDirectory}{Path.DirectorySeparatorChar}", string.Empty)
                .Replace("\\", "/");
        }

        private static string GetBasePath(string path)
        {
            return Path.GetDirectoryName(path)?.Replace("\\", "/") ?? string.Empty;
        }



        //private void AppendImportedSassFiles(IFileProvider fileProvider, StringBuilder cacheKey, string basePath, string subPath)
        //{
        //    // Add extension if missing
        //    if (!Path.HasExtension(subPath))
        //    {
        //        subPath = $"{subPath}.scss";
        //    }

        //    var filePath = PathCombine(basePath, subPath);
        //    var file = fileProvider.GetFileInfo(filePath);

        //    // Add underscore at the start if missing
        //    if (!file.Exists)
        //    {
        //        filePath = PathCombine(basePath, $"_{subPath}");
        //        file = fileProvider.GetFileInfo(filePath);
        //        if (!file.Exists)
        //        {
        //            return;
        //        }
        //    }

        //    // Don't add same file twice
        //    if (_addedImports.Contains(filePath))
        //    {
        //        return;
        //    }

        //    // Add file in cache key
        //    _addedImports.Add(filePath);
        //    cacheKey.Append(_fileVersionProvider.AddFileVersionToPath(filePath));

        //    // Add sub files
        //    using var stream = file.CreateReadStream();
        //    using var reader = new StreamReader(stream);
        //    for (var line = reader.ReadLine(); line != null; line = reader.ReadLine())
        //    {
        //        var match = ImportRegex.Match(line.Trim());
        //        if (match.Success)
        //        {
        //            for (int i = 1; i < match.Groups.Count; i++)
        //            {
        //                var subRoute = match.Groups[i].Value;
        //                if (!string.IsNullOrEmpty(subRoute) && !Uri.TryCreate(subRoute, UriKind.Absolute, out _))
        //                {
        //                    AppendImportedSassFiles(fileProvider, cacheKey, basePath, subRoute);
        //                }
        //            }
        //        }
        //    }
        //}
    }
}

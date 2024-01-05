using System.Buffers;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Smartstore.IO;
using Smartstore.Web.Bundling.Processors;

namespace Smartstore.Web.Bundling
{
    [DebuggerDisplay("BundleFile: {Path}")]
    public class BundleFile
    {
        public string Path { get; init; }
        public IFileInfo File { get; init; }
        public IFileProvider FileProvider { get; init; }
    }

    /// <summary>
    /// Represents a script bundle that does Js minification.
    /// </summary>
    [DebuggerDisplay("ScriptBundle: {Route}")]
    public class ScriptBundle : Bundle
    {
        public ScriptBundle(string route)
            : base(route, "application/javascript", JsMinifyProcessor.Instance, ConcatProcessor.Instance)
        {
            ConcatenationToken = ";" + Environment.NewLine;
        }
    }

    /// <summary>
    /// Represents a stylesheet bundle that does CSS minification, URL rewrite & Autoprefixing.
    /// </summary>
    [DebuggerDisplay("StyleBundle: {Route}")]
    public class StyleBundle : Bundle
    {
        public StyleBundle(string route)
            : base(route, "text/css", SassProcessor.Instance, CssMinifyProcessor.Instance, CssRewriteUrlProcessor.Instance, ConcatProcessor.Instance, AutoprefixerProcessor.Instance)
        {
        }
    }

    /// <summary>
    /// Represents a list of file references to be bundled together as a single resource.
    /// </summary>
    /// <remarks>
    /// A bundle is referenced statically via the <see cref="Route"/> property (i.e. Route = ~/bundle/js/public.js).
    /// </remarks>
    [DebuggerDisplay("Bundle: {Route}")]
    public class Bundle
    {
        private static readonly SearchValues<char> _globChars = SearchValues.Create("*[?");
        private static readonly SearchValues<char> _queryChars = SearchValues.Create("?#");

        private readonly HashSet<string> _sourceFiles = [];

        protected Bundle()
        {
        }

        protected internal Bundle(Bundle other)
        {
            Route = other.Route;
            ContentType = other.ContentType;
            ConcatenationToken = other.ConcatenationToken;
            FileProvider = other.FileProvider;

            _sourceFiles.AddRange(other.SourceFiles);
            Processors.AddRange(other.Processors);
        }

        public Bundle(string route, string contentType, params IBundleProcessor[] processors)
            : this(route, contentType, null, processors)
        {
        }

        public Bundle(string route, string contentType, IFileProvider fileProvider, params IBundleProcessor[] processors)
        {
            Guard.NotEmpty(route, nameof(route));
            Guard.NotEmpty(contentType, nameof(contentType));

            Route = ValidateRoute(NormalizeRoute(route));
            ContentType = contentType;
            FileProvider = fileProvider;

            Processors.AddRange(processors);
        }

        #region Init & Util

        protected virtual string ValidateRoute(string route)
        {
            if (route.AsSpan().IndexOfAny(_globChars) > -1)
            {
                throw new ArgumentException($"The route \"{route}\" appears to be a globbing pattern which isn't supported for bundle routes.", nameof(route));
            }

            return route;
        }

        public static string NormalizeRoute(string route)
        {
            route = route.Trim();
            var normalizedRoute = route[0] == '/' || route[0] == '~'
                ? "/" + route.Trim().TrimStart('~', '/')
                : route;

            var index = normalizedRoute.AsSpan().IndexOfAny(_queryChars);
            if (index > -1)
            {
                normalizedRoute = normalizedRoute[..index];
            }

            return normalizedRoute;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Virtual path used to reference the <see cref="Bundle"/> from within a view or Web page.
        /// </summary>
        public string Route { get; }

        /// <summary>
        /// Gets the bundle content type.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// The token inserted between bundled files to ensure that the final bundle content is valid
        /// </summary>
        /// <remarks>
        /// By default, if <see cref="ConcatenationToken"/> is not specified, the bundling framework inserts a new line.
        /// </remarks>
        public string ConcatenationToken { get; set; } = Environment.NewLine;

        /// <summary>
        /// Source files that represent the contents of the bundle. 
        /// Globbing patterns are allowed.
        /// </summary>
        public virtual IEnumerable<string> SourceFiles
        {
            get => _sourceFiles.AsReadOnly();
        }

        /// <summary>
        /// The file provider to use for file resolution. 
        /// If <c>null</c>, <see cref="BundlingOptions.FileProvider"/> will be used instead.
        /// </summary>
        public IFileProvider FileProvider { get; set; }

        /// <summary>
        /// The list of processor for the bundle.
        /// </summary>
        public IList<IBundleProcessor> Processors { get; } = new List<IBundleProcessor>();

        #endregion

        #region Fluent

        /// <summary>
        /// Adds bundle processors to the bundle processing pipeline.
        /// </summary>
        public Bundle AddProcessor(params IBundleProcessor[] processors)
        {
            Processors.AddRange(processors);
            return this;
        }

        /// <summary>
        /// Removes a bundle processor by code from the processing pipeline.
        /// </summary>
        public Bundle RemoveProcessor(string processorCode)
        {
            if (processorCode.HasValue())
            {
                var processors = Processors.Where(x => x.Code == processorCode).ToArray();
                processors.Each(x => Processors.Remove(x));
            }

            return this;
        }

        /// <summary>
        /// Replaces the first processor with specified <paramref name="processorCode"/> with given <paramref name="replaceWith"/>.
        /// </summary>
        public Bundle ReplaceProcessor(string processorCode, IBundleProcessor replaceWith)
        {
            Guard.NotEmpty(processorCode, nameof(processorCode));
            Guard.NotNull(replaceWith, nameof(replaceWith));

            var processor = Processors.FirstOrDefault(x => x.Code == processorCode);

            if (processor != null)
            {
                var index = Processors.IndexOf(processor);
                Processors.Insert(index, replaceWith);
                Processors.Remove(processor);
            }

            return this;
        }

        /// <summary>
        /// Clears bundle processor list.
        /// </summary>
        public Bundle ClearProcessors()
        {
            Processors.Clear();
            return this;
        }

        /// <summary>
        /// Uses the given file provider for file access.
        /// </summary>
        public Bundle UseFileProvider(IFileProvider provider)
        {
            FileProvider = provider;
            return this;
        }

        /// <summary>
        /// Specifies a set of files to be included in the <see cref="Bundle"/>.
        /// </summary>
        /// <param name="paths">The virtual path of the file or file pattern to be included in the bundle.</param>
        /// <returns>The <see cref="Bundle"/> object itself for use in subsequent method chaining.</returns>
        /// <remarks>
        /// To bundle all files from a particular folder, you can use globbing patterns like this: "css/**/*.css".
        /// </remarks>
        public virtual Bundle Include(params string[] paths)
        {
            _sourceFiles.AddRange(paths);
            return this;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the cache key associated with this bundle.
        /// </summary>
        public virtual BundleCacheKey GetCacheKey(HttpContext httpContext)
        {
            Guard.NotNull(httpContext);

            var fragments = new Dictionary<string, string>();

            foreach (var processor in Processors)
            {
                try
                {
                    processor.PopulateCacheKey(this, httpContext, fragments);
                }
                catch (Exception ex)
                {
                    throw new Exception($"CacheKey generation failed in '{processor.GetType().FullName}' bundle processor.", ex);
                }
            }

            var key = Route;

            foreach (var fragment in fragments)
            {
                key += $"-{fragment.Key}-{fragment.Value.EmptyNull()}";
            }

            return new BundleCacheKey { Key = key, Fragments = fragments };
        }

        public virtual IEnumerable<BundleFile> EnumerateFiles(HttpContext httpContext, BundlingOptions options)
        {
            Guard.NotNull(httpContext);
            Guard.NotNull(options);

            var fileProvider =
                FileProvider ??
                options.FileProvider ??
                httpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().WebRootFileProvider;

            var files = new Dictionary<string, BundleFile>();

            foreach (var source in SourceFiles)
            {
                foreach (var file in ExpandFile(source, fileProvider, httpContext, options))
                {
                    // To prevent duplicates
                    files[file.Path] = file;
                }
            }

            return files.Values;
        }

        /// <summary>
        /// Loads an included bundle file into memory.
        /// </summary>
        /// <param name="bundleFile">The included file</param>
        /// <returns>The loaded bundle file content.</returns>
        public virtual async Task<AssetContent> LoadContentAsync(BundleFile bundleFile)
        {
            Guard.NotNull(bundleFile, nameof(bundleFile));

            using var stream = bundleFile.File.CreateReadStream();
            var content = await stream.AsStringAsync();

            return new AssetContent
            {
                Content = content,
                Path = bundleFile.Path,
                ContentType = MimeTypes.MapNameToMimeType(bundleFile.Path),
                LastModifiedUtc = bundleFile.File.LastModified,
                FileProvider = bundleFile.FileProvider
            };
        }

        public virtual async Task<BundleResponse> GenerateBundleResponseAsync(BundleContext context)
        {
            Guard.NotNull(context, nameof(context));

            foreach (var processor in Processors)
            {
                await processor.ProcessAsync(context);
            }

            if (context.Content.Count > 1)
            {
                await ConcatProcessor.Instance.ProcessAsync(context);
            }

            var combined = context.Content.FirstOrDefault();

            var response = new BundleResponse
            {
                Route = Route,
                CreationDate = combined?.LastModifiedUtc ?? DateTimeOffset.UtcNow,
                Content = combined?.Content,
                ContentType = ContentType,
                FileProvider = context.Files.FirstOrDefault().FileProvider,
                ProcessorCodes = context.ProcessorCodes.Distinct().ToArray(),
                IncludedFiles = context.IncludedFiles
            };

            return response;
        }

        protected virtual IEnumerable<BundleFile> ExpandFile(string path, IFileProvider fileProvider, HttpContext httpContext, BundlingOptions options)
        {
            var isPattern = path.IndexOf('*') > -1;

            if (!isPattern)
            {
                if (options.EnableMinification == true)
                {
                    var assetBuilder = httpContext.RequestServices.GetService<IPageAssetBuilder>();
                    if (assetBuilder != null)
                    {
                        path = assetBuilder.TryFindMinFile(path, fileProvider);
                    }
                }

                yield return new BundleFile
                {
                    Path = path,
                    File = fileProvider.GetFileInfo(path),
                    FileProvider = fileProvider
                };
            }
            else
            {
                // Process glob pattern
                var fileInfo = fileProvider.GetFileInfo("/");
                var root = fileInfo.PhysicalPath;

                if (root != null)
                {
                    var dir = new DirectoryInfoWrapper(new DirectoryInfo(root));
                    var matcher = new Matcher();
                    matcher.AddInclude(path);
                    var globbingResult = matcher.Execute(dir);
                    var fileMatches = globbingResult.Files.Select(f => f.Path.Replace(root, string.Empty));

                    foreach (var fileMatch in fileMatches)
                    {
                        yield return new BundleFile
                        {
                            Path = fileMatch,
                            File = fileProvider.GetFileInfo(fileMatch),
                            FileProvider = fileProvider
                        };
                    }
                }
            }
        }

        #endregion
    }
}

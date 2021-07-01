using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Smartstore.IO;

namespace Smartstore.Web.Bundling.Processors
{
    public class CssRewriteUrlProcessor : BundleProcessor
    {
        internal const string Code = "urlrewrite";
        internal static readonly CssRewriteUrlProcessor Instance = new(true);

        private static readonly Regex _rgUrl = new(@"url\(['""]?(?<url>[^)]+?)['""]?\)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly bool _inlineFiles;
        private readonly int? _inlineMaxFileSize;

        public CssRewriteUrlProcessor()
            : this(false, null)
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="inlineFiles">
        /// Whether to inline referenced files.
        /// </param>
        /// <param name="inlineMaxFileSize">
        /// The max file size of referenced file to apply inlining (files greater than this value will not be inlined).
        /// </param>
        public CssRewriteUrlProcessor(bool inlineFiles, int? inlineMaxFileSize = 5120)
        {
            _inlineFiles = inlineFiles;
            _inlineMaxFileSize = inlineMaxFileSize;
        }

        public override Task ProcessAsync(BundleContext context)
        {
            if (!context.ProcessorCodes.Contains(Code))
            {
                var webPathBase = context.HttpContext.Request.PathBase.Value;

                foreach (var asset in context.Content)
                {
                    var baseUrl = asset.Path.Substring(0, Path.GetDirectoryName(asset.Path).Length + 1);
                    asset.Content = ConvertUrlsToAbsolute(asset, webPathBase, baseUrl, asset.Content);
                }

                context.ProcessorCodes.Add(Code);
            }

            return Task.CompletedTask;
        }

        internal string ConvertUrlsToAbsolute(AssetContent asset, string webPathBase, string baseUrl, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return content;
            }

            // Replace all urls with absolute urls
            return _rgUrl.Replace(content, ((match) => 
            {
                return "url(\"" + RebaseUrlToAbsolute(asset, webPathBase, baseUrl, match.Groups["url"].Value) + "\")";
            }));
        }

        internal string RebaseUrlToAbsolute(AssetContent asset, string webBasePath, string baseUrl, string url)
        {
            // Don't do anything to invalid urls, absolute urls or embedded images
            if (string.IsNullOrWhiteSpace(url) || 
                string.IsNullOrWhiteSpace(baseUrl) || 
                url.StartsWith('/') ||
                url.StartsWith("data:image"))
            {
                return url;
            }

            var combinedPath = PathUtility.Combine(baseUrl, url).EnsureStartsWith('/');

            if (_inlineFiles)
            {
                var file = asset.FileProvider.GetFileInfo(combinedPath);
                if (file.Exists && (_inlineMaxFileSize == null || file.Length <= _inlineMaxFileSize.Value))
                {
                    var mime = MimeTypes.MapNameToMimeType(file.Name);
                    using var stream = file.CreateReadStream();
                    var base64 = Convert.ToBase64String(stream.ToByteArray());

                    return $"data:{mime};base64,{base64}";
                }
            }

            return webBasePath + combinedPath;
        }
    }
}

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Smartstore.Web.Bundling
{
    [DebuggerDisplay("AssetContent: {Path}")]
    public class AssetContent
    {
        public string Path { get; init; }
        public DateTimeOffset LastModifiedUtc { get; init; }
        public string ContentType { get; init; }
        public IFileProvider FileProvider { get; init; }
        public string Content { get; set; }

        private bool? _isMinified;
        public bool IsMinified
        {
            get => _isMinified ??= RegularExpressions.IsMinFile.IsMatch(Path);
            set => _isMinified = value;
        }
    }

    public class BundleContext
    {
        public Bundle Bundle { get; init; }
        public BundleCacheKey CacheKey { get; init; }
        public HttpContext HttpContext { get; init; }
        public BundlingOptions Options { get; init; }
        public IEnumerable<BundleFile> Files { get; init; }
        public IDictionary<string, object> DataTokens { get; } = new Dictionary<string, object>();
        public IList<AssetContent> Content { get; } = new List<AssetContent>();
        public IList<string> ProcessorCodes { get; } = new List<string>();
        public IList<string> IncludedFiles { get; } = new List<string>();
    }
}

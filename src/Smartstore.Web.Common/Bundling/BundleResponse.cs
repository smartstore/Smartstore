using System.Runtime.Serialization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.FileProviders;

namespace Smartstore.Web.Bundling
{
    /// <summary>
    /// The response data that will be sent in reply to a bundle request.
    /// </summary>
    public class BundleResponse
    {
        private string _content;
        private string _contentHash;

        public BundleResponse()
        {
        }

        public BundleResponse(BundleResponse response)
        {
            Guard.NotNull(response, nameof(response));

            CacheKey = response.CacheKey;
            CacheKeyFragments = response.CacheKeyFragments;
            Route = response.Route;
            Content = response.Content;
            ContentType = response.ContentType;
            ContentHash = response.ContentHash;
            CreationDate = response.CreationDate;
            FileProvider = response.FileProvider;
            ProcessorCodes = response.ProcessorCodes;
            IncludedFiles = response.IncludedFiles;
        }

        /// <summary>
        /// Gets or sets the bundle response cache key.
        /// </summary>
        public string CacheKey { get; set; }

        /// <summary>
        /// Gets or sets the cache key fragments used to generate the cache key (e.g. theme name, store id etc.)
        /// </summary>
        public IDictionary<string, string> CacheKeyFragments { get; set; }

        /// <summary>
        /// Gets or sets the bundle route.
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// Gets or sets the media type that is sent in the HTTP content/type header.
        /// </summary>
        public string ContentType { get; set; }

        public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// The content of the bundle which is sent as the response body.
        /// </summary>
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                _contentHash = null;
            }
        }

        [IgnoreDataMember]
        public string ContentHash
        {
            get => _contentHash ??= (_content != null && _content.Length > 0 ? ComputeHash(_content) : string.Empty);
            set => _contentHash = value;
        }

        /// <summary>
        /// Gets or sets the path list of files in the bundle.
        /// </summary>
        public IEnumerable<string> IncludedFiles { get; set; }

        /// <summary>
        /// Codes of processors that have been applied.
        /// </summary>
        public string[] ProcessorCodes { get; set; } = Array.Empty<string>();

        [IgnoreDataMember]
        public IFileProvider FileProvider { get; set; }

        internal static string ComputeHash(string content)
        {
            using var algo = SHA1.Create();
            byte[] hash = algo.ComputeHash(content.GetBytes());
            return WebEncoders.Base64UrlEncode(hash);
        }
    }
}

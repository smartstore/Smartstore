using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;

namespace Smartstore.Web.Bundling
{
    /// <summary>
    /// The response data that will be sent in reply to a bundle request.
    /// </summary>
    public class BundleResponse
    {
        private string _content;
        private string _contentHash;

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

        [JsonIgnore]
        public string ContentHash
        {
            get => _contentHash ??= (_content != null && _content.Length > 0 ? ComputeHash(_content) : string.Empty);
            set => _contentHash = value;
        }

        /// <summary>
        /// Gets or sets the path list of files in the bundle.
        /// </summary>
        public IEnumerable<string> IncludedFiles { get; set; }

        public string[] ProcessorCodes { get; set; } = Array.Empty<string>();

        [JsonIgnore]
        public IFileProvider FileProvider { get; init; }

        internal static string ComputeHash(string content)
        {
            using var algo = SHA1.Create();
            byte[] hash = algo.ComputeHash(Encoding.UTF8.GetBytes(content));
            return WebEncoders.Base64UrlEncode(hash);
        }
    }
}

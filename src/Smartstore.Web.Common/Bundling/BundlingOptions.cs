using System;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

namespace Smartstore.Web.Bundling
{
    public class BundlingOptions
    {
        public bool? EnableBundling { get; set; }

        public bool? EnableClientCache { get; set; }

        public bool? EnableMemoryCache { get; set; }

        public bool? EnableDiskCache { get; set; }

        public bool? EnableMinification { get; set; }

        public bool? EnableAutoPrefixer { get; set; }

        public string CacheDirectory { get; set; }

        public IFileProvider FileProvider { get; set; }

        /// <summary>
        /// Indicates if files should be compressed for HTTPS requests when the Response Compression middleware is available.
        /// The default value is <see cref="HttpsCompressionMode.Compress"/>.
        /// </summary>
        /// <remarks>
        /// Enabling compression on HTTPS requests for remotely manipulable content may expose security problems.
        /// </remarks>
        public HttpsCompressionMode HttpsCompression { get; set; } = HttpsCompressionMode.Default;
    }
}

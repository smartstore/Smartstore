using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

namespace Smartstore.Web.Bundling
{
    public class BundlingOptions
    {
        public bool? EnableBundling { get; set; }

        public bool? EnableClientCache { get; set; }

        public bool? EnableDiskCache { get; set; }

        public bool? EnableMinification { get; set; }

        public bool? EnableAutoprefixer { get; set; }

        public AutoprefixerOptions Autoprefixer { get; set; } = new AutoprefixerOptions();

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

    public class AutoprefixerOptions
    {
        public bool AlwaysDisableInDevMode { get; set; } = true;
        public IList<string> Browsers { get; set; }
        public bool Cascade { get; set; }
        public bool Add { get; set; } = true;
        public bool Remove { get; set; } = true;
        public bool Supports { get; set; } = true;
        public bool IgnoreUnknownVersions { get; set; }
        public bool Flexbox { get; set; }
        public bool Grid { get; set; }
    }
}

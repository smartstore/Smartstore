#nullable enable

using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Smartstore.Engine
{
    public partial class SmartConfiguration
    {
        public string ApplicationName { get; set; } = default!;
        public bool EnableDynamicDiscovery { get; set; } = true;
        public string ThemesBasePath { get; set; } = "/Themes";
        public string ModulesBasePath { get; set; } = "/Modules";
        public string TempDirectory { get; set; } = "/App_Data/_temp";
        public string TranslateCheckUrl { get; set; } = "https://translate.smartstore.com/StringResource/CheckAvailableResources?version={0}";

        /// <summary>
        /// Set this to "true" if the view engine should also try to resolve view files ending with {CurrentLanguage} (e.g.: MyView.de.cshtml > MyView.cshtml).
        /// </summary>
        public bool EnableLocalizedViews { get; set; }

        public bool EnableViewLocationCacheDuringDebug { get; set; } = true;
        public bool ShowCustomErrors { get; set; } = true;
        public bool EnableLegacyRoutesMapping { get; set; }

        /// <summary>
        /// Set this to "false" if you don't intend to edit view files during runtime. 
        /// Disabling razor runtime compilation uses less resources.
        /// </summary>
        public bool EnableRazorRuntimeCompilation { get; set; } = true;

        /// <summary>
        /// Increase timeout if you run into database related timeout problems during regular operations (Default: 30 sec.)
        /// </summary>
        public int? DbCommandTimeout { get; set; } = 30;

        /// <summary>
        /// Increase timeout if you run into database related timeout problems during migration (Default: 120 sec.)
        /// </summary>
        public int? DbMigrationCommandTimeout { get; set; } = 120;

        /// <summary>
        /// The size of pool for <see cref="Microsoft.EntityFrameworkCore.DbContext"/> instances
        /// </summary>
        public int DbContextPoolSize { get; set; } = 1024;

        /// <summary>
        /// Enables pooling for <see cref="Microsoft.EntityFrameworkCore.IDbContextFactory{TContext}"/>.
        /// </summary>
        public bool UsePooledDbContextFactory { get; set; }

        /// <summary>
        /// The default schema to use for database objects. Set <c>null</c> to use database default schema (e.g. "dbo").
        /// </summary>
        public string? DbDefaultSchema { get; set; }

        /// <summary>
        /// Enabled second level database cache.
        /// </summary>
        public bool UseDbCache { get; set; }

        /// <summary>
        /// If <c>true</c>, optimizes a <see cref="Microsoft.EntityFrameworkCore.DbContext"/> for
        /// accessing large columns (text, ntext, varchar(max) and nvarchar(max)). It enables the
        /// "SequentialAccess" option and uses an optimized method
        /// for converting large text columns into <see cref="string"/> objects.
        /// Default is <c>false</c>.
        /// </summary>
        public bool UseSequentialDbDataReader { get; set; }

        /// <summary>
        /// Sets the MS SQL Server compatibility level that EF Core will use when interacting
        /// with the database. This allows configuring EF Core to work with older (or newer)
        /// versions of SQL Server. Defaults to 160 (SQL Server 2022).
        /// Allowed levels are (without the version in brackets):
        /// <list type="table">
        ///     <item>null (auto)</item>
        ///     <item>160 (2022)</item>
        ///     <item>150 (2019)</item>
        ///     <item>140 (2017)</item>
        ///     <item>130 (2016)</item>
        ///     <item>120 (2014)</item>
        ///     <item>110 (2012)</item>
        ///     <item>100 (2008)</item>
        /// </list>
        /// </summary>
        public int? SqlServerCompatLevel { get; set; }

        /// <summary>
        /// Storage path for media files and assets like thumbs, uploads, email attachments etc.
        /// Must be either an app local relative path or a fully qualified physical path to a shared location. E.g.:
        ///     - "Media" or "/Media" points to the subfolder named "Media" in your application root
        ///     - "F:\SharedMedia" points to a (mapped network) drive
        ///     - "\\Server1\SharedMedia" points to a network drive.
        /// Leave empty if path should be "App_Data/Tenants/{Tenant}/Media".
        /// In web farms, you should specify the same UNC or mapped network drive on each server.
        /// </summary>
        public string? MediaStoragePath { get; set; } = default!;

        /// <summary>
        /// Public base path to the media storage used to generate URLs for output HTML.
        /// E.g.: "media" (default), "static", "storage/files" etc.
        /// </summary>
        public string MediaPublicPath { get; set; } = "media";

        /// <summary>
        /// A value defining the maximum size of the image processor
        /// internal memory pool in Megabytes. <c>null</c> means platform default.
        /// </summary>
        public int? ImagingMaxPoolSizeMB { get; set; }

        /// <summary>
        /// List of module names to ignore during app installation.
        /// </summary>
        public string[]? IgnoredModules { get; set; }

        /// <summary>
        /// Task Scheduler poll interval in minutes (1 recommended)
        /// </summary>
        public int TaskSchedulerPollInterval { get; set; } = 1;

        /// <summary>
        /// Set this to your store's task scheduler base url
        /// when the automatic url resolution fails (e.g. 'http://www.mystore.com')
        /// </summary>
        public string? TaskSchedulerBaseUrl { get; set; }

        /// <summary>
        /// Set this to your store's PDF engine base url
        /// when the automatic url resolution fails (e.g. 'http://www.mystore.com')
        /// </summary>
        public string? PdfEngineBaseUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to store TempData in a cookie.
        /// By default the session-based TempData provider is used to store TempData in user session.
        /// </summary>
        public bool UseCookieTempDataProvider { get; set; }

        /// <summary>
        /// Gets or sets the max-age parameter of the Strict-Transport-Security header in days. Default = 365 days.
        /// </summary>
        public int HstsMaxAge { get; set; } = 365;

        /// <summary>
        /// Use with care!
        /// </summary>
        public string? EngineType { get; set; } = typeof(SmartEngine).FullName;

        /// <summary>
        /// Gets or sets a value indicating whether to display exception
        /// always with full stack trace and other infos, even in production mode.
        /// Default is <c>null</c> (fall back to current host's environment name).
        /// </summary>
        public bool? UseDeveloperExceptionPage { get; set; }

        /// <summary>
        /// Gets or sets the minimum length of time between successive scans for expired items in memory cache.
        /// Default is 1 minute.
        /// </summary>
        public TimeSpan? MemoryCacheExpirationScanFrequency { get; set; }

        /// <summary>
        /// Gets or sets the maximum size of the memory cache.
        /// </summary>
        public long? MemoryCacheSizeLimit { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed size of any request body in bytes. When set
        /// to null, the maximum request body size is unlimited. This limit has no effect
        /// on upgraded connections which are always unlimited. This can be overridden per-request
        /// via <see cref="Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature"/>.
        /// Defaults to 30,000,000 bytes, which is approximately 28.6MB.
        /// </summary>
        public long? MaxRequestBodySize { get; set; }

        /// <summary>
        /// Gets or sets the timeout for the mail client. Default = 5000 ms.
        /// </summary>
        public int SmtpServerTimeout { get; set; } = 5000;

        /// <summary>
        /// Gets or sets Google configuration options. 
        /// </summary>
        public GoogleConfiguration Google { get; set; } = new();

        /// <summary>
        /// Gets or sets reverse proxy configuration options. 
        /// </summary>
        public ProxyConfiguration ReverseProxy { get; set; } = new();

        public class GoogleConfiguration
        {
            public string RecaptchaWidgetUrl { get; set; } = "https://www.google.com/recaptcha/api.js";
            public string RecaptchaVerifyUrl { get; set; } = "https://www.google.com/recaptcha/api/siteverify";
            public string MapsUrl { get; set; } = "https://maps.google.com/maps?hl={0}&q={1}";
        }

        public class ProxyConfiguration
        {
            /// <summary>
            /// <c>true</c> enables the <see cref="ForwardedHeadersMiddleware"/>.
            /// </summary>
            public bool Enabled { get; set; }

            /// <summary>
            /// Processes the X-Forwarded-For header, which identifies the originating IP address of the client.
            /// </summary>
            public bool ForwardForHeader { get; set; } = true;

            /// <summary>
            /// Processes the X-Forwarded-Proto header, which identifies the protocol (HTTP or HTTPS) the client used to connect.
            /// </summary>
            public bool ForwardProtoHeader { get; set; } = true;

            /// <summary>
            /// Processes the X-Forwarded-Host header, which identifies the original host requested by the client.
            /// </summary>
            public bool ForwardHostHeader { get; set; } = true;

            /// <summary>
            /// Processes the X-Forwarded-Prefix header, which retrieves the value for the path base.
            /// </summary>
            public bool ForwardPrefixHeader { get; set; } = true;

            /// <summary>
            /// Gets or sets the header used to retrieve the originating client IP. Default: "X-Forwarded-For".
            /// </summary>
            public string? ForwardedForHeaderName { get; set; }

            /// <summary>
            /// Gets or sets the header used to retrieve the value for the originating scheme (HTTP/HTTPS). Default: "X-Forwarded-Proto".
            /// </summary>
            public string? ForwardedProtoHeaderName { get; set; }

            /// <summary>
            ///  Gets or sets the header used to retrieve the original value of the Host header field. Default: "X-Forwarded-Host".
            /// </summary>
            public string? ForwardedHostHeaderName { get; set; }

            /// <summary>
            ///  Gets or sets the header used to retrieve the value for the path base. Default: "X-Forwarded-Prefix".
            /// </summary>
            public string? ForwardedPrefixHeaderName { get; set; }

            /// <summary>
            /// Addresses of known proxies to accept forwarded headers from.
            /// </summary>
            public string[]? KnownProxies { get; set; }

            /// <summary>
            /// The allowed values from X-Forwarded-Host. If the list is empty then all hosts are allowed.
            /// Failing to restrict these values may allow an attacker to spoof links generated by your service.
            /// </summary>
            /// <remarks>
            /// <list type="bullet">
            /// <item><description>Port numbers must be excluded.</description></item>
            /// <item><description>A top level wildcard "*" allows all non-empty hosts.</description></item>
            /// <item><description>Subdomain wildcards are permitted. E.g. "*.example.com" matches subdomains like foo.example.com, but not the parent domain example.com.</description></item>
            /// <item><description>Unicode host names are allowed but will be converted to punycode for matching.</description></item>
            /// <item><description>IPv6 addresses must include their bounding brackets and be in their normalized form.</description></item>
            /// </list>
            /// </remarks>
            public string[]? AllowedHosts { get; set; }
        }
    }
}
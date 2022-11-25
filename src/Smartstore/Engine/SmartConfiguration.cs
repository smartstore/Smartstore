namespace Smartstore.Engine
{
    public partial class SmartConfiguration
    {
        public string ApplicationName { get; set; }
        public bool EnableDynamicDiscovery { get; set; } = true;
        public string ThemesBasePath { get; set; } = "/Themes";
        public string ModulesBasePath { get; set; } = "/Modules";
        public string TempDirectory { get; set; } = "/App_Data/_temp";
        public string TranslateCheckUrl { get; set; } = "https://translate.smartstore.com/StringResource/CheckAvailableResources?version={0}";

        /// <summary>
        /// Monitor all theme folders for file changes/creations/deletions. Only turn this off when you encounter problems on the production webserver.
        /// </summary>
        public bool MonitorThemesFolder { get; set; } = true;

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
        /// The default schema to use for database objects. Set <c>null</c> to use database default schema (e.g. "dbo").
        /// </summary>
        public string DbDefaultSchema { get; set; }

        /// <summary>
        /// Storage path for media files and assets like thumbs, uploads, email attachments etc.
        /// Must be either an app local relative path or a fully qualified physical path to a shared location. E.g.:
        ///     - "Media" or "/Media" points to the subfolder named "Media" in your application root
        ///     - "F:\SharedMedia" points to a (mapped network) drive
        ///     - "\\Server1\SharedMedia" points to a network drive.
        /// Leave empty if path should be "App_Data/Tenants/{Tenant}/Media".
        /// In web farms, you should specify the same UNC or mapped network drive on each server.
        /// </summary>
        public string MediaStoragePath { get; set; }

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
        public string[] IgnoredModules { get; set; }

        /// <summary>
        /// Task Scheduler poll interval in minutes (1 recommended)
        /// </summary>
        public int TaskSchedulerPollInterval { get; set; } = 1;

        /// <summary>
        /// Set this to your store's task scheduler base url
        /// when the automatic url resolution fails (e.g. 'http://www.mystore.com/taskscheduler')
        /// </summary>
        public string TaskSchedulerBaseUrl { get; set; }

        /// <summary>
        /// Set this to your store's PDF engine base url
        /// when the automatic url resolution fails (e.g. 'http://www.mystore.com')
        /// </summary>
        public string PdfEngineBaseUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to store TempData in a cookie.
        /// By default the session-based TempData provider is used to store TempData in user session.
        /// </summary>
        public bool UseCookieTempDataProvider { get; set; }

        /// <summary>
        /// Use with care!
        /// </summary>
        public string EngineType { get; set; } = typeof(SmartEngine).FullName;

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
        ///  Gets or sets the maximum allowed size of any request body in bytes. When set
        ///  to null, the maximum request body size is unlimited. This limit has no effect
        ///  on upgraded connections which are always unlimited. This can be overridden per-request
        ///  via <see cref="Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature"/>.
        ///  Defaults to 30,000,000 bytes, which is approximately 28.6MB.
        /// </summary>
        public long? MaxRequestBodySize { get; set; }

        /// <summary>
        /// Gets or sets the timeout for the mail client. Default = 5000 ms.
        /// </summary>
        public int SmtpServerTimeout { get; set; } = 5000;

        public GoogleConfiguration Google { get; set; } = new();

        public class GoogleConfiguration
        {
            public string RecaptchaWidgetUrl { get; set; }
            public string RecaptchaVerifyUrl { get; set; }
            public string MapsUrl { get; set; }
        }
    }
}
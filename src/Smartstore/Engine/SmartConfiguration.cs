namespace Smartstore.Engine
{
    /// <summary>
    /// Memory managers are used to allocate memory for image processing operations.
    /// </summary>
    public enum ImagingMemoryAllocation
    {
        /// <summary>
        /// For environments with very limited memory capabilities, only small buffers like
        //  image rows are pooled.
        /// </summary>
        Minimal,

        /// <summary>
        /// For environments with limited memory capabilities, only small array requests
        //  are pooled, which can result in reduced throughput.
        /// </summary>
        Moderate,

        /// <summary>
        /// Should be good for most use cases.
        /// </summary>
        Default,

        /// <summary>
        /// For environments where memory capabilities are not an issue, the maximum amount
        //  of array requests are pooled which results in optimal throughput.
        /// </summary>
        Aggressive
    }

    public partial class SmartConfiguration
    {
        public string ApplicationName { get; set; } = "Smartstore";
        public bool EnableDynamicDiscovery { get; set; } = true;
        public string ThemesBasePath { get; set; } = "/Themes";
        public string ModulesBasePath { get; set; } = "/Plugins";
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
        public int DbContextPoolSize { get; set; } = 128;

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
        /// The amount of memory allocated for image processing operations.
        /// </summary>
        public ImagingMemoryAllocation ImagingMemoryAllocation { get; set; } = ImagingMemoryAllocation.Minimal;

        public string[] PluginsIgnoredDuringInstallation { get; set; }

        /// <summary>
        /// Task Scheduler sweep interval in minutes (1 recommended)
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
        /// Default is False.
        /// </summary>
        public bool UseDeveloperExceptionPage { get; set; }

        public GoogleConfiguration Google { get; set; } = new();

        public class GoogleConfiguration
        {
            public string RecaptchaWidgetUrl { get; set; }
            public string RecaptchaVerifyUrl { get; set; }
            public string MapsUrl { get; set; }
        }
    }
}
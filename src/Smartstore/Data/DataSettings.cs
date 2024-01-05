using Smartstore.Data.Providers;
using Smartstore.Engine;
using Smartstore.IO;
using Smartstore.Threading;

namespace Smartstore.Data
{
    public enum DbSystemType
    {
        Unknown,
        SqlServer,
        MySql,
        PostgreSql,
        SQLite
    }

    public partial class DataSettings
    {
        private static Func<IApplicationContext, DataSettings> _settingsFactory = new(x => new DataSettings());
        private static Action<DataSettings> _loadedCallback;
        private static readonly ReaderWriterLockSlim _rwLock = new();

        private static IApplicationContext _appContext;
        private static DataSettings _instance;
        private static bool? _installed;
        private static bool _testMode;

        protected const char SettingSeparator = ':';
        protected const string SettingsFileName = "Settings.txt";

        #region Static members

        public static void SetApplicationContext(IApplicationContext appContext, Action<DataSettings> loaded = null)
        {
            Guard.NotNull(appContext);

            Interlocked.Exchange(ref _appContext, appContext);
            Interlocked.Exchange(ref _loadedCallback, loaded);
        }

        public static void ReplaceFactory(Func<IApplicationContext, DataSettings> factory)
        {
            Guard.NotNull(factory);

            Interlocked.Exchange(ref _settingsFactory, factory);
        }

        public static DataSettings Instance
        {
            get
            {
                using (_rwLock.GetUpgradeableReadLock())
                {
                    if (_instance == null)
                    {
                        using (_rwLock.GetWriteLock())
                        {
                            if (_instance == null)
                            {
                                if (_appContext == null)
                                {
                                    throw new SystemException($"Missing '{nameof(IApplicationContext)}' instance. Please call '{nameof(DataSettings.SetApplicationContext)}' and pass a valid context before accessing '{nameof(DataSettings.Instance)}'.");
                                }

                                _instance = _settingsFactory(_appContext);
                                _instance.Load();
                                _loadedCallback?.Invoke(_instance);
                            }
                        }
                    }
                }

                return _instance;
            }
            internal set
            {
                // For unit-tests
                Interlocked.Exchange(ref _instance, value);
            }
        }

        public static bool DatabaseIsInstalled()
        {
            if (_testMode)
            {
                return false;
            }

            _installed ??= Instance.IsValid();

            return _installed.Value;
        }

        internal static void SetTestMode(bool isTestMode)
        {
            _testMode = isTestMode;
            _installed = null;
        }

        public static void Reload()
        {
            using (_rwLock.GetWriteLock())
            {
                _instance = null;
                _installed = null;
            }
        }

        public static void Delete()
        {
            if (_instance?.TenantRoot != null)
            {
                using (_rwLock.GetWriteLock())
                {
                    try
                    {
                        _instance.TenantRoot.ClearDirectory(_instance.TenantRoot.GetDirectory(""), false, TimeSpan.Zero);
                    }
                    finally
                    {
                        _installed = null;
                        _instance = null;
                    }
                }
            }
        }

        #endregion

        #region Instance members

        internal DataSettings()
        {
            // Internal for unit-test purposes
        }

        public IDictionary<string, string> RawDataSettings { get; } = new Dictionary<string, string>();

        public string TenantName { get; internal set; }

        public IFileSystem TenantRoot { get; internal set; }

        public Version AppVersion { get; set; }

        public DbFactory DbFactory { get; internal set; }

        public string ConnectionString { get; set; }

        /// <summary>
        /// Volatile custom database collation.
        /// Relevant during installation only.
        /// </summary>
        internal string Collation { get; set; }

        public bool IsValid()
            => DbFactory != null && ConnectionString.HasValue();

        protected virtual bool Load()
        {
            using (_rwLock.GetWriteLock())
            {
                Reset();

                (TenantName, TenantRoot) = ResolveTenant();

                if (TenantRoot == null)
                {
                    return false;
                }

                if (TenantRoot.FileExists(SettingsFileName) && !_testMode)
                {
                    string text = TenantRoot.ReadAllText(SettingsFileName);
                    var settings = ParseSettings(text);
                    if (settings.Any())
                    {
                        RawDataSettings.AddRange(settings);

                        // AppVersion
                        var currentVersion = SmartstoreVersion.Version;
                        if (!Version.TryParse(settings.Get("AppVersion"), out var previousVersion))
                        {
                            previousVersion = currentVersion;
                        }

                        AppVersion = previousVersion;
                        var shouldSave = currentVersion > previousVersion;

                        // DbFactory
                        DbFactory = DbFactory.Load(settings.Get("DataProvider"), _appContext.TypeScanner);

                        // ConnectionString
                        var connectionString = settings.Get("DataConnectionString");
                        if (DbFactory.TryNormalizeConnectionString(connectionString, out var normalizedConnectionString))
                        {
                            connectionString = normalizedConnectionString;
                            shouldSave = true;
                        }

                        ConnectionString = connectionString;

                        if (shouldSave)
                        {
                            new DataSettings 
                            { 
                                TenantName = TenantName,
                                TenantRoot = TenantRoot,
                                AppVersion = currentVersion,
                                ConnectionString = ConnectionString,
                                DbFactory = DbFactory
                            }.Save();
                        }

                        return IsValid();
                    }
                }

                return false;
            }
        }

        public void Reset()
        {
            using (_rwLock.GetWriteLock())
            {
                RawDataSettings.Clear();
                TenantName = null;
                TenantRoot = null;
                AppVersion = null;
                DbFactory = null;
                ConnectionString = null;

                _installed = null;
            }
        }

        public virtual bool Save()
        {
            if (!IsValid())
            {
                return false;
            } 

            using (_rwLock.GetWriteLock())
            {
                TenantRoot.WriteAllText(SettingsFileName, SerializeSettings());
                return true;
            }
        }

        #endregion

        #region Instance helpers

        protected virtual (string name, IFileSystem root) ResolveTenant()
        {
            if (_appContext.AppDataRoot == null)
            {
                return default;
            }

            var fs = _appContext.AppDataRoot;
            var tenantsBaseDir = "Tenants";
            var curTenantFile = PathUtility.Join(tenantsBaseDir, "current.txt");

            string curTenant = null;

            if (fs.FileExists(curTenantFile))
            {
                curTenant = fs.ReadAllText(curTenantFile).EmptyNull().Trim().NullEmpty();
                if (curTenant != PathUtility.SanitizePath(curTenant))
                {
                    // File contains invalid path string
                    curTenant = null;
                }

                if (curTenant != null && !fs.DirectoryExists(PathUtility.Join(tenantsBaseDir, curTenant)))
                {
                    // Specified Tenant directory does not exist
                    curTenant = null;
                }
            }

            curTenant ??= "Default";

            var tenantPath = PathUtility.Join(tenantsBaseDir, curTenant);

            if (curTenant.EqualsNoCase("Default") && !fs.DirectoryExists(tenantPath))
            {
                // The Default tenant dir should be created if it doesn't exist
                fs.TryCreateDirectory(tenantPath);
            }

            var tenantRoot = new LocalFileSystem(Path.GetFullPath(Path.Combine(fs.Root, tenantPath)));

            return (curTenant.TrimEnd('/'), tenantRoot);
        }

        protected virtual Dictionary<string, string> ParseSettings(string text)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (text.IsEmpty())
                return result;

            var settings = new List<string>();
            using (var reader = new StringReader(text))
            {
                string str;
                while ((str = reader.ReadLine()) != null)
                    settings.Add(str);
            }

            foreach (var setting in settings)
            {
                var separatorIndex = setting.IndexOf(SettingSeparator);
                if (separatorIndex == -1)
                {
                    continue;
                }
                string key = setting.Substring(0, separatorIndex).Trim();
                string value = setting[(separatorIndex + 1)..].Trim();

                if (key.HasValue() && value.HasValue())
                {
                    result.Add(key, value);
                }
            }

            return result;
        }

        protected virtual string SerializeSettings()
        {
            return string.Format("AppVersion: {0}{3}DataProvider: {1}{3}DataConnectionString: {2}{3}",
                AppVersion.ToString(),
                DbFactory.DbSystem,
                ConnectionString,
                Environment.NewLine);
        }

        #endregion
    }
}

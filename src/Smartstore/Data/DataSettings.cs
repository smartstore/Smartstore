using System.Reflection;
using System.Runtime.Loader;
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
        Sqlite
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

        protected const char SEPARATOR = ':';
        protected const string SETTINGS_FILENAME = "Settings.txt";

        #region Static members

        public static void SetApplicationContext(IApplicationContext appContext, Action<DataSettings> loaded = null)
        {
            Guard.NotNull(appContext, nameof(appContext));

            Interlocked.Exchange(ref _appContext, appContext);
            Interlocked.Exchange(ref _loadedCallback, loaded);
        }

        public static void ReplaceFactory(Func<IApplicationContext, DataSettings> factory)
        {
            Guard.NotNull(factory, nameof(factory));

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
                                    throw new SmartException($"Missing '{nameof(IApplicationContext)}' instance. Please call '{nameof(DataSettings.SetApplicationContext)}' and pass a valid context before accessing '{nameof(DataSettings.Instance)}'.");
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
                return false;

            if (_installed == null)
            {
                _installed = Instance.IsValid();
            }

            return _installed.Value;
        }

        internal static void SetTestMode(bool isTestMode)
        {
            _testMode = isTestMode;
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
                    if (_instance.TenantRoot.TryDeleteFile(SETTINGS_FILENAME))
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

                if (TenantRoot.FileExists(SETTINGS_FILENAME) && !_testMode)
                {
                    string text = TenantRoot.ReadAllText(SETTINGS_FILENAME);
                    var settings = ParseSettings(text);
                    if (settings.Any())
                    {
                        RawDataSettings.AddRange(settings);

                        DbFactory = DbFactory.Load(settings.Get("DataProvider"), _appContext.TypeScanner);

                        ConnectionString = settings.Get("DataConnectionString");

                        if (settings.ContainsKey("AppVersion"))
                        {
                            AppVersion = new Version(settings["AppVersion"]);
                        }

                        return IsValid();
                    }
                }

                return false;
            }
        }

        private static DbFactory CreateDbFactory(string provider)
        {
            Guard.NotEmpty(provider, nameof(provider));

            var assemblyName = string.Empty;

            switch (provider.ToLowerInvariant())
            {
                case "sqlserver":
                    assemblyName = "Smartstore.Core.dll";
                    break;
                case "mysql":
                    assemblyName = "Smartstore.Data.MySql.dll";
                    break;
            }

            if (assemblyName.IsEmpty())
            {
                throw new SmartException($"Unknown database provider type name '${provider}'.");
            }

            var binPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assemblyPath = Path.Combine(binPath, assemblyName);
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);

            var dbFactoryType = _appContext.TypeScanner.FindTypes<DbFactory>(new[] { assembly }).FirstOrDefault();
            if (dbFactoryType == null)
            {
                throw new SmartException($"The data provider assembly '${assemblyName}' does not contain any concrete '${typeof(DbFactory)}' implementation.");
            }

            return (DbFactory)Activator.CreateInstance(dbFactoryType);
        }

        protected void Reset()
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
                return false;

            using (_rwLock.GetWriteLock())
            {
                TenantRoot.WriteAllText(SETTINGS_FILENAME, SerializeSettings());
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
            var curTenantFile = fs.PathCombine(tenantsBaseDir, "current.txt");

            string curTenant = null;

            if (fs.FileExists(curTenantFile))
            {
                curTenant = fs.ReadAllText(curTenantFile).EmptyNull().Trim().NullEmpty();
                if (curTenant != curTenant.EmptyNull().ToValidPath())
                {
                    // File contains invalid path string
                    curTenant = null;
                }

                if (curTenant != null && !fs.DirectoryExists(fs.PathCombine(tenantsBaseDir, curTenant)))
                {
                    // Specified Tenant directory does not exist
                    curTenant = null;
                }
            }

            curTenant ??= "Default";

            var tenantPath = fs.PathCombine(tenantsBaseDir, curTenant);

            if (curTenant.EqualsNoCase("Default") && !fs.DirectoryExists(tenantPath))
            {
                // The Default tenant dir should be created if it doesn't exist
                fs.TryCreateDirectory(tenantPath);
            }

            var tenantRoot = new LocalFileSystem(Path.GetFullPath(Path.Combine(fs.Root, tenantPath)));

            return (curTenant.TrimEnd('/'), tenantRoot);
        }

        protected virtual IDictionary<string, string> ParseSettings(string text)
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
                var separatorIndex = setting.IndexOf(SEPARATOR);
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

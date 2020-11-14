using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Smartstore.Data.DataProviders;
using Smartstore.Engine;
using Smartstore.IO;
using Smartstore.Threading;

namespace Smartstore.Data
{
    public enum DataProviderType
    {
        Unknown,
        SqlServer,
        MySql,
        Sqlite
    }

    public partial class DataSettings
    {
        private static Func<IApplicationContext, DataSettings> _settingsFactory = new Func<IApplicationContext, DataSettings>(x => new DataSettings());
        private static Action<DataSettings> _loadedCallback;
        private static readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        private static IApplicationContext _appContext;
        private static DataSettings _instance;
        private static bool? _installed;
        private static bool _testMode;

        protected const char SEPARATOR = ':';
        protected const string FILENAME = "Settings.txt";

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
                    if (_instance.TenantRoot.TryDeleteFile(FILENAME))
                    {
                        _installed = null;
                        _instance = null;
                    }
                }
            }
        }

        #endregion

        #region Instance members

        private DataSettings()
        {
        }

        public IDictionary<string, string> RawDataSettings { get; } = new Dictionary<string, string>();

        public string TenantName { get; private set; }

        public IFileSystem TenantRoot { get; private set; }

        public Version AppVersion { get; set; }

        public DataProviderType DataProviderType { get; set; }

        public Type DataProviderClrType { get; set; }

        public string ConnectionString { get; set; }

        // TODO: (core) Do we still need DataSettings.DataConnectionType?
        //public string DataConnectionType { get; set; }

        public bool IsValid()
            => DataProviderType > DataProviderType.Unknown && ConnectionString.HasValue();

        // TODO: (core) Do we still need DataSettings.ProviderInvariantName and ProviderFriendlyName?
        //public string ProviderInvariantName
        //{
        //    get
        //    {
        //        if (this.DataProvider.HasValue() && this.DataProvider.IsCaseInsensitiveEqual("sqlserver"))
        //            return "System.Data.SqlClient";

        //        // SqlCe should always be the default provider
        //        return "System.Data.SqlServerCe.4.0";
        //    }
        //}

        //public string ProviderFriendlyName
        //{
        //    get
        //    {
        //        if (this.DataProvider.HasValue() && this.DataProvider.IsCaseInsensitiveEqual("sqlserver"))
        //            return "SQL Server";

        //        // SqlCe should always be the default provider
        //        return "SQL Server Compact (SQL CE)";
        //    }
        //}

        protected virtual bool Load()
        {
            using (_rwLock.GetWriteLock())
            {
                this.Reset();
                (TenantName, TenantRoot) = ResolveTenant();

                if (TenantRoot.FileExists(FILENAME) && !_testMode)
                {
                    string text = TenantRoot.ReadAllText(FILENAME);
                    var settings = ParseSettings(text);
                    if (settings.Any())
                    {
                        RawDataSettings.AddRange(settings);

                        (DataProviderType, DataProviderClrType) = ConvertDataProvider(settings.Get("DataProvider"));
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

        private static (DataProviderType, Type) ConvertDataProvider(string provider)
        {
            if (provider.HasValue())
            {
                if (provider.EqualsNoCase("sqlserver"))
                {
                    return (DataProviderType.SqlServer, typeof(SqlServerDataProvider));
                }
                if (provider.EqualsNoCase("mysql"))
                {
                    return (DataProviderType.MySql, typeof(MySqlDataProvider));
                }
            }

            return (DataProviderType.Unknown, null);
        }

        protected void Reset()
        {
            using (_rwLock.GetWriteLock())
            {
                RawDataSettings.Clear();
                TenantName = null;
                TenantRoot = null;
                AppVersion = null;
                DataProviderType = DataProviderType.Unknown;
                DataProviderClrType = null;
                ConnectionString = null;
                //this.DataConnectionType = null;

                _installed = null;
            }
        }

        public virtual bool Save()
        {
            if (!IsValid())
                return false;

            using (_rwLock.GetWriteLock())
            {
                TenantRoot.WriteAllText(FILENAME, SerializeSettings());
                return true;
            }
        }

        #endregion

        #region Instance helpers

        protected virtual (string name, IFileSystem root) ResolveTenant()
        {
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

            var tenantRoot = new LocalFileSystem(fs.GetDirectory(tenantPath).PhysicalPath);

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
                this.AppVersion.ToString(),
                this.DataProviderType,
                this.ConnectionString,
                Environment.NewLine);
        }

        #endregion
    }
}

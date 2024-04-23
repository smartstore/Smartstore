using System.Net;
using Smartstore.Data.Providers;
using Smartstore.Utilities;

namespace Smartstore.Admin.Models.Maintenance
{
    [LocalizedDisplay("Admin.System.SystemInfo.")]
    public class SystemInfoModel : ModelBase
    {
        [LocalizedDisplay("*ASPNETInfo")]
        public string AspNetInfo { get; set; }

        [LocalizedDisplay("*AppVersion")]
        public string AppVersion { get; set; }

        [LocalizedDisplay("*AppDate")]
        public DateTime AppDate { get; set; }

        [LocalizedDisplay("*OperatingSystem")]
        public string OperatingSystem { get; set; }

        [LocalizedDisplay("*ServerLocalTime")]
        public DateTime ServerLocalTime { get; set; }

        [LocalizedDisplay("*ServerTimeZone")]
        public string ServerTimeZone { get; set; }

        [LocalizedDisplay("Admin.System.Log.Fields.IPAddress")]
        public IPAddress IPAddress { get; set; }

        [LocalizedDisplay("*UTCTime")]
        public DateTime UtcTime { get; set; }

        [LocalizedDisplay("*LoadedAssemblies")]
        public List<LoadedAssembly> LoadedAssemblies { get; set; } = [];

        [LocalizedDisplay("*DatabaseSize")]
        public long DatabaseSize { get; set; }
        public string DatabaseSizeString => DatabaseSize == 0 ? string.Empty : Prettifier.HumanizeBytes(DatabaseSize);

        [LocalizedDisplay("*DbTableInfo")]
        public List<DbTableInfo> DbTableInfos { get; set; } = [];

        [LocalizedDisplay("*UsedMemorySize")]
        public long UsedMemorySize { get; set; }
        public string UsedMemorySizeString => Prettifier.HumanizeBytes(UsedMemorySize);

        [LocalizedDisplay("*DataProviderFriendlyName")]
        public string DataProviderFriendlyName { get; set; }

        public bool OptimizeDatabaseEnabled { get; set; }
        public bool OptimizeTableEnabled { get; set; }

        [Obsolete("Too fragile in .NET Core")]
        public Dictionary<string, long> MemoryCacheStats { get; set; } = [];

        public class LoadedAssembly
        {
            public string FullName { get; set; }
            public string Location { get; set; }
        }

    }
}

using Smartstore.Licensing;

namespace Smartstore.Admin.Models.Modularity
{
    public class LicenseLabelModel : ModelBase
    {
        public string LicenseUrl { get; set; }
        public bool IsLicensable { get; set; }
        public bool HideLabel { get; set; }
        public LicensingState LicenseState { get; set; }
        public string TruncatedLicenseKey { get; set; }
        public int? RemainingDemoUsageDays { get; set; }
    }

    public class LicenseModuleModel : ModelBase
    {
        public string SystemName { get; set; }
        public int InvalidDataStoreId { get; set; }
        public List<StoreLicenseModel> StoreLicenses { get; set; }
    }

    public class StoreLicenseModel
    {
        public LicenseLabelModel LicenseLabel { get; set; } = new();

        [LocalizedDisplay("Admin.Configuration.Plugins.LicenseKey")]
        public string LicenseKey { get; set; }

        public int StoreId { get; set; }
        public string StoreName { get; set; }
        public string StoreUrl { get; set; }
    }
}

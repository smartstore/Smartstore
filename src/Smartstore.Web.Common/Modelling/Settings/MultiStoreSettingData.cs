namespace Smartstore.Web.Modelling.Settings
{
    public class MultiStoreSettingData
    {
        public int ActiveStoreScopeConfiguration { get; set; }
        public HashSet<string> OverrideSettingKeys { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public string RootSettingClass { get; set; }
    }
}

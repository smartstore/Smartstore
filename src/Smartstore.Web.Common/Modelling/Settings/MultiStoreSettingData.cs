namespace Smartstore.Web.Modelling.Settings
{
    public class MultiStoreSettingData
    {
        public int ActiveStoreScopeConfiguration { get; set; }
        public HashSet<string> OverrideSettingKeys { get; } = new(StringComparer.OrdinalIgnoreCase);
    }
}

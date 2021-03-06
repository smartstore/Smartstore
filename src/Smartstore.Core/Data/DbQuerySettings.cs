namespace Smartstore.Core.Data
{
    public class DbQuerySettings
    {
        public DbQuerySettings(bool ignoreAcl, bool ignoreMultiStore)
        {
            IgnoreAcl = ignoreAcl;
            IgnoreMultiStore = ignoreMultiStore;
        }

        public bool IgnoreAcl { get; }
        public bool IgnoreMultiStore { get; }

        public static DbQuerySettings Default { get; } = new DbQuerySettings(false, false);
    }
}

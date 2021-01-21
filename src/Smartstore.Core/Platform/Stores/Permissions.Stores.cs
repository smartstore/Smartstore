namespace Smartstore.Core.Security
{
    public static partial class Permissions
    {
        public static partial class Configuration
        {
            public static class Store
            {
                public const string Self = "configuration.store";
                public const string Read = "configuration.store.read";
                public const string Update = "configuration.store.update";
                public const string Create = "configuration.store.create";
                public const string Delete = "configuration.store.delete";
                public const string ReadStats = "configuration.store.readstats";
            }
        }
    }
}

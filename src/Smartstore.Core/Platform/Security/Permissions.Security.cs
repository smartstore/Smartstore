namespace Smartstore.Core.Security
{
    /// <summary>
    /// Provides system names of standard permissions.
    /// Usage: [Permission(Permissions.Customer.Read)]
    /// </summary>
    public static partial class Permissions
    {
        public static partial class Cms
        {
            public const string Self = "cms";

            public static class Widget
            {
                public const string Self = "cms.widget";
                public const string Read = "cms.widget.read";
                public const string Update = "cms.widget.update";
                public const string Activate = "cms.widget.activate";
            }
        }

        public static partial class Configuration
        {
            public const string Self = "configuration";

            public static class Authentication
            {
                public const string Self = "configuration.authentication";
                public const string Read = "configuration.authentication.read";
                public const string Update = "configuration.authentication.update";
                public const string Activate = "configuration.authentication.activate";
            }

            public static class Acl
            {
                public const string Self = "configuration.acl";
                public const string Read = "configuration.acl.read";
                public const string Update = "configuration.acl.update";
            }

            public static class Module
            {
                public const string Self = "configuration.plugin";
                public const string Read = "configuration.plugin.read";
                public const string Update = "configuration.plugin.update";
                public const string Upload = "configuration.plugin.upload";
                public const string Install = "configuration.plugin.install";
                public const string License = "configuration.plugin.license";
            }
        }

        public static partial class System
        {
            public const string Self = "system";
            public const string AccessBackend = "system.accessbackend";
            public const string AccessShop = "system.accessshop";

            public static class Maintenance
            {
                public const string Self = "system.maintenance";
                public const string Read = "system.maintenance.read";
                public const string Execute = "system.maintenance.execute";
            }
        }
    }
}

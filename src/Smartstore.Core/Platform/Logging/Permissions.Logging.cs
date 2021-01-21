namespace Smartstore.Core.Security
{
    public static partial class Permissions
    {
        public static partial class Configuration
        {
            public static class ActivityLog
            {
                public const string Self = "configuration.activitylog";
                public const string Read = "configuration.activitylog.read";
                public const string Update = "configuration.activitylog.update";
                public const string Delete = "configuration.activitylog.delete";
            }
        }

        public static partial class System
        {
            public static class Log
            {
                public const string Self = "system.log";
                public const string Read = "system.log.read";
                public const string Delete = "system.log.delete";
            }
        }
    }
}

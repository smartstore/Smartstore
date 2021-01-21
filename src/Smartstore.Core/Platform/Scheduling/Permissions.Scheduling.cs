namespace Smartstore.Core.Security
{
    public static partial class Permissions
    {
        public static partial class System
        {
            public static class ScheduleTask
            {
                public const string Self = "system.scheduletask";
                public const string Read = "system.scheduletask.read";
                public const string Update = "system.scheduletask.update";
                public const string Delete = "system.scheduletask.delete";
                public const string Execute = "system.scheduletask.execute";
            }
        }
    }
}

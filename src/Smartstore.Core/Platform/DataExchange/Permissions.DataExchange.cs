namespace Smartstore.Core.Security
{
    public static partial class Permissions
    {
        public static partial class Configuration
        {
            public static class Export
            {
                public const string Self = "configuration.export";
                public const string Read = "configuration.export.read";
                public const string Update = "configuration.export.update";
                public const string Create = "configuration.export.create";
                public const string Delete = "configuration.export.delete";
                public const string Execute = "configuration.export.execute";
                public const string CreateDeployment = "configuration.export.createdeployment";
            }

            public static class Import
            {
                public const string Self = "configuration.import";
                public const string Read = "configuration.import.read";
                public const string Update = "configuration.import.update";
                public const string Create = "configuration.import.create";
                public const string Delete = "configuration.import.delete";
                public const string Execute = "configuration.import.execute";
            }
        }
    }
}

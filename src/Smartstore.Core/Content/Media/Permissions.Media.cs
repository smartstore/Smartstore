namespace Smartstore.Core.Security
{
    public static partial class Permissions
    {
        public static class Media
        {
            public const string Self = "media";
            public const string Update = "media.update";
            public const string Delete = "media.delete";
            public const string Upload = "media.upload";

            public static class Download
            {
                public const string Self = "media.download";
                public const string Read = "media.download.read";
                public const string Update = "media.download.update";
                public const string Create = "media.download.create";
                public const string Delete = "media.download.delete";
            }
        }
    }
}

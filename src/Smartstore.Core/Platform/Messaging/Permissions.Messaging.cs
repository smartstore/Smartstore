namespace Smartstore.Core.Security
{
    public static partial class Permissions
    {
        public static partial class Promotion
        {
            public static class Campaign
            {
                public const string Self = "promotion.campaign";
                public const string Read = "promotion.campaign.read";
                public const string Update = "promotion.campaign.update";
                public const string Create = "promotion.campaign.create";
                public const string Delete = "promotion.campaign.delete";
            }

            public static class Newsletter
            {
                public const string Self = "promotion.newsletter";
                public const string Read = "promotion.newsletter.read";
                public const string Update = "promotion.newsletter.update";
                public const string Delete = "promotion.newsletter.delete";
            }
        }

        public static partial class Cms
        {
            public static class MessageTemplate
            {
                public const string Self = "cms.messagetemplate";
                public const string Read = "cms.messagetemplate.read";
                public const string Update = "cms.messagetemplate.update";
                public const string Create = "cms.messagetemplate.create";
                public const string Delete = "cms.messagetemplate.delete";
            }
        }

        public static partial class Configuration
        {
            public static class EmailAccount
            {
                public const string Self = "configuration.emailaccount";
                public const string Read = "configuration.emailaccount.read";
                public const string Update = "configuration.emailaccount.update";
                public const string Create = "configuration.emailaccount.create";
                public const string Delete = "configuration.emailaccount.delete";
            }
        }

        public static partial class System
        {
            public static class Message
            {
                public const string Self = "system.message";
                public const string Read = "system.message.read";
                public const string Update = "system.message.update";
                public const string Create = "system.message.create";
                public const string Delete = "system.message.delete";
                public const string Send = "system.message.send";
            }
        }
    }
}

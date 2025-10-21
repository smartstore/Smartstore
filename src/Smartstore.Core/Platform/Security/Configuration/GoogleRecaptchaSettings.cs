using Smartstore.Core.Configuration;

namespace Smartstore.Core.Security
{
    public class GoogleRecaptchaSettings : ISettings
    {
        public const string DefaultWidgetUrl = "https://www.google.com/recaptcha/api.js";
        public const string DefaultVerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

        public string WidgetUrl { get; set; } = DefaultWidgetUrl;

        public string VerifyUrl { get; set; } = DefaultVerifyUrl;

        public string SiteKey { get; set; }

        public string SecretKey { get; set; }

        public bool IsInvisible { get; set; }
    }
}

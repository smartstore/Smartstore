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

        /// <summary>
        /// v2 | v3
        /// </summary>
        public string Version { get; set; } = "v2";

        /// <summary>
        /// dark | light
        /// </summary>
        public string Theme { get; set; } = "light";

        /// <summary>
        /// normal | compact | invisible
        /// </summary>
        public string Size { get; set; } = "normal";

        public float ScoreThreshold { get; set; } = 0.5f;
    }
}

using Smartstore.Core.Configuration;

namespace Smartstore.Core.Security
{
    public class GoogleRecaptchaSettings : ISettings
    {
        public const string DefaultWidgetUrl = "https://www.google.com/recaptcha/api.js";
        public const string DefaultVerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

        public string WidgetUrl { get; set; }

        public string VerifyUrl { get; set; }

        public string SiteKey { get; set; }

        public string SecretKey { get; set; }

        /// <summary>
        /// v2 | v3
        /// </summary>
        public string Version { get; set; } = "v2";

        public bool UseDarkTheme { get; set; }

        /// <summary>
        /// normal | compact | invisible (only v2)
        /// </summary>
        public string Size { get; set; } = "normal";

        /// <summary>
        /// bottomright | bottomleft | inline | hide (only v2/Size=invisible)
        /// </summary>
        public string BadgePosition { get; set; } = "bottomright";
        public bool HideBadgeV3 { get; set; }

        /// <summary>
        /// Min. score for v3 (0.0 – 1.0). Default: 0.5
        /// </summary>
        public float ScoreThreshold { get; set; } = 0.5f;

        /// <summary>
        /// Expected action for v3, e.g., “submit.” Can be overwritten per use case.
        /// </summary>
        public string DefaultAction { get; set; } = "submit";
    }
}

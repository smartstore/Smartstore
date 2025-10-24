namespace Smartstore.Admin.Models
{
    // TODO: (mc) Change all localization strings
    
    [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.")]
    public class GoogleRecaptchaModel : ModelBase
    {
        public string WidgetUrl { get; set; }

        public string VerifyUrl { get; set; }

        [LocalizedDisplay("*reCaptchaPublicKey")]
        public string SiteKey { get; set; }

        [LocalizedDisplay("*reCaptchaPrivateKey")]
        public string SecretKey { get; set; }

        public string Version { get; set; }

        public string Theme { get; set; }

        //[LocalizedDisplay("*UseInvisibleReCaptcha")]
        public string Size { get; set; }
    }
}

using Smartstore.Core.Configuration;

namespace Smartstore.Core.Security
{
    public class CaptchaSettings : ISettings
    {
        public string ProviderSystemName { get; set; } = GoogleRecaptchaProvider.SystemName;

        public bool Enabled { get; set; }

        // TODO: (mg) (captcha) Migrate to GoogleRecaptchaSettings.SiteKey and remove later
        public string ReCaptchaPublicKey { get; set; }

        // TODO: (mg) (captcha) Migrate to GoogleRecaptchaSettings.SecretKey and remove later
        public string ReCaptchaPrivateKey { get; set; }

        // TODO: (mg) (captcha) Migrate to GoogleRecaptchaSettings.IsInvisible and remove later
        public bool UseInvisibleReCaptcha { get; set; }

        public bool ShowOnLoginPage { get; set; }
        public bool ShowOnRegistrationPage { get; set; }
        public bool ShowOnPasswordRecoveryPage { get; set; }
        public bool ShowOnContactUsPage { get; set; }
        public bool ShowOnEmailWishlistToFriendPage { get; set; }
        public bool ShowOnEmailProductToFriendPage { get; set; }
        public bool ShowOnAskQuestionPage { get; set; }
        public bool ShowOnBlogCommentPage { get; set; }
        public bool ShowOnNewsCommentPage { get; set; }
        public bool ShowOnForumPage { get; set; }
        public bool ShowOnProductReviewPage { get; set; }

        // TODO: (mg) (captcha) Remove later
        public bool CanDisplayCaptcha => Enabled && ReCaptchaPublicKey.HasValue() && ReCaptchaPrivateKey.HasValue();
    }
}

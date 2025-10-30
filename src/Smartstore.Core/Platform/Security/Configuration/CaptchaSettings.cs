using Smartstore.Core.Configuration;

namespace Smartstore.Core.Security
{
    public class CaptchaSettings : ISettings
    {
        public string ProviderSystemName { get; set; } = GoogleRecaptchaProvider.SystemName;

        public bool Enabled { get; set; }

        private string[] _showOn = [];
        public string[] ShowOn
        {
            get => _showOn;
            set
            {
                if (value == null || value.Length == 0)
                {
                    _showOn = [];
                    return;
                }

                _showOn = value
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }
        }

        #region Backward compatibility

        public bool ShowOnLoginPage => IsActiveTarget(Targets.Login);
        public bool ShowOnRegistrationPage => IsActiveTarget(Targets.Registration);
        public bool ShowOnPasswordRecoveryPage => IsActiveTarget(Targets.PasswordRecovery);
        public bool ShowOnContactUsPage => IsActiveTarget(Targets.ContactUs);
        public bool ShowOnEmailWishlistToFriendPage => IsActiveTarget(Targets.ShareWishlist);
        public bool ShowOnEmailProductToFriendPage => IsActiveTarget(Targets.ShareProduct);
        public bool ShowOnAskQuestionPage => IsActiveTarget(Targets.ProductInquiry);
        public bool ShowOnBlogCommentPage => IsActiveTarget(Targets.BlogComment);
        public bool ShowOnNewsCommentPage => IsActiveTarget(Targets.NewsComment);
        public bool ShowOnForumPage => IsActiveTarget(Targets.Forum);
        public bool ShowOnProductReviewPage => IsActiveTarget(Targets.ProductReview);

        public bool IsActiveTarget(string target)
            => Enabled && (target.IsEmpty() || _showOn.Contains(target, StringComparer.OrdinalIgnoreCase));

        #endregion

        #region Targets

        public static class Targets
        {
            public const string Login = "Login";
            public const string Registration = "Registration";
            public const string PasswordRecovery = "PasswordRecovery";
            public const string ContactUs = "ContactUs";
            public const string ShareWishlist = "ShareWishlist";
            public const string ShareProduct = "ShareProduct";
            public const string ProductInquiry = "ProductInquiry";
            public const string BlogComment = "BlogComment";
            public const string NewsComment = "NewsComment";
            public const string Forum = "Forum";
            public const string ProductReview = "ProductReview";

            public static readonly IReadOnlyList<string> All =
            [
                Login,
                Registration,
                PasswordRecovery,
                ContactUs,
                ShareWishlist,
                ShareProduct,
                ProductInquiry,
                BlogComment,
                NewsComment,
                Forum,
                ProductReview
            ];

            public static IDictionary<string, string> GetLegacySettingNames()
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [nameof(ShowOnLoginPage)] = Login,
                    [nameof(ShowOnRegistrationPage)] = Registration,
                    [nameof(ShowOnPasswordRecoveryPage)] = PasswordRecovery,
                    [nameof(ShowOnContactUsPage)] = ContactUs,
                    [nameof(ShowOnEmailWishlistToFriendPage)] = ShareWishlist,
                    [nameof(ShowOnEmailProductToFriendPage)] = ShareProduct,
                    [nameof(ShowOnAskQuestionPage)] = ProductInquiry,
                    [nameof(ShowOnBlogCommentPage)] = BlogComment,
                    [nameof(ShowOnNewsCommentPage)] = NewsComment,
                    [nameof(ShowOnForumPage)] = Forum,
                    [nameof(ShowOnProductReviewPage)] = ProductReview
                };
            }

            const string ResPrefix = "Admin.Configuration.Settings.GeneralCommon.CaptchaShowOnTargets.Option.";
            public static IDictionary<string, string> GetDisplayResourceKeys()
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    [Login] = ResPrefix + "Login",
                    [Registration] = ResPrefix + "Registration",
                    [PasswordRecovery] = ResPrefix + "PasswordRecovery",
                    [ContactUs] = ResPrefix + "ContactUs",
                    [ShareWishlist] = ResPrefix + "ShareWishlist",
                    [ShareProduct] = ResPrefix + "ShareProduct",
                    [ProductInquiry] = ResPrefix + "ProductInquiry",
                    [BlogComment] = ResPrefix + "BlogComment",
                    [NewsComment] = ResPrefix + "NewsComment",
                    [Forum] = ResPrefix + "Forum",
                    [ProductReview] = ResPrefix + "ProductReview"
                };
            }
        }

        #endregion
    }
}

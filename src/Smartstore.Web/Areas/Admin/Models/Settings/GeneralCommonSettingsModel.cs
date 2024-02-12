using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;

namespace Smartstore.Admin.Models
{
    public partial class GeneralCommonSettingsModel : ModelBase
    {
        public StoreInformationSettingsModel StoreInformationSettings { get; set; } = new();
        public DateTimeSettingsModel DateTimeSettings { get; set; } = new();
        public EmailAccountSettingsModel EmailAccountSettings { get; set; } = new();

        [AdditionalMetadata("MetaTitleResKey", "Admin.Configuration.Settings.GeneralCommon.DefaultTitle")]
        [AdditionalMetadata("MetaDescriptionResKey", "Admin.Configuration.Settings.GeneralCommon.DefaultMetaDescription")]
        [AdditionalMetadata("MetaKeywordsResKey", "Admin.Configuration.Settings.GeneralCommon.DefaultMetaKeywords")]
        [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.")]
        public SeoSettingsModel SeoSettings { get; set; } = new();

        [AdditionalMetadata("MetaTitleResKey", "Admin.Configuration.Settings.GeneralCommon.HomepageTitle")]
        [AdditionalMetadata("MetaDescriptionResKey", "Admin.Configuration.Settings.GeneralCommon.HomepageMetaDescription")]
        [AdditionalMetadata("MetaKeywordsResKey", "Admin.Configuration.Settings.GeneralCommon.HomepageMetaKeywords")]
        public HomepageSettingsModel HomepageSettings { get; set; } = new();

        public SecuritySettingsModel SecuritySettings { get; set; } = new();
        public CaptchaSettingsModel CaptchaSettings { get; set; } = new();
        public PdfSettingsModel PdfSettings { get; set; } = new();
        public LocalizationSettingsModel LocalizationSettings { get; set; } = new();
        public CompanyInformationSettingsModel CompanyInformationSettings { get; set; } = new();
        public ContactDataSettingsModel ContactDataSettings { get; set; } = new();
        public BankConnectionSettingsModel BankConnectionSettings { get; set; } = new();
        public SocialSettingsModel SocialSettings { get; set; } = new();

        #region Nested classes

        public partial class HomepageSettingsModel : ISeoModel
        {
            public string MetaTitle { get; set; }

            public string MetaDescription { get; set; }

            public string MetaKeywords { get; set; }

            public List<SeoModelLocal> Locales { get; set; } = new();
        }

        [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.")]
        public partial class StoreInformationSettingsModel
        {
            [LocalizedDisplay("*StoreClosed")]
            public bool StoreClosed { get; set; }

            [LocalizedDisplay("*StoreClosedAllowForAdmins")]
            public bool StoreClosedAllowForAdmins { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.CustomerUser.")]
        public partial class DateTimeSettingsModel
        {
            [LocalizedDisplay("*AllowCustomersToSetTimeZone")]
            public bool AllowCustomersToSetTimeZone { get; set; }

            [LocalizedDisplay("*DefaultStoreTimeZone")]
            public string DefaultStoreTimeZoneId { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.EmailAccount.")]
        public partial class EmailAccountSettingsModel
        {
            [LocalizedDisplay("*DefaultEmailAccountId")]
            public int DefaultEmailAccountId { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.")]
        public partial class SeoSettingsModel : ISeoModel
        {
            [LocalizedDisplay("*PageTitleSeparator")]
            public string PageTitleSeparator { get; set; }

            [LocalizedDisplay("*PageTitleSeoAdjustment")]
            public PageTitleSeoAdjustment PageTitleSeoAdjustment { get; set; }

            public string MetaTitle { get; set; }

            public string MetaDescription { get; set; }

            public string MetaKeywords { get; set; }

            public List<SeoModelLocal> Locales { get; set; } = new();

            [LocalizedDisplay("*MetaRobotsContent")]
            public string MetaRobotsContent { get; set; }

            [LocalizedDisplay("*ConvertNonWesternChars")]
            public bool ConvertNonWesternChars { get; set; }

            [LocalizedDisplay("*AllowUnicodeCharsInUrls")]
            public bool AllowUnicodeCharsInUrls { get; set; }

            [LocalizedDisplay("*SeoNameCharConversion")]
            [UIHint("Textarea")]
            [AdditionalMetadata("rows", 10)]
            public string SeoNameCharConversion { get; set; }

            [LocalizedDisplay("*TestSeoNameCreation")]
            public string TestSeoNameCreation { get; set; }

            [LocalizedDisplay("*CanonicalUrlsEnabled")]
            public bool CanonicalUrlsEnabled { get; set; }

            [LocalizedDisplay("*CanonicalHostNameRule")]
            public CanonicalHostNameRule CanonicalHostNameRule { get; set; }

            [LocalizedDisplay("*AppendTrailingSlashToUrls")]
            public bool AppendTrailingSlashToUrls { get; set; }

            [LocalizedDisplay("*TrailingSlashRule")]
            public TrailingSlashRule TrailingSlashRule { get; set; }

            [LocalizedDisplay("*ExtraRobotsDisallows")]
            [UIHint("Textarea")]
            [AdditionalMetadata("rows", 10)]
            public string ExtraRobotsDisallows { get; set; }

            [LocalizedDisplay("*ExtraRobotsAllows")]
            [UIHint("Textarea")]
            [AdditionalMetadata("rows", 10)]
            public string ExtraRobotsAllows { get; set; }

            [LocalizedDisplay("*ExtraRobotsLines")]
            [UIHint("Textarea")]
            [AdditionalMetadata("rows", 10)]
            public string ExtraRobotsLines { get; set; }

            [LocalizedDisplay("*XmlSitemapEnabled")]
            public bool XmlSitemapEnabled { get; set; }

            [LocalizedDisplay("*XmlSitemapIncludesBlog")]
            public bool XmlSitemapIncludesBlog { get; set; }

            [LocalizedDisplay("*XmlSitemapIncludesCategories")]
            public bool XmlSitemapIncludesCategories { get; set; }

            [LocalizedDisplay("*XmlSitemapIncludesForum")]
            public bool XmlSitemapIncludesForum { get; set; }

            [LocalizedDisplay("*XmlSitemapIncludesManufacturers")]
            public bool XmlSitemapIncludesManufacturers { get; set; }

            [LocalizedDisplay("*XmlSitemapIncludesNews")]
            public bool XmlSitemapIncludesNews { get; set; }

            [LocalizedDisplay("*XmlSitemapIncludesProducts")]
            public bool XmlSitemapIncludesProducts { get; set; }

            [LocalizedDisplay("*XmlSitemapIncludesTopics")]
            public bool XmlSitemapIncludesTopics { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.")]
        public partial class SecuritySettingsModel
        {
            [LocalizedDisplay("*EncryptionKey")]
            public string EncryptionKey { get; set; }

            [LocalizedDisplay("*AdminAreaAllowedIpAddresses")]
            public string AdminAreaAllowedIpAddresses { get; set; }

            [LocalizedDisplay("*HideAdminMenuItemsBasedOnPermissions")]
            public bool HideAdminMenuItemsBasedOnPermissions { get; set; }

            [LocalizedDisplay("*EnableHoneypotProtection")]
            public bool EnableHoneypotProtection { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.")]
        public partial class CaptchaSettingsModel
        {
            [LocalizedDisplay("*CaptchaEnabled")]
            public bool Enabled { get; set; }

            [LocalizedDisplay("*CaptchaShowOnLoginPage")]
            public bool ShowOnLoginPage { get; set; }

            [LocalizedDisplay("*CaptchaShowOnRegistrationPage")]
            public bool ShowOnRegistrationPage { get; set; }

            [LocalizedDisplay("*ShowOnPasswordRecoveryPage")]
            public bool ShowOnPasswordRecoveryPage { get; set; }

            [LocalizedDisplay("*CaptchaShowOnContactUsPage")]
            public bool ShowOnContactUsPage { get; set; }

            [LocalizedDisplay("*CaptchaShowOnEmailWishlistToFriendPage")]
            public bool ShowOnEmailWishlistToFriendPage { get; set; }

            [LocalizedDisplay("*CaptchaShowOnEmailProductToFriendPage")]
            public bool ShowOnEmailProductToFriendPage { get; set; }

            [LocalizedDisplay("*CaptchaShowOnAskQuestionPage")]
            public bool ShowOnAskQuestionPage { get; set; }

            [LocalizedDisplay("*CaptchaShowOnBlogCommentPage")]
            public bool ShowOnBlogCommentPage { get; set; }

            [LocalizedDisplay("*CaptchaShowOnNewsCommentPage")]
            public bool ShowOnNewsCommentPage { get; set; }

            [LocalizedDisplay("*CaptchaShowOnForumPage")]
            public bool ShowOnForumPage { get; set; }

            [LocalizedDisplay("*CaptchaShowOnProductReviewPage")]
            public bool ShowOnProductReviewPage { get; set; }

            [LocalizedDisplay("*reCaptchaPublicKey")]
            public string ReCaptchaPublicKey { get; set; }

            [LocalizedDisplay("*reCaptchaPrivateKey")]
            public string ReCaptchaPrivateKey { get; set; }

            [LocalizedDisplay("*UseInvisibleReCaptcha")]
            public bool UseInvisibleReCaptcha { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.")]
        public partial class PdfSettingsModel
        {
            [LocalizedDisplay("*PdfEnabled")]
            public bool Enabled { get; set; }

            [LocalizedDisplay("*PdfLetterPageSizeEnabled")]
            public bool LetterPageSizeEnabled { get; set; }

            [LocalizedDisplay("*PdfLogo")]
            [UIHint("Media"), AdditionalMetadata("album", "content"), AdditionalMetadata("transientUpload", true)]
            public int LogoPictureId { get; set; }

            [LocalizedDisplay("*AttachOrderPdfToOrderPlacedEmail")]
            public bool AttachOrderPdfToOrderPlacedEmail { get; set; }

            [LocalizedDisplay("*AttachOrderPdfToOrderCompletedEmail")]
            public bool AttachOrderPdfToOrderCompletedEmail { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.")]
        public partial class LocalizationSettingsModel
        {
            [LocalizedDisplay("*UseImagesForLanguageSelection")]
            public bool UseImagesForLanguageSelection { get; set; }

            [LocalizedDisplay("*SeoFriendlyUrlsForLanguagesEnabled")]
            public bool SeoFriendlyUrlsForLanguagesEnabled { get; set; }

            [LocalizedDisplay("*DefaultLanguageRedirectBehaviour")]
            public DefaultLanguageRedirectBehaviour DefaultLanguageRedirectBehaviour { get; set; }

            [LocalizedDisplay("*InvalidLanguageRedirectBehaviour")]
            public InvalidLanguageRedirectBehaviour InvalidLanguageRedirectBehaviour { get; set; }

            [LocalizedDisplay("*DetectBrowserUserLanguage")]
            public bool DetectBrowserUserLanguage { get; set; }

            [LocalizedDisplay("*DisplayRegionInLanguageSelector")]
            public bool DisplayRegionInLanguageSelector { get; set; }

            [LocalizedDisplay("*UseNativeNameInLanguageSelector")]
            public bool UseNativeNameInLanguageSelector { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.CompanyInformationSettings.")]
        public partial class CompanyInformationSettingsModel
        {
            [LocalizedDisplay("*CompanyName")]
            public string CompanyName { get; set; }

            [LocalizedDisplay("*Salutation")]
            public string Salutation { get; set; }

            [LocalizedDisplay("*Title")]
            public string Title { get; set; }

            [LocalizedDisplay("*Firstname")]
            public string Firstname { get; set; }

            [LocalizedDisplay("*Lastname")]
            public string Lastname { get; set; }

            [LocalizedDisplay("*CompanyManagementDescription")]
            public string CompanyManagementDescription { get; set; }

            [LocalizedDisplay("*CompanyManagement")]
            public string CompanyManagement { get; set; }

            [LocalizedDisplay("*Street")]
            public string Street { get; set; }

            [LocalizedDisplay("*Street2")]
            public string Street2 { get; set; }

            [LocalizedDisplay("*ZipCode")]
            public string ZipCode { get; set; }

            [LocalizedDisplay("*Location")]
            public string City { get; set; }

            [LocalizedDisplay("*Country")]
            public int? CountryId { get; set; }

            [LocalizedDisplay("*Country")]
            public string CountryName { get; set; }

            [LocalizedDisplay("*State")]
            public string Region { get; set; }

            [LocalizedDisplay("*VatId")]
            public string VatId { get; set; }

            [LocalizedDisplay("*CommercialRegister")]
            public string CommercialRegister { get; set; }

            [LocalizedDisplay("*TaxNumber")]
            public string TaxNumber { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.ContactDataSettings.")]
        public partial class ContactDataSettingsModel
        {
            [LocalizedDisplay("*CompanyTelephoneNumber")]
            public string CompanyTelephoneNumber { get; set; }

            [LocalizedDisplay("*HotlineTelephoneNumber")]
            public string HotlineTelephoneNumber { get; set; }

            [LocalizedDisplay("*MobileTelephoneNumber")]
            public string MobileTelephoneNumber { get; set; }

            [LocalizedDisplay("*CompanyFaxNumber")]
            public string CompanyFaxNumber { get; set; }

            [LocalizedDisplay("*CompanyEmailAddress")]
            public string CompanyEmailAddress { get; set; }

            [LocalizedDisplay("*WebmasterEmailAddress")]
            public string WebmasterEmailAddress { get; set; }

            [LocalizedDisplay("*SupportEmailAddress")]
            public string SupportEmailAddress { get; set; }

            [LocalizedDisplay("*ContactEmailAddress")]
            public string ContactEmailAddress { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.BankConnectionSettings.")]
        public partial class BankConnectionSettingsModel
        {
            [LocalizedDisplay("*Bankname")]
            public string Bankname { get; set; }

            [LocalizedDisplay("*Bankcode")]
            public string Bankcode { get; set; }

            [LocalizedDisplay("*AccountNumber")]
            public string AccountNumber { get; set; }

            [LocalizedDisplay("*AccountHolder")]
            public string AccountHolder { get; set; }

            [LocalizedDisplay("*Iban")]
            public string Iban { get; set; }

            [LocalizedDisplay("*Bic")]
            public string Bic { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.GeneralCommon.SocialSettings.")]
        public partial class SocialSettingsModel
        {
            [LocalizedDisplay("*FacebookAppId")]
            public string FacebookAppId { get; set; }

            [LocalizedDisplay("*TwitterSite")]
            public string TwitterSite { get; set; }

            [LocalizedDisplay("*ShowSocialLinksInFooter")]
            public bool ShowSocialLinksInFooter { get; set; }

            [LocalizedDisplay("*FacebookLink", "*LeaveEmpty")]
            public string FacebookLink { get; set; }

            [LocalizedDisplay("*TwitterLink", "*LeaveEmpty")]
            public string TwitterLink { get; set; }

            [LocalizedDisplay("*PinterestLink", "*LeaveEmpty")]
            public string PinterestLink { get; set; }

            [LocalizedDisplay("*YoutubeLink", "*LeaveEmpty")]
            public string YoutubeLink { get; set; }

            [LocalizedDisplay("*InstagramLink", "*LeaveEmpty")]
            public string InstagramLink { get; set; }

            [LocalizedDisplay("*FlickrLink", "*LeaveEmpty")]
            public string FlickrLink { get; set; }

            [LocalizedDisplay("*LinkedInLink", "*LeaveEmpty")]
            public string LinkedInLink { get; set; }

            [LocalizedDisplay("*XingLink", "*LeaveEmpty")]
            public string XingLink { get; set; }

            [LocalizedDisplay("*TikTokLink", "*LeaveEmpty")]
            public string TikTokLink { get; set; }

            [LocalizedDisplay("*SnapchatLink", "*LeaveEmpty")]
            public string SnapchatLink { get; set; }

            [LocalizedDisplay("*VimeoLink", "*LeaveEmpty")]
            public string VimeoLink { get; set; }

            [LocalizedDisplay("*TumblrLink", "*LeaveEmpty")]
            public string TumblrLink { get; set; }

            [LocalizedDisplay("*ElloLink", "*LeaveEmpty")]
            public string ElloLink { get; set; }

            [LocalizedDisplay("*BehanceLink", "*LeaveEmpty")]
            public string BehanceLink { get; set; }
        }

        #endregion
    }

    public partial class ContactDataSettingsValidator : SettingModelValidator<GeneralCommonSettingsModel.ContactDataSettingsModel, ContactDataSettings>
    {
        public ContactDataSettingsValidator()
        {
            RuleFor(x => x.CompanyEmailAddress).EmailAddress();
            RuleFor(x => x.ContactEmailAddress).EmailAddress();
            RuleFor(x => x.SupportEmailAddress).EmailAddress();
            RuleFor(x => x.WebmasterEmailAddress).EmailAddress();
        }
    }

    public partial class CaptchaSettingsValidator : SettingModelValidator<GeneralCommonSettingsModel.CaptchaSettingsModel, CaptchaSettings>
    {
        public CaptchaSettingsValidator(Localizer T)
        {
            RuleFor(x => x.ReCaptchaPublicKey)
                .NotEmpty()
                .When(x => x.Enabled)
                .WithMessage(T("Admin.Configuration.Settings.GeneralCommon.CaptchaEnabledNoKeys"));

            RuleFor(x => x.ReCaptchaPrivateKey)
                .NotEmpty()
                .When(x => x.Enabled)
                .WithMessage(T("Admin.Configuration.Settings.GeneralCommon.CaptchaEnabledNoKeys"));
        }
    }
}

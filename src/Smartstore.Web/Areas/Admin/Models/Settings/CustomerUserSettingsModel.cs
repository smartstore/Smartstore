using System.ComponentModel.DataAnnotations;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Identity;

namespace Smartstore.Admin.Models
{
    public partial class CustomerUserSettingsModel : ModelBase, ILocalizedModel<CustomerUserSettingsLocalizedModel>
    {
        public CustomerSettingsModel CustomerSettings { get; set; } = new();
        public AddressSettingsModel AddressSettings { get; set; } = new();
        public PrivacySettingsModel PrivacySettings { get; set; } = new();
        public List<CustomerUserSettingsLocalizedModel> Locales { get; set; } = new();

        #region Nested classes

        [LocalizedDisplay("Admin.Configuration.Settings.CustomerUser.")]
        public partial class CustomerSettingsModel
        {
            [LocalizedDisplay("*CustomerLoginType")]
            public CustomerLoginType CustomerLoginType { get; set; }

            [LocalizedDisplay("*CustomerNumberMethod")]
            public CustomerNumberMethod CustomerNumberMethod { get; set; }

            [LocalizedDisplay("*CustomerNumberVisibility")]
            public CustomerNumberVisibility CustomerNumberVisibility { get; set; }

            [LocalizedDisplay("*AllowUsersToChangeUsernames")]
            public bool AllowUsersToChangeUsernames { get; set; }

            [LocalizedDisplay("*CheckUsernameAvailabilityEnabled")]
            public bool CheckUsernameAvailabilityEnabled { get; set; }

            [LocalizedDisplay("*UserRegistrationType")]
            public UserRegistrationType UserRegistrationType { get; set; }

            [UIHint("CustomerRoles")]
            [AdditionalMetadata("includeSystemRoles", false)]
            [LocalizedDisplay("*RegisterCustomerRole")]
            public int? RegisterCustomerRoleId { get; set; }

            [LocalizedDisplay("*AllowCustomersToUploadAvatars")]
            public bool AllowCustomersToUploadAvatars { get; set; }

            [LocalizedDisplay("*MaxAvatarFileSize")]
            public long MaxAvatarFileSize { get; set; } = 10240;

            [LocalizedDisplay("*ShowCustomersLocation")]
            public bool ShowCustomersLocation { get; set; }

            [LocalizedDisplay("*ShowCustomersJoinDate")]
            public bool ShowCustomersJoinDate { get; set; }

            [LocalizedDisplay("*AllowViewingProfiles")]
            public bool AllowViewingProfiles { get; set; }

            [LocalizedDisplay("*NotifyNewCustomerRegistration")]
            public bool NotifyNewCustomerRegistration { get; set; }

            [LocalizedDisplay("*HideDownloadableProductsTab")]
            public bool HideDownloadableProductsTab { get; set; }

            [LocalizedDisplay("*HideBackInStockSubscriptionsTab")]
            public bool HideBackInStockSubscriptionsTab { get; set; }

            [LocalizedDisplay("*CustomerNameFormat")]
            public CustomerNameFormat CustomerNameFormat { get; set; }

            [LocalizedDisplay("*CustomerNameFormatMaxLength")]
            public int CustomerNameFormatMaxLength { get; set; }

            [LocalizedDisplay("*CustomerNameAllowedCharacters")]
            public string CustomerNameAllowedCharacters { get; set; }

            [LocalizedDisplay("*NewsletterEnabled")]
            public bool NewsletterEnabled { get; set; }

            [LocalizedDisplay("*HideNewsletterBlock")]
            public bool HideNewsletterBlock { get; set; }

            [LocalizedDisplay("*StoreLastVisitedPage")]
            public bool StoreLastVisitedPage { get; set; }

            [LocalizedDisplay("*StoreLastUserAgent")]
            public bool StoreLastUserAgent { get; set; }

            [LocalizedDisplay("*StoreLastDeviceFamily")]
            public bool StoreLastDeviceFamily { get; set; }

            [LocalizedDisplay("*GenderEnabled")]
            public bool GenderEnabled { get; set; }

            [LocalizedDisplay("*TitleEnabled")]
            public bool TitleEnabled { get; set; }

            [LocalizedDisplay("*FirstNameRequired")]
            public bool FirstNameRequired { get; set; }

            [LocalizedDisplay("*LastNameRequired")]
            public bool LastNameRequired { get; set; }

            [LocalizedDisplay("*DateOfBirthEnabled")]
            public bool DateOfBirthEnabled { get; set; }

            [LocalizedDisplay("*CompanyEnabled")]
            public bool CompanyEnabled { get; set; }

            [LocalizedDisplay("*CompanyRequired")]
            public bool CompanyRequired { get; set; }

            [LocalizedDisplay("*StreetAddressEnabled")]
            public bool StreetAddressEnabled { get; set; }

            [LocalizedDisplay("*StreetAddressRequired")]
            public bool StreetAddressRequired { get; set; }

            [LocalizedDisplay("*StreetAddress2Enabled")]
            public bool StreetAddress2Enabled { get; set; }

            [LocalizedDisplay("*StreetAddress2Required")]
            public bool StreetAddress2Required { get; set; }

            [LocalizedDisplay("*ZipPostalCodeEnabled")]
            public bool ZipPostalCodeEnabled { get; set; }

            [LocalizedDisplay("*ZipPostalCodeRequired")]
            public bool ZipPostalCodeRequired { get; set; }

            [LocalizedDisplay("*CityEnabled")]
            public bool CityEnabled { get; set; }

            [LocalizedDisplay("*CityRequired")]
            public bool CityRequired { get; set; }

            [LocalizedDisplay("*CountryEnabled")]
            public bool CountryEnabled { get; set; }

            [LocalizedDisplay("*StateProvinceEnabled")]
            public bool StateProvinceEnabled { get; set; }

            [LocalizedDisplay("*StateProvinceRequired")]
            public bool StateProvinceRequired { get; set; }

            [LocalizedDisplay("*PhoneEnabled")]
            public bool PhoneEnabled { get; set; }

            [LocalizedDisplay("*PhoneRequired")]
            public bool PhoneRequired { get; set; }

            [LocalizedDisplay("*FaxEnabled")]
            public bool FaxEnabled { get; set; }

            [LocalizedDisplay("*FaxRequired")]
            public bool FaxRequired { get; set; }

            #region Password

            [LocalizedDisplay("*DefaultPasswordFormat")]
            public PasswordFormat DefaultPasswordFormat { get; set; }

            [LocalizedDisplay("*PasswordMinLength")]
            public int PasswordMinLength { get; set; } = 6;

            [LocalizedDisplay("*PasswordRequireDigit")]
            public bool PasswordRequireDigit { get; set; }

            [LocalizedDisplay("*PasswordRequireUppercase")]
            public bool PasswordRequireUppercase { get; set; }

            [LocalizedDisplay("*PasswordRequireLowercase")]
            public bool PasswordRequireLowercase { get; set; }

            [LocalizedDisplay("*PasswordRequiredUniqueChars")]
            public int PasswordRequiredUniqueChars { get; set; }

            [LocalizedDisplay("*PasswordRequireNonAlphanumeric")]
            public bool PasswordRequireNonAlphanumeric { get; set; }

            #endregion
        }

        [LocalizedDisplay("Admin.Configuration.Settings.CustomerUser.")]
        public partial class AddressSettingsModel
        {
            [LocalizedDisplay("*SalutationEnabled")]
            public bool SalutationEnabled { get; set; }

            [LocalizedDisplay("*Salutations")]
            public string Salutations { get; set; }

            [LocalizedDisplay("*TitleEnabled")]
            public bool TitleEnabled { get; set; }

            [LocalizedDisplay("*CompanyEnabled")]
            public bool CompanyEnabled { get; set; }
            [LocalizedDisplay("*CompanyRequired")]
            public bool CompanyRequired { get; set; }

            [LocalizedDisplay("*StreetAddressEnabled")]
            public bool StreetAddressEnabled { get; set; }
            [LocalizedDisplay("*StreetAddressRequired")]
            public bool StreetAddressRequired { get; set; }

            [LocalizedDisplay("*StreetAddress2Enabled")]
            public bool StreetAddress2Enabled { get; set; }
            [LocalizedDisplay("*StreetAddress2Required")]
            public bool StreetAddress2Required { get; set; }

            [LocalizedDisplay("*ZipPostalCodeEnabled")]
            public bool ZipPostalCodeEnabled { get; set; }
            [LocalizedDisplay("*ZipPostalCodeRequired")]
            public bool ZipPostalCodeRequired { get; set; }

            [LocalizedDisplay("*CityEnabled")]
            public bool CityEnabled { get; set; }
            [LocalizedDisplay("*CityRequired")]
            public bool CityRequired { get; set; }

            [LocalizedDisplay("*CountryEnabled")]
            public bool CountryEnabled { get; set; }
            [LocalizedDisplay("*CountryRequired")]
            public bool CountryRequired { get; set; }

            [LocalizedDisplay("*StateProvinceEnabled")]
            public bool StateProvinceEnabled { get; set; }
            [LocalizedDisplay("*StateProvinceRequired")]
            public bool StateProvinceRequired { get; set; }

            [LocalizedDisplay("*PhoneEnabled")]
            public bool PhoneEnabled { get; set; }
            [LocalizedDisplay("*PhoneRequired")]
            public bool PhoneRequired { get; set; }

            [LocalizedDisplay("*FaxEnabled")]
            public bool FaxEnabled { get; set; }
            [LocalizedDisplay("*FaxRequired")]
            public bool FaxRequired { get; set; }

            [LocalizedDisplay("*ValidateEmailAddress")]
            public bool ValidateEmailAddress { get; set; }
        }

        [LocalizedDisplay("Admin.Configuration.Settings.CustomerUser.")]
        public partial class PrivacySettingsModel
        {
            [LocalizedDisplay("*Privacy.CookieConsentRequirement")]
            public CookieConsentRequirement CookieConsentRequirement { get; set; }

            [LocalizedDisplay("*Privacy.ModalCookieConsent")]
            public bool ModalCookieConsent { get; set; } = true;

            [LocalizedDisplay("*Privacy.SameSiteMode")]
            public SameSiteMode SameSiteMode { get; set; } = SameSiteMode.Lax;

            [LocalizedDisplay("*Privacy.VisitorCookieExpirationDays")]
            public int VisitorCookieExpirationDays { get; set; } = 365;

            [LocalizedDisplay("*Privacy.StoreLastIpAddress")]
            public bool StoreLastIpAddress { get; set; }

            [LocalizedDisplay("*Privacy.DisplayGdprConsentOnForms")]
            public bool DisplayGdprConsentOnForms { get; set; }

            [LocalizedDisplay("*Privacy.FullNameOnContactUsRequired")]
            public bool FullNameOnContactUsRequired { get; set; }

            [LocalizedDisplay("*Privacy.FullNameOnProductRequestRequired")]
            public bool FullNameOnProductRequestRequired { get; set; }

            public List<CookieInfo> CookieInfos { get; set; } = new();
        }

        #endregion
    }

    public class CustomerUserSettingsLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("Admin.Configuration.Settings.CustomerUser.Salutations")]
        public string Salutations { get; set; }
    }

    [LocalizedDisplay("Admin.Configuration.Settings.CustomerUser.Privacy.CookieInfo.")]
    public partial class CookieInfoModel : ILocalizedModel<CookieInfoLocalizedModel>
    {
        [Required]
        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [Required]
        [LocalizedDisplay("*Description")]
        public string Description { get; set; }

        [LocalizedDisplay("*CookieType")]
        public CookieType CookieType { get; set; }

        /// <summary>
        /// Used for display in grid
        /// </summary>
        [LocalizedDisplay("*CookieType")]
        public string CookieTypeName { get; set; }

        /// <summary>
        /// Used to mark which cookie info can be deleted from setting.
        /// </summary>
        [LocalizedDisplay("*IsPluginInfo")]
        public bool IsPluginInfo { get; set; }

        [UIHint("Stores")]
        [AdditionalMetadata("multiple", true)]
        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public int[] SelectedStoreIds { get; set; }

        [LocalizedDisplay("Admin.Common.Store.LimitedTo")]
        public bool LimitedToStores { get; set; }

        public List<CookieInfoLocalizedModel> Locales { get; set; } = new();
    }

    [LocalizedDisplay("Admin.Configuration.Settings.CustomerUser.Privacy.CookieInfo.")]
    public class CookieInfoLocalizedModel : ILocalizedLocaleModel
    {
        public int LanguageId { get; set; }

        [LocalizedDisplay("*Name")]
        public string Name { get; set; }

        [LocalizedDisplay("*Description")]
        public string Description { get; set; }
    }

    public partial class CustomerUserSettingsValidator : SettingModelValidator<CustomerUserSettingsModel, CustomerSettings>
    {
        public CustomerUserSettingsValidator()
        {
            RuleFor(x => x.CustomerSettings.MaxAvatarFileSize).GreaterThan(0);
            RuleFor(x => x.CustomerSettings.PasswordMinLength).GreaterThanOrEqualTo(4);
            RuleFor(x => x.CustomerSettings.PasswordRequiredUniqueChars).GreaterThanOrEqualTo(0);

            RuleFor(x => x.PrivacySettings.VisitorCookieExpirationDays).GreaterThan(0);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Smartstore.AmazonPay.Models
{
    [LocalizedDisplay("Plugins.Payments.AmazonPay.")]
    public class ConfigurationModel : ModelBase
    {
        [LocalizedDisplay("*UseSandbox")]
        public bool UseSandbox { get; set; }

        [LocalizedDisplay("*SellerId")]
        public string SellerId { get; set; }

        [LocalizedDisplay("*AccessKey")]
        public string AccessKey { get; set; }

        [LocalizedDisplay("*SecretKey")]
        [DataType(DataType.Password)]
        public string SecretKey { get; set; }

        [LocalizedDisplay("*ClientId")]
        public string ClientId { get; set; }

        [LocalizedDisplay("*Marketplace")]
        public string Marketplace { get; set; }

        [LocalizedDisplay("*DataFetching")]
        public AmazonPayDataFetchingType DataFetching { get; set; }

        [LocalizedDisplay("*IpnUrl")]
        public string IpnUrl { get; set; }

        [LocalizedDisplay("*PollingMaxOrderCreationDays")]
        public int PollingMaxOrderCreationDays { get; set; }

        [LocalizedDisplay("*TransactionType")]
        public AmazonPayTransactionType TransactionType { get; set; }

        [LocalizedDisplay("*AuthorizeMethod")]
        public AmazonPayAuthorizeMethod AuthorizeMethod { get; set; }

        [LocalizedDisplay("*SaveEmailAndPhone")]
        public AmazonPaySaveDataType? SaveEmailAndPhone { get; set; }

        [LocalizedDisplay("*ShowPayButtonForAdminOnly")]
        public bool ShowPayButtonForAdminOnly { get; set; }

        [LocalizedDisplay("*ShowButtonInMiniShoppingCart")]
        public bool ShowButtonInMiniShoppingCart { get; set; }

        [LocalizedDisplay("Admin.Configuration.Payment.Methods.AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [LocalizedDisplay("Admin.Configuration.Payment.Methods.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }

        [LocalizedDisplay("*AddOrderNotes")]
        public bool AddOrderNotes { get; set; }

        [LocalizedDisplay("*InformCustomerAboutErrors")]
        public bool InformCustomerAboutErrors { get; set; }

        [LocalizedDisplay("*InformCustomerAddErrors")]
        public bool InformCustomerAddErrors { get; set; }


        [LocalizedDisplay("*PayButtonColor")]
        public string PayButtonColor { get; set; }

        [LocalizedDisplay("*PayButtonSize")]
        public string PayButtonSize { get; set; }

        [LocalizedDisplay("*AuthButtonType")]
        public string AuthButtonType { get; set; }

        [LocalizedDisplay("*AuthButtonColor")]
        public string AuthButtonColor { get; set; }

        [LocalizedDisplay("*AuthButtonSize")]
        public string AuthButtonSize { get; set; }

        #region Registration data

        public string RegisterUrl { get; set; } = "https://payments-eu.amazon.com/register";
        public string SoftwareVersion { get; set; } = SmartstoreVersion.CurrentFullVersion;
        public string ModuleVersion { get; set; }
        public string LeadCode { get; set; }
        public string PlatformId { get; set; }
        public string PublicKey { get; set; }
        public string KeyShareUrl { get; set; }
        public string LanguageLocale { get; set; }

        /// <summary>
        /// Including all domains and sub domains where the login button appears. SSL mandatory.
        /// </summary>
        [LocalizedDisplay("*MerchantLoginDomains")]
        public HashSet<string> MerchantLoginDomains { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        [LocalizedDisplay("*MerchantLoginDomains")]
        public HashSet<string> CurrentMerchantLoginDomains { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Used to populate Allowed Return URLs on the Login with Amazon application. SSL mandatory. Max 512 characters.
        /// </summary>
        [LocalizedDisplay("*MerchantLoginRedirectUrls")]
        public HashSet<string> MerchantLoginRedirectUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        [LocalizedDisplay("*MerchantLoginRedirectUrls")]
        public HashSet<string> CurrentMerchantLoginRedirectUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        public string MerchantStoreDescription { get; set; }
        public string MerchantPrivacyNoticeUrl { get; set; }
        public string MerchantCountry { get; set; }
        public string MerchantSandboxIpnUrl { get; set; }
        public string MerchantProductionIpnUrl { get; set; }

        #endregion
    }
}

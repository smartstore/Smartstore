using Smartstore.AmazonPay.Domain;
using Smartstore.Core.Configuration;

namespace Smartstore.AmazonPay
{
    public class AmazonPaySettings : ISettings
    {
        public bool UseSandbox { get; set; }

        public string SellerId { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string ClientId { get; set; }
        public string Marketplace { get; set; } = "de";

        public AmazonPayDataFetchingType DataFetching { get; set; } = AmazonPayDataFetchingType.Ipn;
        public AmazonPayTransactionType TransactionType { get; set; } = AmazonPayTransactionType.Authorize;
        public AmazonPayAuthorizeMethod AuthorizeMethod { get; set; } = AmazonPayAuthorizeMethod.Omnichronous;

        public AmazonPaySaveDataType? SaveEmailAndPhone { get; set; } = AmazonPaySaveDataType.OnlyIfEmpty;
        public bool ShowPayButtonForAdminOnly { get; set; }
        public bool ShowButtonInMiniShoppingCart { get; set; }

        public int PollingMaxOrderCreationDays { get; set; } = 31;

        public decimal AdditionalFee { get; set; }
        public bool AdditionalFeePercentage { get; set; }

        public bool AddOrderNotes { get; set; } = true;

        public bool InformCustomerAboutErrors { get; set; } = true;
        public bool InformCustomerAddErrors { get; set; } = true;

        public string PayButtonColor { get; set; } = "Gold";
        public string PayButtonSize { get; set; } = "small";

        public string AuthButtonType { get; set; } = "LwA";
        public string AuthButtonColor { get; set; } = "Gold";
        public string AuthButtonSize { get; set; } = "medium";

        public bool CanSaveEmailAndPhone(string value)
            => SaveEmailAndPhone == AmazonPaySaveDataType.Always || (SaveEmailAndPhone == AmazonPaySaveDataType.OnlyIfEmpty && value.IsEmpty());
    }
}

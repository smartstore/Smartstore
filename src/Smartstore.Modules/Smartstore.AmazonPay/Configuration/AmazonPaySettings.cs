using Smartstore.Core.Configuration;

namespace Smartstore.AmazonPay
{
    public enum AmazonPayTransactionType
    {
        Authorize = 1,
        AuthorizeAndCapture = 2
    }

    public enum AmazonPaySaveDataType
    {
        None = 0,
        OnlyIfEmpty,
        Always
    }

    public class AmazonPaySettings : ISettings
    {
        public bool UseSandbox { get; set; }

        /// <summary>
        /// Public Key-ID to access API (Checkout v2).
        /// </summary>
        public string PublicKeyId { get; set; }

        /// <summary>
        /// Private key to access API (Checkout v2). Provided as a .pem file.
        /// </summary>
        public string PrivateKey { get; set; }

        public string SellerId { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }

        /// <summary>
        /// Also named "Store-ID" (Checkout v2). E.g. amzn1.application-oa2-client.1ab987....
        /// </summary>
        public string ClientId { get; set; }
        public string Marketplace { get; set; } = "de";

        public AmazonPayTransactionType TransactionType { get; set; } = AmazonPayTransactionType.Authorize;

        public AmazonPaySaveDataType? SaveEmailAndPhone { get; set; } = AmazonPaySaveDataType.OnlyIfEmpty;
        public bool ShowPayButtonForAdminOnly { get; set; }
        public bool ShowButtonInMiniShoppingCart { get; set; }
        public bool ShowSignoutButton { get; set; } = true;

        public decimal AdditionalFee { get; set; }
        public bool AdditionalFeePercentage { get; set; }

        public bool AddOrderNotes { get; set; } = true;

        public bool InformCustomerAboutErrors { get; set; } = true;
        public bool InformCustomerAddErrors { get; set; } = true;

        public string PayButtonColor { get; set; } = "Gold";

        public string AuthButtonType { get; set; } = "LwA";
        public string AuthButtonColor { get; set; } = "Gold";

        public bool CanSaveEmailAndPhone(string value)
            => SaveEmailAndPhone == AmazonPaySaveDataType.Always || (SaveEmailAndPhone == AmazonPaySaveDataType.OnlyIfEmpty && value.IsEmpty());

        public string GetCheckoutScriptUrl()
        {
            return Marketplace.EmptyNull().ToLower() switch
            {
                "us" => "https://static-na.payments-amazon.com/checkout.js",
                "jp" => "https://static-fe.payments-amazon.com/checkout.js",
                _ => "https://static-eu.payments-amazon.com/checkout.js",
            };
        }
    }
}

namespace Smartstore.AmazonPay.Models
{
    public class AmazonPayButtonModel : ModelBase
    {
        public AmazonPayButtonModel(AmazonPaySettings settings,
            string currencyCode,
            string languageSeoCode = null)
        {
            Guard.NotNull(settings, nameof(settings));
            Guard.NotEmpty(currencyCode, nameof(currencyCode));

            UseSandbox = settings.UseSandbox;
            PublicKeyId = settings.PublicKeyId;
            PrivateKey = settings.PrivateKey;
            StoreId = settings.ClientId;
            CurrencyCode = currencyCode;

            ButtonColor = settings.PayButtonColor;
            ButtonSize = settings.PayButtonSize;

            var marketplace = settings.Marketplace.EmptyNull().ToLower();

            CheckoutScriptUrl = marketplace switch
            {
                "us" => "https://static-na.payments-amazon.com/checkout.js",
                "jp" => "https://static-fe.payments-amazon.com/checkout.js",
                _ => "https://static-eu.payments-amazon.com/checkout.js",
            };

            CheckoutLanguage = marketplace switch
            {
                "us" => "en_US",
                "jp" => "ja_JP",
                _ => languageSeoCode.EmptyNull().ToLower() switch
                {
                    "en" => "en_GB",
                    "fr" => "fr_FR",
                    "it" => "it_IT",
                    "es" => "es_ES",
                    _ => "de_DE",
                },
            };
        }

        public bool UseSandbox { get; }
        public string PublicKeyId { get; }
        public string PrivateKey { get; }
        public string StoreId { get; }

        public string CheckoutScriptUrl { get; }

        public string CurrencyCode { get; }
        public string CheckoutLanguage { get; }

        public string ButtonPlacement { get; init; } = "Cart";
        public string ButtonType { get; init; } = "PayAndShip";
        public string ButtonColor { get; }
        public string ButtonSize { get; }   // TODO: (mg) (core) Obsolete. Button is responsive now.
    }
}

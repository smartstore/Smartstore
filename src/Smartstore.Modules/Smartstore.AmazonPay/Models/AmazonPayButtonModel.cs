namespace Smartstore.AmazonPay.Models
{
    public class AmazonPayButtonModel : ModelBase
    {
        public AmazonPayButtonModel(
            AmazonPaySettings settings,
            string buttonType = null,
            string currencyCode = null,
            string languageSeoCode = null)
        {
            Guard.NotNull(settings, nameof(settings));

            UseSandbox = settings.UseSandbox;
            PublicKeyId = settings.PublicKeyId;
            PrivateKey = settings.PrivateKey;
            SellerId = settings.SellerId;
            StoreId = settings.ClientId;
            
            CurrencyCode = currencyCode;
            ButtonType = buttonType;
            ButtonColor = buttonType.EqualsNoCase("SignIn") ? settings.AuthButtonColor : settings.PayButtonColor;

            Marketplace = settings.Marketplace.EmptyNull().ToLower();
            CheckoutScriptUrl = settings.GetCheckoutScriptUrl();

            CheckoutLanguage = Marketplace switch
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

        public string SellerId { get; }
        public string StoreId { get; }

        public string Marketplace { get; }
        public string CheckoutScriptUrl { get; }

        public string CurrencyCode { get; }
        public string CheckoutLanguage { get; }

        public string ButtonPlacement { get; init; } = "Cart";
        public string ButtonType { get; init; } = "PayAndShip";
        public string ButtonColor { get; }
    }
}

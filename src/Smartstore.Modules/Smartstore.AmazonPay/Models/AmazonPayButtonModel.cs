namespace Smartstore.AmazonPay.Models;

public class AmazonPayButtonModel : ModelBase
{
    public AmazonPayButtonModel(
        AmazonPaySettings settings,
        string buttonType,
        string ledgerCurrency,
        string languageSeoCode = null,
        bool isPaymentSelection = false)
    {
        Guard.NotNull(settings, nameof(settings));
        Guard.NotEmpty(buttonType, nameof(buttonType));
        Guard.NotEmpty(ledgerCurrency, nameof(ledgerCurrency));

        var signIn = buttonType.EqualsNoCase("SignIn");

        UseSandbox = settings.UseSandbox;
        PublicKeyId = settings.PublicKeyId;
        PrivateKey = settings.PrivateKey;
        SellerId = settings.SellerId;
        StoreId = settings.ClientId;

        LedgerCurrency = ledgerCurrency;
        ButtonType = buttonType;
        ButtonColor = signIn ? settings.AuthButtonColor : settings.PayButtonColor;
        ButtonPlacement = signIn ? "Other" : isPaymentSelection ? "Checkout" : "Cart";
        IsPaymentSelection = isPaymentSelection;

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

    /// <summary>
    /// Required. AmazonPay script fails if ledger currency is missing or not supported.
    /// </summary>
    public string LedgerCurrency { get; }
    public string CheckoutLanguage { get; }

    /// <summary>
    /// Supported values: Home, Product, Cart, Checkout, Other.
    /// </summary>
    public string ButtonPlacement { get; }

    /// <summary>
    /// Supported values: PayAndShip, PayOnly, SignIn.
    /// </summary>
    public string ButtonType { get; }

    /// <summary>
    /// Supported values: Gold, LightGray, DarkGray.
    /// </summary>
    public string ButtonColor { get; }

    /// <summary>
    /// Gets a value indicating whether the button is rendered on the checkout payment method page.
    /// </summary>
    public bool IsPaymentSelection { get; }

    /// <summary>
    /// Gets a value indicating whether Amazon Pay is the currently selected payment method.
    /// </summary>
    public bool IsSelectedMethod { get; init; }
}
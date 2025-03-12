using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Represents an order object.
    /// </summary>
    public class OrderMessage
    {
        /// <summary>
        /// The intent to either capture payment immediately or authorize a payment for an order after order creation. 
        /// Possible values are CAPTURE & AUTHORIZE
        /// </summary>
        [Required]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public Intent Intent;

        /// <summary>
        /// The instruction to process an order.
        /// </summary>
        public string ProcessingInstruction;
        
        /// <summary>
        /// An array of purchase units. Each purchase unit establishes a contract between a payer and the payee.
        /// </summary>
        [Required]
        public PurchaseUnit[] PurchaseUnits;

        /// <summary>
        /// Holds information about the payment method to be used.
        /// </summary>
        public PaymentSource PaymentSource;

        /// <summary>
        /// Holds information about the payer.
        /// </summary>
        public Payer Payer;

        /// <summary>
        /// Holds information about the payment method to be used.
        /// </summary>
        [JsonProperty("application_context")]
        public PayPalApplictionContext AppContext;
    }

    public class PurchaseUnit
    {
        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [Required]
        public AmountWithBreakdown Amount;

        /// <summary>
        /// The API caller-provided external ID. Used to reconcile client transactions with PayPal transactions. 
        /// Appears in transaction and settlement reports but is not visible to the payer.
        /// </summary>
        public string CustomId;

        /// <summary>
        /// The purchase description. 
        /// </summary>
        [MaxLength(127)]
        public string Description;

        /// <summary>
        /// The API caller-provided external invoice number for this order. Appears in both the payer's transaction history and the emails that the payer receives.
        /// </summary>
        public string InvoiceId;

        /// <summary>
        /// An array of items that the customer purchases from the merchant.
        /// </summary>
        public PurchaseUnitItem [] Items;

        /// <summary>
        /// The shipping details.
        /// </summary>
        public ShippingDetail Shipping;

        /// <summary>
        /// The API caller-provided external ID for the purchase unit. Required for multiple purchase units when you must update the order through `PATCH`. If you omit this value and the order contains only one purchase unit, PayPal sets this value to `default`.
        /// </summary>
        public string ReferenceId;

        // TODO: (mh) (core) Implement on demand
        // payee, payment_instruction, soft_descriptor
    }

    public class PurchaseUnitItem
    {
        /// <summary>       
        /// The item name or title.
        /// </summary>
        [MaxLength(127)]
        [Required]
        public string Name;

        /// <summary>
        /// The item quantity. Must be a whole number.
        /// </summary>
        [MaxLength(10)]
        [Required]
        public string Quantity;

        /// <summary>
        /// The item quantity. Must be a whole number.
        /// </summary>
        public string TaxRate;
        
        /// <summary>
        /// The item price or rate per unit. 
        /// </summary>
        [Required]
        public MoneyMessage UnitAmount;

        /// <summary>
        /// The item category type (possible values are DIGITAL_GOODS, PHYSICAL_GOODS & DONATION). 
        /// </summary>
        public ItemCategoryType Category;

        /// <summary>
        /// The detailed item description.
        /// </summary>
        [MaxLength(127)]
        public string Description;

        /// <summary>
        /// The stock keeping unit (SKU) for the item.
        /// </summary>
        [MaxLength(127)]
        public string Sku;

        /// <summary>
        /// The item price or rate per unit. 
        /// </summary>
        public MoneyMessage Tax;
    }

    public class PaymentSource
    {
        [JsonProperty("paypal")]
        public PaymentSourceWallet PaymentSourceWallet;

        [JsonProperty("pay_upon_invoice")]
        public PaymentSourceInvoice PaymentSourceInvoice;

        [JsonProperty("trustly")]
        public PaymentSourceApm PaymentSourceTrustly;

        [JsonProperty("bancontact")]
        public PaymentSourceApm PaymentSourceBancontact;

        [JsonProperty("blik")]
        public PaymentSourceApm PaymentSourceBlik;

        [JsonProperty("eps")]
        public PaymentSourceApm PaymentSourceEps;

        [JsonProperty("ideal")]
        public PaymentSourceApm PaymentSourceIdeal;

        [JsonProperty("mybank")]
        public PaymentSourceApm PaymentSourceMyBank;

        [JsonProperty("p24")]
        public PaymentSourceApm PaymentSourceP24;

        [JsonProperty("google_pay")]
        public PaymentSourceGooglePay PaymentSourceGooglePay;
    }

    public class ShippingDetail
    {
        /// <summary>
        /// The API caller-provided external invoice number for this order. Appears in both the payer's transaction history and the emails that the payer receives.
        /// </summary>
        [JsonProperty("name")]
        public ShippingName ShippingName;

        [JsonProperty("address")]
        public ShippingAddress ShippingAddress;
    }

    public class ShippingName
    {
        /// <summary>
        /// When the party is a person, the party's full name.
        /// </summary>
        public string FullName;
    }

    public class ShippingAddress
    {
        /// <summary>
        /// The two-character ISO 3166-1 code that identifies the country or region.
        /// </summary>
        public string CountryCode;

        /// <summary>
        /// The first line of the address. For example, number or street. For example, 173 Drury Lane. 
        /// Required for data entry and compliance and risk checks. Must contain the full address.
        /// </summary>
        [MaxLength(300)]        
        [JsonProperty("address_line_1")]
        public string AddressLine1;

        /// <summary>
        /// The second line of the address. For example, suite or apartment number.
        /// </summary>
        [MaxLength(300)]
        [JsonProperty("address_line_2")]
        public string AddressLine2;

        /// <summary>
        /// The highest level sub-division in a country, which is usually a province, state, or ISO-3166-2 subdivision. 
        /// Format for postal delivery. For example, CA and not California. Value, by country, is:
        /// UK.A county.
        /// US.A state.
        /// Canada.A province.
        /// Japan.A prefecture.
        /// Switzerland.A kanton.
        /// </summary>
        [MaxLength(300)]
        [JsonProperty("admin_area_1")]
        public string AdminArea1;

        /// <summary>
        /// A city, town, or village. Smaller than admin_area_level_1.
        /// </summary>
        [MaxLength(300)]
        [JsonProperty("admin_area_2")]
        public string AdminArea2;

        /// <summary>
        /// The postal code, which is the zip code or equivalent. 
        /// Typically required for countries with a postal code or an equivalent. 
        /// </summary>
        [MaxLength(300)]
        public string PostalCode;
    }

    public class PaymentSourceWallet
    {
        public string ReturnUrl;
        public string CancelUrl;
    }

    public class PaymentSourceInvoice
    {
        public NameMessage Name;

        public string Email;

        /// <summary>
        /// Format: YYYY-mm-dd e.g. 1990-01-01.
        /// </summary>
        public string BirthDate;

        public PhoneMessage Phone;
        public AddressMessage BillingAddress;
        public ExperienceContext ExperienceContext;
    }

    public class PaymentSourceApm
    {
        public string Name;
        public string CountryCode;
        public string Email;
        [JsonProperty("bic")] // INFO: Snake case will transform it to b_i_c otherwise
        public string BIC;
    }

    public class PaymentSourceGooglePay
    {
        public PayPalAttributes Attributes;
    }

    public class PayPalAttributes
    {
        public VerificationAttribute Verification;
    }

    public class VerificationAttribute
    {
        public string Method;
    }

    public class Payer
    {
        public NameMessage Name;
        public string EmailAddress;
        public string PayerId;
    }

    public class NameMessage
    {
        /// <summary>
        /// Fistname
        /// </summary>
        public string GivenName;

        public string Surname;
    }

    public class PhoneMessage
    {
        public string NationalNumber;
        public string CountryCode;
    }

    public class AddressMessage
    {
        /// <summary>
        /// Street inclusive house number
        /// </summary>
        [JsonProperty("address_line_1")]
        public string AddressLine1;

        /// <summary>
        /// City
        /// </summary>
        [JsonProperty("admin_area_2")]
        public string AdminArea2;

        public string PostalCode;

        /// <summary>
        /// Two character country code e.g DE
        /// </summary>
        public string CountryCode;
    }

    public class ExperienceContext
    {
        /// <summary>
        /// Region info e.g. de-DE
        /// </summary>
        public string Locale;
        public string BrandName;
        public string LogoUrl;
        public string[] CustomerServiceInstructions;
    }

    public class PayPalApplictionContext
    {
        /// <summary>
        /// The location from which the shipping address is derived.
        /// </summary>
        public ShippingPreference ShippingPreference;

        /// <summary>
        /// Configures the label name to Continue or Subscribe Now for subscription consent experience.
        /// </summary>
        public UserAction UserAction { get; set; }

        /// <summary>
        /// Region info e.g. de-DE
        /// </summary>
        [MaxLength(10)]
        [MinLength(2)]
        public string Locale;

        /// <summary>
        /// Specifies the URL to which the customer's browser is returned after payment was made.
        /// </summary>
        [MaxLength(4000)]
        [MinLength(10)]
        public string ReturnUrl;

        /// <summary>
        /// Specifies the URL to which the customer's browser is returned if payment was cancelled.
        /// </summary>
        [MaxLength(4000)]
        [MinLength(10)]
        public string CancelUrl;
    }

    /// <summary>
    /// Configures the label name to Continue or Subscribe Now for subscription consent experience.
    /// </summary>
    public enum UserAction
    {
        /// <summary>
        /// After you redirect the customer to the PayPal subscription consent page, a Continue button appears. 
        /// Use this option when you want to control the activation of the subscription and do not want PayPal to activate the subscription.
        /// </summary>
        [EnumMember(Value = "CONTINUE")]
        Continue,
        /// <summary>
        /// After you redirect the customer to the PayPal subscription consent page, a Subscribe Now button appears. 
        /// Use this option when you want PayPal to activate the subscription.
        /// </summary>
        [EnumMember(Value = "SUBSCRIBE_NOW")]
        SubscribeNow
    }

    public enum Intent
    {
        [EnumMember(Value = "CAPTURE")]
        Capture,

        [EnumMember(Value = "AUTHORIZE")]
        Authorize
    }

    public enum ItemCategoryType
    {
        [EnumMember(Value = "DIGITAL_GOODS")]
        DigitalGoods,

        [EnumMember(Value = "PHYSICAL_GOODS")]
        PhysicalGoods,

        [EnumMember(Value = "DONATION")]
        Donation
    }

    public enum ShippingPreference
    {
        /// <summary>
        /// Get the merchant-provided address. The customer cannot change this address on the PayPal site. 
        /// If merchant does not pass an address, customer can choose the address on PayPal pages.
        /// </summary>
        [EnumMember(Value = "SET_PROVIDED_ADDRESS")]
        SetProvidedAddress,

        /// <summary>
        /// Redacts the shipping address from the PayPal site. Recommended for digital goods.
        /// </summary>
        [EnumMember(Value = "NO_SHIPPING")]
        NoShipping,

        /// <summary>
        /// Get the customer-provided shipping address on the PayPal site.
        /// </summary>
        [EnumMember(Value = "GET_FROM_FILE")]
        GetFromFile
    }
}
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
        [JsonProperty("intent")]
        public Intent Intent;

        /// <summary>
        /// The instruction to process an order.
        /// </summary>
        [JsonProperty("processing_instruction", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ProcessingInstruction;
        
        /// <summary>
        /// An array of purchase units. Each purchase unit establishes a contract between a payer and the payee.
        /// </summary>
        [Required]
        [JsonProperty("purchase_units", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PurchaseUnit[] PurchaseUnits;

        /// <summary>
        /// Holds information about the payment method to be used.
        /// </summary>
        [JsonProperty("payment_source", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PaymentSource PaymentSource;

        /// <summary>
        /// Holds information about the payment method to be used.
        /// </summary>
        [JsonProperty("application_context", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PayPalApplictionContext AppContext;
    }

    public class PurchaseUnit
    {
        /// <summary>
        /// The currency and amount for a financial transaction, such as a balance or payment due.
        /// </summary>
        [Required]
        [JsonProperty("amount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public AmountWithBreakdown Amount;

        /// <summary>
        /// The API caller-provided external ID. Used to reconcile client transactions with PayPal transactions. 
        /// Appears in transaction and settlement reports but is not visible to the payer.
        /// </summary>
        [JsonProperty("custom_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CustomId;

        /// <summary>
        /// The purchase description. 
        /// </summary>
        [MaxLength(127)]
        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Description;

        /// <summary>
        /// The API caller-provided external invoice number for this order. Appears in both the payer's transaction history and the emails that the payer receives.
        /// </summary>
        [JsonProperty("invoice_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string InvoiceId;

        /// <summary>
        /// An array of items that the customer purchases from the merchant.
        /// </summary>
        [JsonProperty("items", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PurchaseUnitItem [] Items;

        /// <summary>
        /// The shipping details.
        /// </summary>
        [JsonProperty("shipping", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ShippingDetail Shipping;

        /// <summary>
        /// The API caller-provided external ID for the purchase unit. Required for multiple purchase units when you must update the order through `PATCH`. If you omit this value and the order contains only one purchase unit, PayPal sets this value to `default`.
        /// </summary>
        [JsonProperty("reference_id", DefaultValueHandling = DefaultValueHandling.Ignore)]
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
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name;

        /// <summary>
        /// The item quantity. Must be a whole number.
        /// </summary>
        [MaxLength(10)]
        [Required]
        [JsonProperty("quantity", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Quantity;

        /// <summary>
        /// The item quantity. Must be a whole number.
        /// </summary>
        [JsonProperty("tax_rate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TaxRate;
        
        /// <summary>
        /// The item price or rate per unit. 
        /// </summary>
        [Required]
        [JsonProperty("unit_amount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public MoneyMessage UnitAmount;

        /// <summary>
        /// The item category type (possible values are DIGITAL_GOODS, PHYSICAL_GOODS & DONATION). 
        /// </summary>
        [JsonProperty("category")]
        public ItemCategoryType Category;

        /// <summary>
        /// The detailed item description.
        /// </summary>
        [MaxLength(127)]
        [JsonProperty("description", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Description;

        /// <summary>
        /// The stock keeping unit (SKU) for the item.
        /// </summary>
        [MaxLength(127)]
        [JsonProperty("sku", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Sku;

        /// <summary>
        /// The item price or rate per unit. 
        /// </summary>
        [JsonProperty("tax", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public MoneyMessage Tax;
    }

    public class PaymentSource
    {
        [JsonProperty("pay_upon_invoice", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PaymentSourceInvoice PaymentSourceInvoice;

        [JsonProperty("giropay", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PaymentSourceApm PaymentSourceGiroPay;

        [JsonProperty("bancontact", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PaymentSourceApm PaymentSourceBancontact;

        [JsonProperty("blik", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PaymentSourceApm PaymentSourceBlik;

        [JsonProperty("eps", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PaymentSourceApm PaymentSourceEps;

        [JsonProperty("ideal", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PaymentSourceApm PaymentSourceIdeal;

        [JsonProperty("mybank", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PaymentSourceApm PaymentSourceMyBank;

        [JsonProperty("p24", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PaymentSourceApm PaymentSourceP24;
    }

    public class ShippingDetail
    {
        /// <summary>
        /// The API caller-provided external invoice number for this order. Appears in both the payer's transaction history and the emails that the payer receives.
        /// </summary>
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ShippingName ShippingName;

        [JsonProperty("address", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ShippingAddress ShippingAddress;

    }

    public class ShippingName
    {
        /// <summary>
        /// When the party is a person, the party's full name.
        /// </summary>
        [JsonProperty("full_name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string FullName;
    }

    public class ShippingAddress
    {
        /// <summary>
        /// The two-character ISO 3166-1 code that identifies the country or region.
        /// </summary>
        [JsonProperty("country_code", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CountryCode;

        /// <summary>
        /// The first line of the address. For example, number or street. For example, 173 Drury Lane. 
        /// Required for data entry and compliance and risk checks. Must contain the full address.
        /// </summary>
        [MaxLength(300)]
        [JsonProperty("address_line_1", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AddressLine1;

        /// <summary>
        /// The second line of the address. For example, suite or apartment number.
        /// </summary>
        [MaxLength(300)]
        [JsonProperty("address_line_2", DefaultValueHandling = DefaultValueHandling.Ignore)]
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
        [JsonProperty("admin_area_1", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AdminArea1;

        /// <summary>
        /// A city, town, or village. Smaller than admin_area_level_1.
        /// </summary>
        [MaxLength(300)]
        [JsonProperty("admin_area_2", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AdminArea2;

        /// <summary>
        /// The postal code, which is the zip code or equivalent. 
        /// Typically required for countries with a postal code or an equivalent. 
        /// </summary>
        [MaxLength(300)]
        [JsonProperty("postal_code", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PostalCode;
    }

    public class PaymentSourceInvoice
    {
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public NameMessage Name;

        [JsonProperty("email", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Email;

        /// <summary>
        /// Format: YYYY-mm-dd e.g. 1990-01-01.
        /// </summary>
        [JsonProperty("birth_date", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BirthDate;

        [JsonProperty("phone", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PhoneMessage Phone;

        [JsonProperty("billing_address", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public BillingAddressMessage BillingAddress;

        [JsonProperty("experience_context", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ExperienceContext ExperienceContext;
    }

    public class PaymentSourceApm
    {
        [JsonProperty("name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Name;

        [JsonProperty("country_code", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CountryCode;

        [JsonProperty("email", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Email;

        [JsonProperty("bic", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BIC;
    }

    public class NameMessage
    {
        /// <summary>
        /// Fistname
        /// </summary>
        [JsonProperty("given_name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string GivenName;

        [JsonProperty("surname", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string SurName;
    }

    public class PhoneMessage
    {
        [JsonProperty("national_number", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string NationalNumber;

        [JsonProperty("country_code", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CountryCode;
    }

    public class BillingAddressMessage
    {
        /// <summary>
        /// Street inclusive house number
        /// </summary>
        [JsonProperty("address_line_1", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AddressLine1;

        /// <summary>
        /// City
        /// </summary>
        [JsonProperty("admin_area_2", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string AdminArea2;

        [JsonProperty("postal_code", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string PostalCode;

        /// <summary>
        /// Two character country code e.g DE
        /// </summary>
        [JsonProperty("country_code", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CountryCode;
    }

    public class ExperienceContext
    {
        /// <summary>
        /// Region info e.g. de-DE
        /// </summary>
        [JsonProperty("locale", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Locale;

        [JsonProperty("brand_name", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string BrandName;

        [JsonProperty("logo_url", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string LogoUrl;

        [JsonProperty("customer_service_instructions", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] CustomerServiceInstructions;
    }

    public class PayPalApplictionContext
    {
        [JsonProperty("shipping_preference")]
        public ShippingPreference ShippingPreference;

        /// <summary>
        /// Region info e.g. de-DE
        /// </summary>
        [JsonProperty("locale", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Locale;

        /// <summary>
        /// Specifies the URL to which the customer's browser is returned after payment was made.
        /// </summary>
        [JsonProperty("return_url", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ReturnUrl;

        /// <summary>
        /// Specifies the URL to which the customer's browser is returned if payment was cancelled.
        /// </summary>
        [JsonProperty("cancel_url", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CancelUrl;
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
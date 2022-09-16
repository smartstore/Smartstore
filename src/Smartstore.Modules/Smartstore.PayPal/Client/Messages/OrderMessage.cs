using System;
using System.ComponentModel.DataAnnotations;

namespace Smartstore.PayPal.Client.Messages
{
    /// <summary>
    /// Represents an order object.
    /// </summary>
    public class OrderMessage
    {
        // TODO: (mh) (core) Make it an enum
        /// <summary>
        /// The intent to either capture payment immediately or authorize a payment for an order after order creation. 
        /// Possible values are CAPTURE & AUTHORIZE
        /// </summary>
        [Required]
        [JsonProperty("intent", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Intent;

        /// <summary>
        /// TODO: (mh) (core) Docs
        /// </summary>
        [JsonProperty("processing_instruction", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ProcessingInstruction;
        
        /// <summary>
        /// An array of purchase units. Each purchase unit establishes a contract between a payer and the payee.
        /// </summary>
        [Required]
        [JsonProperty("purchase_units", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PurchaseUnit[] PurchaseUnits;

        // TODO: (mh) (core) This must be an interface as Payment sources may differ
        /// <summary>
        /// Holds information about the payment method to be used.
        /// </summary>
        [JsonProperty("payment_source", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public PaymentSource PaymentSource;
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
        /// The API caller-provided external invoice number for this order. Appears in both the payer's transaction history and the emails that the payer receives.
        /// </summary>
        [JsonProperty("shipping", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ShippingDetail Shipping;

        // TODO: (mh) (core) Implement on demand
        // payee, payment_instruction, reference_id, soft_descriptor
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

        // TODO: (mh) (core) Make enum
        /// <summary>
        /// The item category type (possible values are DIGITAL_GOODS, PHYSICAL_GOODS & DONATION). 
        /// </summary>
        [JsonProperty("category", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Category;

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
}
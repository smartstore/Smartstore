using System.Runtime.Serialization;
using Newtonsoft.Json.Converters;

namespace Smartstore.PayPal.Models
{
    /// <summary>
    /// Represents a Google Pay transaction info object.
    /// https://developers.google.com/pay/api/web/reference/request-objects#TransactionInfo
    /// </summary>
    public class GoogleTransactionInfo
    {
        /// <summary>
        /// The ISO 4217 alphabetic currency code.
        /// Warning: Make sure that currencyCode matches the currency that you authorize and charge to the user. 
        /// If currencyCode doesn't match the transaction currency, the transaction might be declined. 
        /// Not providing the correct currency might influence the ECI value and the liable party.
        /// </summary>
        [JsonProperty("currencyCode", Required = Required.Always)]
        public string CurrencyCode { get; set; }

        /// <summary>
        /// The ISO 3166-1 alpha-2 country code where the transaction is processed.
        /// This property is required for merchants who process transactions in European Economic Area (EEA) countries and any other countries that are subject to Strong Customer Authentication (SCA).
        /// Merchants must specify the acquirer bank country code.
        /// Note: When you support Brazilian combo cards, the countryCode must be BR.
        /// </summary>
        [JsonProperty("countryCode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CountryCode { get; set; }

        /// <summary>
        /// A unique ID that identifies a facilitation attempt.
        /// Merchants can use an existing ID or generate a specific one for Google Pay facilitation attempts.
        /// Note: Optional, but highly encouraged for troubleshooting.
        /// </summary>
        [JsonProperty("transactionId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TransactionId { get; set; }

        /// <summary>
        /// The status of the total price used:
        /// ESTIMATED: Total price might adjust based on the details of the response, such as sales tax collected that's based on a billing address.
        /// FINAL: Total price doesn't change from the amount presented to the shopper.
        /// </summary>
        [JsonProperty("totalPriceStatus", Required = Required.Always)]
        public string TotalPriceStatus { get; set; }

        /// <summary>
        /// Total monetary value of the transaction with an optional decimal precision of two decimal places.
        /// Note: totalPrice is shown in payment sheet to users, for transaction processed in an EEA country or any other country that's subject to SCA.
        /// The format of the string should follow the regex format: ^[0-9]+(\.[0-9][0-9])?$
        /// Warning: Make sure that totalPrice matches the amount that you authorize and charge to the user.
        /// If totalPrice doesn't match the transaction amount, the transaction might be declined.
        /// Not providing the correct price might influence the ECI value and the liable party.
        /// </summary>
        [JsonProperty("totalPrice", Required = Required.Always)]
        public string TotalPrice { get; set; }

        /// <summary>
        /// A list of cart items shown in the payment sheet (e.g. subtotals, sales taxes, shipping charges, discounts etc.).
        /// This is typically populated in the payment sheet if you use Authorize Payments or Dynamic Price Updates.
        /// This field is required if you implement support for Authorize Payments or Dynamic Price Updates.
        /// </summary>
        [JsonProperty("displayItems", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DisplayItem[] DisplayItems { get; set; } = [];

        /// <summary>
        /// Custom label for the total price within the display items.
        /// This field is required if displayItems are defined.
        /// </summary>
        [JsonProperty("totalPriceLabel", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string TotalPriceLabel { get; set; }

        /// <summary>
        /// Affects the submit button text displayed in the Google Pay payment sheet.
        /// DEFAULT: Standard text applies for the given totalPriceStatus (default).
        /// COMPLETE_IMMEDIATE_PURCHASE: The selected payment method is charged immediately after the payer confirms their selections.
        /// This option is only available when totalPriceStatus is set to FINAL.
        /// </summary>
        [JsonProperty("checkoutOption", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string CheckoutOption { get; set; }
    }

    public class DisplayItem
    {
        /// <summary>
        /// The label to be displayed for the given option.
        /// </summary>
        [JsonProperty("label", Required = Required.Always)]
        public string Label { get; set; }

        /// <summary>
        /// Type of displayed line item:
        /// LINE_ITEM
        /// SUBTOTAL
        /// </summary>
        [JsonProperty("type", Required = Required.Always)]
        public GooglePayItemType Type { get; set; }

        /// <summary>
        /// The monetary value of the cart item with an optional decimal precision of two decimal places.
        /// Negative values are allowed.
        /// </summary>
        [JsonProperty("price", Required = Required.Always)]
        public string Price { get; set; }

        /// <summary>
        /// The following variables define price variance:
        /// FINAL
        /// PENDING
        /// Default to FINAL if not provided.
        /// </summary>
        [JsonProperty("status", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(StringEnumConverter))]
        public GooglePayItemStatus Status { get; set; }
    }

    public enum GooglePayItemStatus
    {
        [EnumMember(Value = "FINAL")]
        Final,

        [EnumMember(Value = "PENDING")]
        Pending
    }

    public enum GooglePayItemType
    {
        [EnumMember(Value = "LINE_ITEM")]
        LineItem,

        [EnumMember(Value = "TAX")]
        Tax,

        [EnumMember(Value = "SUBTOTAL")]
        Subtotal
    }
}

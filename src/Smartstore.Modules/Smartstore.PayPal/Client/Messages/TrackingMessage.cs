using System.ComponentModel.DataAnnotations;

namespace Smartstore.PayPal.Client.Messages
{
    public class TrackingMessage
    {
        /// <summary>
        /// REQUIRED.
        /// string[1..64] characters. The tracking number for the shipment.This property supports Unicode.
        /// </summary>
        [MaxLength(64)]
        [Required]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string TrackingNumber;

        /// <summary>
        /// string[1..64] characters
        /// The name of the carrier for the shipment.Provide this value only if the carrier parameter is OTHER.This property supports Unicode.
        /// </summary>
        [MaxLength(64)]
        [Required]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string CarrierNameOther;

        /// <summary>
        /// REQUIRED.
        /// string[1..64] characters ^[0-9A-Z_]+$
        /// The carrier for the shipment.
        /// </summary>
        [MaxLength(64)]
        [Required]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string Carrier;

        /// <summary>
        /// REQUIRED.
        /// string [ 1 .. 50 ] characters
        /// The PayPal capture ID.
        /// </summary>
        [MaxLength(64)]
        [Required]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public string CaptureId;

        /// <summary>
        /// boolean > Default: false
        /// If true, sends an email notification to the payer of the PayPal transaction.The email contains the tracking information that was uploaded through the API.
        /// </summary>
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public bool NotifyPayer;

        /// <summary>
        /// An array of details of items in the shipment.
        /// </summary>
        public ShipmentItem[] Items;
    }

    public class ShipmentItem
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
        /// The stock keeping unit (SKU) for the item.
        /// </summary>
        [MaxLength(127)]
        public string Sku;
    }
}
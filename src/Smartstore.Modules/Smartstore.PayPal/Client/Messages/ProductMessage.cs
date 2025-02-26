using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Smartstore.PayPal.Client.Messages
{
    public class ProductMessage
    {
        /// <summary>
        /// The ID of the product.
        /// </summary>
        [MaxLength(50)]
        [MinLength(6)]
        public string Id;

        /// <summary>
        /// The product name.
        /// </summary>
        [MaxLength(127)]
        [Required]
        public string Name;

        /// <summary>
        /// The product description.
        /// </summary>
        [MaxLength(256)]
        public string Description;

        /// <summary>
        /// The product type. Indicates whether the product is physical or digital goods, or a service.
        /// </summary>
        [Required]
        public PayPalProductType Type = PayPalProductType.Physical;

        /// <summary>
        /// The image URL for the product.
        /// </summary>
        [MaxLength(2000)]
        public string ImageUrl;

        /// <summary>
        /// The home page URL for the product.
        /// </summary>
        [MaxLength(2000)]
        public string HomeUrl;
    }

    public enum PayPalProductType
    {
        /// <summary>
        /// Physical goods
        /// </summary>
        [EnumMember(Value = "PHYSICAL")]
        Physical,

        /// <summary>
        /// For digital goods, the value must be set to DIGITAL to get the best rates. For more details, please contact your account manager.
        /// </summary>
        [EnumMember(Value = "DIGITAL")]
        Digital,

        /// <summary>
        /// Product representing a service. Example: Tech Support
        /// </summary>
        [EnumMember(Value = "SERVICE")]
        Service
    }
}
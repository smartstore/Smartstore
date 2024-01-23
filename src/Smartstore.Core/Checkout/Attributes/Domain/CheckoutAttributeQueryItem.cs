using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Smartstore.Core.Content.Media;

namespace Smartstore.Core.Checkout.Attributes
{
    /// <summary>
    /// Represents a checkout attribute query item.
    /// </summary>
    [DataContract]
    public class CheckoutAttributeQueryItem
    {
        /// <summary>
        /// Creates a key used for form names.
        /// </summary>
        /// <param name="attributeId">Checkout attribute identifier.</param>
        /// <returns>Key.</returns>
        public static string CreateKey(int attributeId)
            => $"cattr{attributeId}";

        /// <summary>
        /// The <see cref="CheckoutAttribute"/> identifier.
        /// </summary>
        [Required]
        [JsonProperty("attributeId")]
        [DataMember(Name = "attributeId")]
        public int AttributeId { get; set; }

        /// <summary>
        /// The checkout attribute value.
        /// For list type attributes like a dropdown list, this is the <see cref="CheckoutAttributeValue"/> identifier.
        /// If multiple identifiers must be specified (e.g. for checkboxes), they can be separated by commas.
        /// For a file, this must be a <see cref="Download.DownloadGuid"/>.
        /// </summary>
        /// <example>1234</example>
        [JsonProperty("value")]
        [DataMember(Name = "value")]
        public string Value { get; set; }

        /// <summary>
        /// The date if the control type is a datepicker. The value property is ignored in this case.
        /// </summary>
        [JsonProperty("date")]
        [DataMember(Name = "date")]
        public DateTime? Date { get; set; }

        /// <summary>
        /// Gets or sets a Value indicating whether the attribute is a file.
        /// </summary>
        public bool IsFile { get; set; }

        public bool IsText { get; set; }
        public bool IsTextArea { get; set; }

        public override string ToString()
        {
            var key = CreateKey(AttributeId);

            if (Date.HasValue)
            {
                return key + "-date";
            }
            else if (IsFile)
            {
                return key + "-file";
            }
            else if (IsText)
            {
                return key + "-text";
            }
            else if (IsTextArea)
            {
                return key + "-textarea";
            }

            return key;
        }
    }
}

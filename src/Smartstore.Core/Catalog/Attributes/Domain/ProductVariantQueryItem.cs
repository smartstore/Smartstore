using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;

namespace Smartstore.Core.Catalog.Attributes
{
    /// <summary>
    /// Represents a product variant query item.
    /// </summary>
    [DataContract]
    public class ProductVariantQueryItem
    {
        /// <summary>
        /// Creates a key used for form names.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="bundleItemId">Bundle item identifier. 0 if not a bundle item.</param>
        /// <param name="attributeId">Product attribute identifier.</param>
        /// <param name="variantAttributeId">Product variant attribute identifier.</param>
        /// <returns>Key.</returns>
        public static string CreateKey(int productId, int bundleItemId, int attributeId, int variantAttributeId)
            => $"pvari{productId}-{bundleItemId}-{attributeId}-{variantAttributeId}";

        /// <summary>
        /// The <see cref="Product"/> identifier.
        /// </summary>
        [Required]
        [JsonProperty("productId")]
        [DataMember(Name = "productId")]
        public int ProductId { get; set; }

        /// <summary>
        /// The <see cref="ProductBundleItem"/> identifier. 0 if this item is not a bundle item.
        /// </summary>
        [Required]
        [JsonProperty("bundleItemId")]
        [DataMember(Name = "bundleItemId")]
        public int BundleItemId { get; set; }

        /// <summary>
        /// The <see cref="ProductAttribute"/> identifier.
        /// </summary>
        [Required]
        [JsonProperty("attributeId")]
        [DataMember(Name = "attributeId")]
        public int AttributeId { get; set; }

        /// <summary>
        /// The <see cref="ProductVariantAttribute"/> identifier.
        /// It is the identifier of the mapping between a product and a product attribute.
        /// </summary>
        [Required]
        [JsonProperty("variantAttributeId")]
        [DataMember(Name = "variantAttributeId")]
        public int VariantAttributeId { get; set; }

        /// <summary>
        /// The variant value.
        /// For list type attributes like a dropdown list, this is the <see cref="ProductVariantAttributeValue"/> identifier.
        /// If multiple identifiers must be specified (e.g. for checkboxes), they can be separated by commas.
        /// For a file, this must be a <see cref="Download.DownloadGuid"/>.
        /// </summary>
        /// <example>1234</example>
        [JsonProperty("value")]
        [DataMember(Name = "value")]
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// The date if the control type is a datepicker. The value property is ignored in this case.
        /// </summary>
        [JsonProperty("date")]
        [DataMember(Name = "date")]
        public DateTime? Date { get; set; }

        public bool IsFile { get; set; }

        public bool IsText { get; set; }
        public bool IsTextArea { get; set; }

        public string Alias { get; set; }        
        public string ValueAlias { get; set; }

        public override string ToString()
        {
            var key = Alias.HasValue()
                ? $"{Alias}-{ProductId}-{BundleItemId}-{VariantAttributeId}"
                : CreateKey(ProductId, BundleItemId, AttributeId, VariantAttributeId);

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

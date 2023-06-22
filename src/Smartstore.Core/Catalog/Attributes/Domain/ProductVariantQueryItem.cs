using System.Runtime.Serialization;

namespace Smartstore.Core.Catalog.Attributes
{
    [DataContract]
    public class ProductVariantQueryItem
    {
        public ProductVariantQueryItem()
        {
        }

        public ProductVariantQueryItem(string value)
        {
            Value = value ?? string.Empty;
        }

        /// <summary>
        /// Creates a key used for form names.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="bundleItemId">Bundle item identifier. 0 if not a bundle item.</param>
        /// <param name="attributeId">Product attribute identifier.</param>
        /// <param name="variantAttributeId">Product variant attribute identifier.</param>
        /// <returns>Key.</returns>
        public static string CreateKey(int productId, int bundleItemId, int attributeId, int variantAttributeId)
        {
            return $"pvari{productId}-{bundleItemId}-{attributeId}-{variantAttributeId}";
        }

        [DataMember(Name = "value")]
        public string Value { get; set; } = string.Empty;

        [DataMember(Name = "productId")]
        public int ProductId { get; set; }

        [DataMember(Name = "bundleItemId")]
        public int BundleItemId { get; set; }

        [DataMember(Name = "attributeId")]
        public int AttributeId { get; set; }

        [DataMember(Name = "variantAttributeId")]
        public int VariantAttributeId { get; set; }

        [DataMember(Name = "date")]
        public DateTime? Date { get; set; }

        [DataMember(Name = "isFile")]
        public bool IsFile { get; set; }

        [DataMember(Name = "isText")]
        public bool IsText { get; set; }

        [DataMember(Name = "alias")]
        public string Alias { get; set; }
        
        [DataMember(Name = "valueAlias")]
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

            return key;
        }
    }
}

using System;

namespace Smartstore.Core.Checkout.Attributes
{
    public class CheckoutAttributeQueryItem
    {
        public CheckoutAttributeQueryItem(int attributeId, string value)
        {
            Value = value ?? string.Empty;
            AttributeId = attributeId;
        }

        /// <summary>
        /// Creates a key used for form names.
        /// </summary>
        /// <param name="attributeId">Checkout attribute identifier.</param>
        /// <returns>Key.</returns>
        public static string CreateKey(int attributeId)
        {
            return $"cattr{attributeId}";
        }

        public string Value { get; init; }
        public int AttributeId { get; init; }
        public DateTime? Date { get; set; }
        public bool IsFile { get; set; }
        public bool IsText { get; set; }

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

            return key;
        }
    }
}

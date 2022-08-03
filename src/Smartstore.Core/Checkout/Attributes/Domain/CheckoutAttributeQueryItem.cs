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
            => $"cattr{attributeId}";

        /// <summary>
        /// Gets the attribute value.
        /// </summary>
        public string Value { get; init; }

        /// <summary>
        /// Gets the attribute identifier.
        /// </summary>
        public int AttributeId { get; init; }

        /// <summary>
        /// Gets or sets the selected date time.
        /// </summary>
        public DateTime? Date { get; set; }

        /// <summary>
        /// Gets or sets a Value indicating whether the attribute is a file.
        /// </summary>
        public bool IsFile { get; set; }

        /// <summary>
        /// Gets or sets a Value indicating whether the attribute is text.
        /// </summary>
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

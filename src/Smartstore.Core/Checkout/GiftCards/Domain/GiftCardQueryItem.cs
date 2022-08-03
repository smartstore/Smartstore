namespace Smartstore.Core.Checkout.GiftCards
{
    public partial class GiftCardQueryItem
    {
        /// <summary>
        /// Represents a gift card query item
        /// </summary>
        public GiftCardQueryItem(string name, string value)
        {
            Guard.NotEmpty(name, nameof(name));

            Name = name.StartsWith('.') ? name[1..].ToLower() : name.ToLower();
            Value = value ?? string.Empty;
        }

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// Gets the value
        /// </summary>
        public string Value { get; init; }

        /// <summary>
        /// Gets or sets product identifier
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Gets or sets bundle item identifier
        /// </summary>
        public int BundleItemId { get; set; }

        /// <summary>
        /// Creates gift card key with product identifier, bundle item identifier and with <see cref="Name"/> if possible
        /// </summary>
        public static string CreateKey(int productId, int bundleItemId, string name)
        {
            if (name.HasValue())
            {
                return $"giftcard{productId}-{bundleItemId}-.{name.EmptyNull().ToLower()}";
            }

            // Just return field prefix for partial views.
            return $"giftcard{productId}-{bundleItemId}-";
        }

        /// <summary>
        /// Overrides default <see cref="object.ToString()"/>. Calls <see cref="CreateKey(int, int, string)"/> to generate key string
        /// </summary>
        public override string ToString() => CreateKey(ProductId, BundleItemId, Name);
    }
}

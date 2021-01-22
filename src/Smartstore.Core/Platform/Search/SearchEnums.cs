namespace Smartstore.Core.Search
{
    public enum SearchMode
    {
        /// <summary>
        /// Term search
        /// </summary>
        ExactMatch = 0,

        /// <summary>
        /// Prefix term search
        /// </summary>
        StartsWith,

        /// <summary>
        /// Wildcard search
        /// </summary>
        Contains
    }

    public enum SearchDocumentType
    {
        Product = 0,
        Category,
        Manufacturer,
        DeliveryTime,
        Attribute,
        AttributeValue,
        Variant,
        VariantValue,
        Customer,
        Forum,
        ForumPost
    }
}

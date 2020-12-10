namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Gradually restricts the visibility of products.
    /// </summary>
    public enum ProductVisibility
    {
        /// <summary>
        /// Product is fully visible.
        /// </summary>
        Full = 0,

        /// <summary>
        /// Product is visible in search results.
        /// </summary>
        SearchResults = 10,

        /// <summary>
        /// Product is not visible in lists but clickable on product pages.
        /// </summary>
        ProductPage = 20,

        /// <summary>
        /// Product is not visible but appears on grouped product pages.
        /// </summary>
        Hidden = 30
    }

    /// <summary>
    /// Represents a product condition.
    /// <see cref="https://schema.org/OfferItemCondition"/>
    /// </summary>
    public enum ProductCondition
    {
        New = 0,
        Refurbished = 10,
        Used = 20,
        Damaged = 30
    }

    /// <summary>
    /// Represents a quantity control type.
    /// </summary>
    public enum QuantityControlType
    {
        Spinner = 0,
        Dropdown = 1
    }

    /// <summary>
    /// Represents the behaviour when selecting product attributes.
    /// </summary>
    public enum AttributeChoiceBehaviour
    {
        /// <summary>
        /// Gray out unavailable attributes.
        /// </summary>
        GrayOutUnavailable = 0,

        /// <summary>
        /// No particular behaviour.
        /// </summary>
        None = 30
    }
}

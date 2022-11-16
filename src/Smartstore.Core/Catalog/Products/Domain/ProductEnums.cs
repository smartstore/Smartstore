namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Represents the product type.
    /// </summary>
    public enum ProductType
    {
        /// <summary>
        /// Simple product.
        /// </summary>
        SimpleProduct = 5,

        /// <summary>
        /// Grouped product.
        /// </summary>
        GroupedProduct = 10,

        /// <summary>
        /// Bundled product.
        /// </summary>
        BundledProduct = 15
    }

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
    /// Represents a product condition. <see href="https://schema.org/OfferItemCondition"/>.
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

    /// <summary>
    /// Represents the backorder mode.
    /// </summary>
    public enum BackorderMode
    {
        /// <summary>
        /// No backorders.
        /// </summary>
        NoBackorders = 0,

        /// <summary>
        /// Allow quantity below 0.
        /// </summary>
        AllowQtyBelow0 = 1,

        /// <summary>
        /// Allow quantity below 0 and notify customer.
        /// </summary>
        AllowQtyBelow0AndNotifyCustomer = 2
    }

    /// <summary>
    /// Represents the download activation type.
    /// </summary>
    public enum DownloadActivationType
    {
        /// <summary>
        /// Activate when order is paid.
        /// </summary>
        WhenOrderIsPaid = 1,

        /// <summary>
        /// Activate manually.
        /// </summary>
        Manually = 10
    }

    /// <summary>
    /// Represents the low stock activity.
    /// </summary>
    public enum LowStockActivity
    {
        /// <summary>
        /// Do nothing.
        /// </summary>
        Nothing = 0,

        /// <summary>
        /// Disable buy button.
        /// </summary>
        DisableBuyButton = 1,

        /// <summary>
        /// Do not publish.
        /// </summary>
        Unpublish = 2
    }

    /// <summary>
    /// Represents the method of inventory management.
    /// </summary>
    public enum ManageInventoryMethod
    {
        /// <summary>
        /// Don't track inventory.
        /// </summary>
        DontManageStock = 0,

        /// <summary>
        /// Track inventory.
        /// </summary>
        ManageStock = 1,

        /// <summary>
        /// Track inventory by product attributes.
        /// </summary>
        ManageStockByAttributes = 2
    }

    /// <summary>
    /// Represents the recurring product cycle period.
    /// </summary>
    public enum RecurringProductCyclePeriod
    {
        /// <summary>
        /// Days.
        /// </summary>
        Days = 0,

        /// <summary>
        /// Weeks.
        /// </summary>
        Weeks = 10,

        /// <summary>
        /// Months.
        /// </summary>
        Months = 20,

        /// <summary>
        /// Years.
        /// </summary>
        Years = 30
    }

    /// <summary>
    /// Represents the product sorting.
    /// </summary>
    public enum ProductSortingEnum
    {
        /// <summary>
        /// Initial state
        /// </summary>
        Initial = 0,

        /// <summary>
        /// Relevance
        /// </summary>
        Relevance = 1,

        /// <summary>
        /// Name: A to Z
        /// </summary>
        NameAsc = 5,

        /// <summary>
        /// Name: Z to A
        /// </summary>
        NameDesc = 6,

        /// <summary>
        /// Price: Low to High
        /// </summary>
        PriceAsc = 10,

        /// <summary>
        /// Price: High to Low
        /// </summary>
        PriceDesc = 11,

        /// <summary>
        /// Product creation date.
        /// Actually CreatedOnDesc (but due to localization this remains as is).
        /// </summary>
        CreatedOn = 15,

        /// <summary>
        /// Product creation date
        /// </summary>
        CreatedOnAsc = 16
    }
}

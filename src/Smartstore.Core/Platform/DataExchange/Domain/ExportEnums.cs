namespace Smartstore.Core.DataExchange
{
    /// <summary>
    /// Represents export entity types.
    /// </summary>
    public enum ExportEntityType
    {
        Product = 0,
        Category,
        Manufacturer,
        Customer,
        Order,
        NewsletterSubscription,
        ShoppingCartItem
    }

    /// <summary>
    /// Represents related export entity types (data without own export provider or importer).
    /// </summary>
    public enum RelatedEntityType
    {
        TierPrice = 0,
        ProductVariantAttributeValue,
        ProductVariantAttributeCombination
    }

    /// <summary>
    /// Represents export deployment types.
    /// </summary>
    public enum ExportDeploymentType
    {
        FileSystem = 0,
        Email,
        Http,
        Ftp,
        PublicFolder
    }

    /// <summary>
    /// Represents export HTTP transmission types.
    /// </summary>
    public enum ExportHttpTransmissionType
    {
        SimplePost = 0,
        MultipartFormDataPost
    }

    /// <summary>
    /// Controls the merging of various data as product description during an export.
    /// </summary>
    public enum ExportDescriptionMerging
    {
        None = 0,
        ShortDescriptionOrNameIfEmpty,
        ShortDescription,
        Description,
        NameAndShortDescription,
        NameAndDescription,
        ManufacturerAndNameAndShortDescription,
        ManufacturerAndNameAndDescription
    }

    /// <summary>
    /// Controls the merging of various data while exporting attribute combinations as products.
    /// </summary>
    public enum ExportAttributeValueMerging
    {
        None = 0,
        AppendAllValuesToName
    }

    /// <summary>
    /// Controls data processing and projection items supported by an export provider.
    /// </summary>
    [Flags]
    public enum ExportFeatures
    {
        None = 0,

        /// <summary>
        /// A value indicating whether to automatically create a file based public deployment when an export profile is created.
        /// </summary>
        CreatesInitialPublicDeployment = 1,

        /// <summary>
        /// A value indicating whether to offer option to include\exclude grouped products.
        /// </summary>
        CanOmitGroupedProducts = 1 << 2,

        /// <summary>
        /// A value indicating whether to offer option to export attribute combinations as products.
        /// </summary>
        CanProjectAttributeCombinations = 1 << 3,

        /// <summary>
        /// A value indicating whether to offer further options to manipulate the product description.
        /// </summary>
        CanProjectDescription = 1 << 4,

        /// <summary>
        /// A value indicating whether to offer option to enter a brand fallback.
        /// </summary>
        OffersBrandFallback = 1 << 5,

        /// <summary>
        /// A value indicating whether to offer option to set a picture size and to get the URL of the main image.
        /// </summary>
        CanIncludeMainPicture = 1 << 6,

        /// <summary>
        /// A value indicating whether to use SKU as manufacturer part number if MPN is empty.
        /// </summary>
        UsesSkuAsMpnFallback = 1 << 7,

        /// <summary>
        /// A value indicating whether to offer option to enter a shipping time fallback.
        /// </summary>
        OffersShippingTimeFallback = 1 << 8,

        /// <summary>
        /// A value indicating whether to offer option to enter a shipping costs fallback and a free shipping threshold.
        /// </summary>
        OffersShippingCostsFallback = 1 << 9,

        /// <summary>
        /// A value indicating whether to not automatically send a completion email.
        /// </summary>
        CanOmitCompletionMail = 1 << 10,

        /// <summary>
        /// A value indicating whether to provide additional data of attribute combinations.
        /// </summary>
        UsesAttributeCombination = 1 << 11,

        /// <summary>
        /// A value indicating whether to export attribute combinations as products including parent product. Only effective with CanProjectAttributeCombinations.
        /// </summary>
        UsesAttributeCombinationParent = 1 << 12,

        /// <summary>
        /// A value indicating whether to provide extra data units for related data.
        /// </summary>
        UsesRelatedDataUnits = 1 << 13
    }

    /// <summary>
    /// Possible order status change after exporting orders.
    /// </summary>
    public enum ExportOrderStatusChange
    {
        None = 0,
        Processing,
        Complete
    }
}

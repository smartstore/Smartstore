using System.Xml.Serialization;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Core.DataExchange
{
    /// <summary>
    /// Contains data projected onto an export.
    /// </summary>
    [Serializable]
    public class ExportProjection
    {
        #region All entity types

        /// <summary>
        /// Store identifier.
        /// </summary>
        public int? StoreId { get; set; }

        /// <summary>
        /// The language to be applied to the export.
        /// </summary>
        public int? LanguageId { get; set; }

        /// <summary>
        /// The currency to be applied to the export.
        /// </summary>
        public int? CurrencyId { get; set; }

        /// <summary>
        /// Customer identifier.
        /// </summary>
        public int? CustomerId { get; set; }

        #endregion

        #region Product

        /// <summary>
        /// Description merging identifier.
        /// </summary>
        public int DescriptionMergingId { get; set; }

        /// <summary>
        /// Decription merging.
        /// </summary>
        [XmlIgnore]
        public ExportDescriptionMerging DescriptionMerging
        {
            get => (ExportDescriptionMerging)DescriptionMergingId;
            set => DescriptionMergingId = (int)value;
        }

        /// <summary>
        /// A value indicating whether to convert HTML decription to plain text.
        /// </summary>
        public bool DescriptionToPlainText { get; set; }

        /// <summary>
        /// Comma separated text to append to the decription.
        /// </summary>
        public string AppendDescriptionText { get; set; }

        /// <summary>
        /// Remove critical characters from the description.
        /// </summary>
        public bool RemoveCriticalCharacters { get; set; }

        /// <summary>
        /// Comma separated list of critical characters.
        /// </summary>
        public string CriticalCharacters { get; set; }

        /// <summary>
        /// The price type for calculating the product price.
        /// </summary>
        public PriceDisplayType? PriceType { get; set; }

        /// <summary>
        /// A value indicating whether to convert net to gross prices.
        /// </summary>
        public bool ConvertNetToGrossPrices { get; set; }

        /// <summary>
        /// Fallback for product brand.
        /// </summary>
        public string Brand { get; set; }

        /// <summary>
        /// Number of images per object to be exported.
        /// </summary>
        public int? NumberOfMediaFiles { get; set; }

        /// <summary>
        /// The size of exported pictures in pixel.
        /// </summary>
        public int PictureSize { get; set; }

        /// <summary>
        /// Fallback for shipping time.
        /// </summary>
        public string ShippingTime { get; set; }

        /// <summary>
        /// Fallback for shipping costs.
        /// </summary>
        public decimal? ShippingCosts { get; set; }

        /// <summary>
        /// Free shipping threshold.
        /// </summary>
        public decimal? FreeShippingThreshold { get; set; }

        /// <summary>
        /// A value indicating whether to export attribute combinations as products.
        /// </summary>
        public bool AttributeCombinationAsProduct { get; set; }

        /// <summary>
        /// Identifier for merging attribute values of attribute combinations.
        /// </summary>
        public int AttributeCombinationValueMergingId { get; set; }

        /// <summary>
        /// Merging attribute values of attribute combinations.
        /// </summary>
        [XmlIgnore]
        public ExportAttributeValueMerging AttributeCombinationValueMerging
        {
            get => (ExportAttributeValueMerging)AttributeCombinationValueMergingId;
            set => AttributeCombinationValueMergingId = (int)value;
        }

        /// <summary>
        /// A value indicating whether to export grouped products.
        /// </summary>
        public bool NoGroupedProducts { get; set; }

        /// <summary>
        /// A value indicating whether to ignore associated products with <see cref="Product.Visibility"/> set to <see cref="ProductVisibility.Hidden"/> during export.
        /// </summary>
        public bool OnlyIndividuallyVisibleAssociated { get; set; } = true;

        #endregion

        #region Order

        /// <summary>
        /// Identifier of the new state of order after exporting them.
        /// </summary>
        public int OrderStatusChangeId { get; set; }

        /// <summary>
        /// Gets or sets the new state of orders.
        /// </summary>
        [XmlIgnore]
        public ExportOrderStatusChange OrderStatusChange
        {
            get => (ExportOrderStatusChange)OrderStatusChangeId;
            set => OrderStatusChangeId = (int)value;
        }

        #endregion

        #region Shopping Cart Item

        /// <summary>
        /// A value indicating whether to export bundle products.
        /// </summary>
        public bool NoBundleProducts { get; set; }

        #endregion
    }
}

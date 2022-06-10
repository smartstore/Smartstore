namespace Smartstore.Core.Catalog.Products
{
    /// <summary>
    /// Represents a related product.
    /// </summary>
    [Index(nameof(ProductId1), Name = "IX_RelatedProduct_ProductId1")]
    public partial class RelatedProduct : BaseEntity, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the first product identifier.
        /// </summary>
        public int ProductId1 { get; set; }

        /// <summary>
        /// Gets or sets the second product identifier.
        /// </summary>
        public int ProductId2 { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}

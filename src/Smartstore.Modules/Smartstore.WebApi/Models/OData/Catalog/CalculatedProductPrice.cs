namespace Smartstore.Web.Api.Models.Catalog
{
    /// <summary>
    /// Represents the result of a price calculation process for a single product.
    /// </summary>
    public class CalculatedProductPrice
    {
        /// <summary>
        /// The identifier of the product for which a price was calculated.
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// The identifier of the currency into which the prices were converted.
        /// </summary>
        public int CurrencyId { get; set; }

        /// <summary>
        /// The ISO code of the currency into which the prices were converted.
        /// </summary>
        public string CurrencyCode { get; set; }

        /// <summary>
        /// The final price of the product.
        /// </summary>
        public decimal FinalPrice { get; set; }

        /// <summary>
        /// The regular unit price of the product.
        /// Usually Product.Price, Product.ComparePrice or Product.SpecialPrice.
        /// </summary>
        public decimal? RegularPrice { get; set; }

        /// <summary>
        /// The retail unit price (MSRP) of the product.
        /// A retail price is given Product.ComparePrice is not 0 and Product.ComparePriceLabelId referes to an MSRP label.
        /// </summary>
        public decimal? RetailPrice { get; set; }

        /// <summary>
        /// The special offer price, if any (see Product.SpecialPrice).
        /// </summary>
        public decimal? OfferPrice { get; set; }

        /// <summary>
        /// The date until <see cref="FinalPrice"/> is valid.
        /// </summary>
        public DateTime? ValidUntilUtc { get; set; }

        /// <summary>
        /// The price that is initially displayed on the product detail page, if any.
        /// Includes price adjustments of preselected attributes and prices of attribute combinations.
        /// </summary>
        public decimal? PreselectedPrice { get; set; }

        /// <summary>
        /// The lowest possible price of a product, if any.
        /// Includes prices of attribute combinations and tier prices. Ignores price adjustments of attributes.
        /// </summary>
        public decimal? LowestPrice { get; set; }

        /// <summary>
        /// The discount amount applied to FinalPrice.
        /// </summary>
        public decimal DiscountAmount { get; set; }

        // INFO: produces "The type 'Tax' must be a reference type in order to use it as parameter 'TComplexType' in a generic type or method".
        //public Tax? Tax { get; set; }

        /// <summary>
        /// A price saving in relation to FinalPrice.
        /// The saving results from the applied discounts, if any, otherwise from the difference to the Product.ComparePrice.
        /// </summary>
        public ProductPriceSaving Saving { get; set; }

        public class ProductPriceSaving
        {
            /// <summary>
            /// A value indicating whether there is a price saving on the calculated final price.
            /// </summary>
            public bool HasSaving { get; set; }

            /// <summary>
            /// The price that represents the saving. Often displayed as a crossed-out price.
            /// Always greater than the final price if HasSaving is true.
            /// </summary>
            public decimal SavingPrice { get; set; }

            /// <summary>
            /// The saving, in percent, compared to the final price.
            /// </summary>
            public float SavingPercent { get; set; }

            /// <summary>
            /// The saving, as money amount, compared to the final price.
            /// </summary>
            public decimal? SavingAmount { get; set; }
        }
    }
}

using Smartstore.Core.Catalog.Pricing;

namespace Smartstore.Admin.Models.Export
{
    [LocalizedDisplay("Admin.DataExchange.Export.Projection.")]
    public class ExportProjectionModel
    {
        #region All entity types

        [LocalizedDisplay("*StoreId")]
        public int? StoreId { get; set; }

        [LocalizedDisplay("*LanguageId")]
        public int? LanguageId { get; set; }

        [LocalizedDisplay("*CurrencyId")]
        public int? CurrencyId { get; set; }

        [LocalizedDisplay("*CustomerId")]
        [AdditionalMetadata("invariant", true)]
        public int? CustomerId { get; set; }

        #endregion

        #region Product

        [LocalizedDisplay("*DescriptionMerging")]
        public int DescriptionMergingId { get; set; }

        [LocalizedDisplay("*DescriptionToPlainText")]
        public bool DescriptionToPlainText { get; set; }

        [LocalizedDisplay("*AppendDescriptionText")]
        public string[] AppendDescriptionText { get; set; }

        [LocalizedDisplay("*RemoveCriticalCharacters")]
        public bool RemoveCriticalCharacters { get; set; }

        [LocalizedDisplay("*CriticalCharacters")]
        public string[] CriticalCharacters { get; set; }

        [LocalizedDisplay("*PriceType")]
        public PriceDisplayType? PriceType { get; set; }

        [LocalizedDisplay("*ConvertNetToGrossPrices")]
        public bool ConvertNetToGrossPrices { get; set; }

        [LocalizedDisplay("*Brand")]
        public string Brand { get; set; }

        [LocalizedDisplay("*NumberOfPictures")]
        public int? NumberOfPictures { get; set; }

        [LocalizedDisplay("*PictureSize")]
        public int PictureSize { get; set; }

        [LocalizedDisplay("*ShippingTime")]
        public string ShippingTime { get; set; }

        [LocalizedDisplay("*ShippingCosts")]
        public decimal? ShippingCosts { get; set; }

        [LocalizedDisplay("*FreeShippingThreshold")]
        public decimal? FreeShippingThreshold { get; set; }

        [LocalizedDisplay("*AttributeCombinationAsProduct")]
        public bool AttributeCombinationAsProduct { get; set; }

        [LocalizedDisplay("*AttributeCombinationValueMerging")]
        public int AttributeCombinationValueMergingId { get; set; }

        [LocalizedDisplay("*NoGroupedProducts")]
        public bool NoGroupedProducts { get; set; }

        [LocalizedDisplay("*OnlyIndividuallyVisibleAssociated")]
        public bool OnlyIndividuallyVisibleAssociated { get; set; }

        #endregion

        #region Order

        [LocalizedDisplay("*OrderStatusChange")]
        public int OrderStatusChangeId { get; set; }

        #endregion

        #region Shopping Cart Item

        [LocalizedDisplay("*NoBundleProducts")]
        public bool NoBundleProducts { get; set; }

        #endregion
    }
}

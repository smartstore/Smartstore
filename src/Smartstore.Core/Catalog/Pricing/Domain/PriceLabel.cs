using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Localization;
using Smartstore.Data.Caching;

namespace Smartstore.Core.Catalog.Pricing
{
    /// <summary>
    /// Represents a price label
    /// </summary>
    [CacheableEntity]
    public partial class PriceLabel : EntityWithAttributes, ILocalizedEntity, IDisplayOrder
    {
        /// <summary>
        /// Gets or sets the short name that is usually displayed in listings, e.g. "MSRP", "Lowest".
        /// </summary>
        [Required, StringLength(16)]
        [LocalizedProperty]
        public string ShortName { get; set; }

        /// <summary>
        /// Gets or sets the optional name that is usually displayed in product detail, e.g. "Retail price", "Lowest recent price".
        /// </summary>
        [StringLength(50)]
        [LocalizedProperty]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the optional description that is usually displayed in product detail tooltips.
        /// </summary>
        [StringLength(400)]
        [LocalizedProperty]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this label represents an MSRP price.
        /// </summary>
        public bool IsRetailPrice { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the label's short name should be displayed in product listings.
        /// </summary>
        public bool DisplayShortNameInLists { get; set; }

        /// <summary>
        /// Gets or sets the display order
        /// </summary>
        public int DisplayOrder { get; set; }

        //private static List<PriceLabel> SeedGerman()
        //{
        //    return new()
        //    {
        //        new PriceLabel 
        //        {
        //            ShortName = "UVP",
        //            Name = "Unverb. Preisempf.",
        //            Description = "Die UVP ist der vorgeschlagene oder empfohlene Verkaufspreis eines Produkts, wie er vom Hersteller angegeben und vom Hersteller, einem Lieferanten oder Händler zur Verfügung gestellt wird.",
        //            IsRetailPrice = true,
        //            DisplayShortNameInLists = true
        //        },
        //        new PriceLabel
        //        {
        //            ShortName = "Niedrigster",
        //            Name = "Zuletzt niedrigster Preis",
        //            Description = "Es handelt sich um den niedrigsten Preis des Produktes in den letzten 30 Tagen vor der Anwendung der Preisermäßigung.",
        //            DisplayShortNameInLists = true
        //        },
        //        new PriceLabel
        //        {
        //            ShortName = "Regulär",
        //            Name = "Regulär",
        //            Description = "Es handelt sich um den mittleren Verkaufspreis, den Kunden für ein Produkt in unserem Shop zahlen, ausgenommen Aktionspreise."
        //        }
        //    };
        //}

        //private static List<PriceLabel> SeedEnglish()
        //{
        //    return new()
        //    {
        //        new PriceLabel
        //        {
        //            ShortName = "MSRP",
        //            Name = "Suggested retail price",
        //            Description = "The Suggested Retail Price (MSRP) is the suggested or recommended retail price of a product set by the manufacturer and provided by a manufacturer, supplier, or seller.",
        //            IsRetailPrice = true,
        //            DisplayShortNameInLists = true
        //        },
        //        new PriceLabel
        //        {
        //            ShortName = "Lowest",
        //            Name = "Lowest recent price",
        //            Description = "This is the lowest price of the product in the past 30 days prior to the application of the price reduction.",
        //            DisplayShortNameInLists = true
        //        },
        //        new PriceLabel
        //        {
        //            ShortName = "Regular",
        //            Name = "Regular price",
        //            Description = "The Regular Price is the median selling price paid by customers for a product, excluding promotional prices"
        //        }
        //    };
        //}

        //private static PriceSettings SeedPriceSettingsGerman()
        //{
        //    return new PriceSettings
        //    {
        //        OfferBadgeLabel = "Deal",
        //        LimitedOfferBadgeLabel = "Befristetes Angebot"
        //    };
        //}

        //private static PriceSettings SeedPriceSettingsEnglish()
        //{
        //    return new PriceSettings
        //    {
        //        OfferBadgeLabel = "Deal",
        //        LimitedOfferBadgeLabel = "Limited time deal"
        //    };
        //}
    }
}

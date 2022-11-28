using System.ComponentModel.DataAnnotations;
using Smartstore.Core.Catalog.Products;

namespace Smartstore.Web.Api.Models.Catalog
{
    /// <summary>
    /// Represents query parameters for a catalog search.
    /// </summary>
    public class CatalogSearchQueryModel
    {
        /// <summary>
        /// The search term.
        /// </summary>
        /// <example>iphone</example>
        [Required]
        [FromQuery(Name = "q")]
        public string Term { get; set; }

        /// <summary>
        /// The page index, starting from 1. **$skip** is ignored. Example: 1.
        /// </summary>
        [FromQuery(Name = "i")]
        public int PageIndex { get; set; }

        /// <summary>
        /// The page size. **$top** is ignored.  Example: 100.
        /// </summary>
        [FromQuery(Name = "s")]
        public int PageSize { get; set; } = WebApiSettings.DefaultMaxTop;

        /// <summary>
        /// Product sorting. **$orderby** is ignored.
        /// </summary>
        [FromQuery(Name = "o")]
        public ProductSortingEnum OrderBy { get; set; }

        /// <summary>
        /// Comma separated list of category identifiers. Example: **2,3**.
        /// </summary>
        [FromQuery(Name = "c")]
        public string CategoryIds { get; set; }

        /// <summary>
        /// Comma separated list of manufacturer identifiers. Example: **5**.
        /// </summary>
        [FromQuery(Name = "m")]
        public string ManufacturerIds { get; set; }

        /// <summary>
        /// Comma separated list of delivery time identifiers. Example: **8,9**.
        /// </summary>
        [FromQuery(Name = "d")]
        public string DeliveryTimeIds { get; set; }

        /// <summary>
        /// Price range (from~to or from(~) or ~to). Example: **100~150**.
        /// </summary>
        [FromQuery(Name = "p")]
        public string PriceRange { get; set; }

        /// <summary>
        /// Minimum rating. Example: **2**.
        /// </summary>
        [FromQuery(Name = "r")]
        public double Rating { get; set; }

        /// <summary>
        /// Availability. Example: **true**.
        /// </summary>
        [FromQuery(Name = "a")]
        public bool Availability { get; set; }

        /// <summary>
        /// New arrivals. Example: **true**.
        /// </summary>
        [FromQuery(Name = "n")]
        public bool NewArrivals { get; set; }
    }
}

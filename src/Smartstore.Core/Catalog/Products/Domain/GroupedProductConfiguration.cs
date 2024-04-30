using System.ComponentModel;
using Newtonsoft.Json;

namespace Smartstore.Core.Catalog.Products
{
    public static class AssociatedProductHeader
    {
        public const string Image = "image";
        public const string Sku = "sku";
        public const string Dimensions = "dimensions";
        public const string Weight = "weight";
        public const string Price = "price";
    }

    /// <summary>
    /// Represents the configuration of a grouped product and its associated products.
    /// </summary>
    public partial class GroupedProductConfiguration
    {
        const int DefaultPageSize = 20;
        const int DefaultSearchMinAssociatedCount = 10;

        /// <summary>
        /// The number of associated products per page. The default is <see cref="DefaultPageSize"/>.
        /// </summary>
        [JsonProperty("pageSize", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(DefaultPageSize)]
        public int PageSize { get; set; } = DefaultPageSize;

        /// <summary>
        /// Minimum number of associated products from which the search box is displayed. The default is <see cref="DefaultSearchMinAssociatedCount"/>.
        /// </summary>
        [JsonProperty("searchMinAssociatedCount", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(DefaultSearchMinAssociatedCount)]
        public int SearchMinAssociatedCount { get; set; } = DefaultSearchMinAssociatedCount;

        /// <summary>
        /// A value indicating whether the associated products are collapsible.
        /// </summary>
        [JsonProperty("collapsible", DefaultValueHandling = DefaultValueHandling.Ignore), DefaultValue(false)]
        public bool Collapsible { get; set; }

        /// <summary>
        /// Gets or sets name of fields to display in the collapse header.
        /// </summary>
        [JsonProperty("headerFields", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[] HeaderFields { get; set; }

        public virtual string AsJson()
            => IsTouched() ? JsonConvert.SerializeObject(this) : null;

        /// <summary>
        /// Gets a value indicating whether a header column is displayed.
        /// </summary>
        /// <param name="name"><see cref="AssociatedProductHeader"/>.</param>
        public bool HasHeader(string name)
            => HeaderFields?.Any(x => x.EqualsNoCase(name)) ?? false;

        // TODO: (mg) Revert & rename --> IsDefault()
        private bool IsTouched()
            => PageSize != DefaultPageSize || SearchMinAssociatedCount != DefaultSearchMinAssociatedCount || Collapsible || (Collapsible && !HeaderFields.IsNullOrEmpty());
    }
}

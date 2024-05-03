#nullable enable

using System.ComponentModel;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Smartstore.Utilities;

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
        public string[]? HeaderFields { get; set; }

        public virtual string? ToJson()
            => IsDefault() ? null : JsonConvert.SerializeObject(this);

        /// <summary>
        /// Gets a value indicating whether a header column is displayed.
        /// </summary>
        /// <param name="name"><see cref="AssociatedProductHeader"/>.</param>
        public bool HasHeader(string name)
            => HeaderFields?.Any(x => x.EqualsNoCase(name)) ?? false;

        private bool IsDefault()
            => PageSize == DefaultPageSize && SearchMinAssociatedCount == DefaultSearchMinAssociatedCount && !Collapsible;
    }

#nullable disable

    public class GroupedProductConfigurationConverter : ValueConverter<GroupedProductConfiguration, string>
    {
        public GroupedProductConfigurationConverter()
            : base(
                  v => Serialize(v),
                  v => Deserialize(v))
        {
        }

        private static string Serialize(GroupedProductConfiguration obj)
        {
            return obj.ToJson();
        }

        private static GroupedProductConfiguration Deserialize(string json)
        {
            return CommonHelper.TryAction(() => JsonConvert.DeserializeObject<GroupedProductConfiguration>(json));
        }
    }
}

﻿#nullable enable

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Smartstore.Utilities;

namespace Smartstore.Core.Catalog.Products
{
    public static class AssociatedProductHeader
    {
        public const string Name = "name";
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
        /// <summary>
        /// The number of associated products per page.
        /// </summary>
        [JsonProperty("pageSize", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? PageSize { get; set; }

        /// <summary>
        /// Minimum number of associated products from which the search box is displayed.
        /// </summary>
        [JsonProperty("searchMinAssociatedCount", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int? SearchMinAssociatedCount { get; set; }

        /// <summary>
        /// A value indicating whether the associated products are collapsible.
        /// </summary>
        [JsonProperty("collapsible", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool? Collapsible { get; set; }

        /// <summary>
        /// Gets or sets name of fields to display in the collapse header.
        /// </summary>
        [JsonProperty("headerFields", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string[]? HeaderFields { get; set; }

        public virtual string? ToJson()
        {
            if (IsDefault())
            {
                return null;
            }

            if (HeaderFields.IsNullOrEmpty())
            {
                HeaderFields = null;
            }

            return JsonConvert.SerializeObject(this);
        }

        private bool IsDefault()
            => PageSize == null && SearchMinAssociatedCount == null && Collapsible == null && HeaderFields.IsNullOrEmpty();
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

        private static string Serialize(GroupedProductConfiguration obj) => obj.ToJson();

        private static GroupedProductConfiguration Deserialize(string json) => 
            CommonHelper.TryAction(() => JsonConvert.DeserializeObject<GroupedProductConfiguration>(json));
    }
}

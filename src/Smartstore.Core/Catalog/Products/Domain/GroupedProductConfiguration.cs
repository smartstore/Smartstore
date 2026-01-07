#nullable enable

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Smartstore.Json;
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
        /// The localized title for the associated products list.
        /// The key is the language culture and the value is the localized title.
        /// </summary>
        public Dictionary<string, string>? Titles { get; set; }

        /// <summary>
        /// The number of associated products per page.
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// Minimum number of associated products from which the search box is displayed.
        /// </summary>
        public int? SearchMinAssociatedCount { get; set; }

        /// <summary>
        /// A value indicating whether the associated products are collapsible.
        /// </summary>
        public bool? Collapsible { get; set; }

        /// <summary>
        /// Gets or sets name of fields to display in the collapse header.
        /// </summary>
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

            Titles = Titles?.Where(x => x.Value.HasValue())?.ToDictionary(x => x.Key, x => x.Value);

            if (Titles.IsNullOrEmpty())
            {
                Titles = null;
            }

            return JsonSerializer.Serialize(this, SmartJsonOptions.CamelCasedIgnoreDefaultValue);
        }

        private bool IsDefault()
        {
            return PageSize == null
                && SearchMinAssociatedCount == null
                && Collapsible == null
                && HeaderFields.IsNullOrEmpty()
                && (Titles?.Values?.All(x => x.IsEmpty()) ?? true);
        }
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
            return CommonHelper.TryAction(() => JsonSerializer.Deserialize<GroupedProductConfiguration>(json, SmartJsonOptions.CamelCasedIgnoreDefaultValue));
        }
    }
}
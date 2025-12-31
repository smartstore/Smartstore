using System.Text.Json.Serialization;
using Smartstore.Core.Catalog.Attributes;

namespace Smartstore.Web.Api.Models.Catalog;

/// <summary>
/// Represents a managed product attributes and its values.
/// </summary>
public class ManagedProductAttribute
{
    /// <summary>
    /// The name of the product attribute.
    /// </summary>
    /// <example>Color</example>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// A value indicating whether the attribute is required (mandatory to be selected).
    /// </summary>
    [JsonPropertyName("isRequired")]
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// The input control type of the attribute.
    /// </summary>
    [JsonPropertyName("controlType")]
    public AttributeControlType ControlType { get; set; } = AttributeControlType.DropdownList;

    /// <summary>
    /// Any custom data. It's not used by Smartstore but is being passed to the choice partial view.
    /// </summary>
    [JsonPropertyName("customData")]
    public string CustomData { get; set; }

    /// <summary>
    /// A list of attribute values.
    /// </summary>
    [JsonPropertyName("values")]
    public List<ManageProductAttributeValue> Values { get; set; } = new();

    public class ManageProductAttributeValue
    {
        /// <summary>
        /// The name of of the attribute value.
        /// </summary>
        /// <example>Green</example>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// The SEO friendly search alias.
        /// </summary>
        [JsonPropertyName("alias")]
        public string Alias { get; set; }

        /// <summary>
        /// The color RGB value (used with "Boxes" attribute type).
        /// </summary>
        /// <example>#00ff00</example>
        [JsonPropertyName("color")]
        public string Color { get; set; }

        /// <summary>
        /// A price adjustment\surcharge.
        /// </summary>
        /// <example>2.5</example>
        [JsonPropertyName("priceAdjustment")]
        public decimal PriceAdjustment { get; set; }

        /// <summary>
        /// A weight adjustment.
        /// </summary>
        [JsonPropertyName("weightAdjustment")]
        public decimal WeightAdjustment { get; set; }

        /// <summary>
        /// A value indicating whether the value is preselected.
        /// </summary>
        [JsonPropertyName("isPreSelected")]
        public bool IsPreSelected { get; set; }
    }
}
